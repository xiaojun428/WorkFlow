using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Messages;
using Microsoft.EntityFrameworkCore;
using WorkflowSystem.Application.Workflows;
using WorkflowSystem.Domain.Entities.Workflow;
using WorkflowSystem.Domain.Enums;
using WorkflowSystem.Infrastructure.Persistence;

namespace WorkflowSystem.Infrastructure.Services;

/// <summary>
/// Elsa Workflows 3 的工作流服务实现。
/// 通过 Elsa 的 IWorkflowRuntime 客户端 API 启动/终止工作流，
/// 通过 IStimulusSender 向挂起的工作流实例投递刺激（stimulus）以恢复审批 Bookmark，
/// 同时在业务侧 WorkflowInstances 表保存 DocumentId ↔ ElsaInstanceId ↔ DefinitionVersionId 映射。
///
/// 业务查询永远不直接触碰 Elsa 内部表，仅通过本服务的映射表与 Elsa 交互。
/// 阶段 8：审批节点通过 Bookmark 挂起，审批完成时通过 IStimulusSender.SendAsync 恢复。
/// </summary>
public class ElsaWorkflowService : IWorkflowService
{
    // 审批步骤 Activity 的类型名（Elsa 从类名自动派生 Type，与 Workflows 程序集解耦）。
    private const string ApprovalStepActivityTypeName = "ManagerApprovalStep";

    private readonly WorkflowDbContext _db;
    private readonly IWorkflowRuntime _workflowRuntime;
    private readonly IStimulusSender _stimulusSender;

    public ElsaWorkflowService(WorkflowDbContext db, IWorkflowRuntime workflowRuntime, IStimulusSender stimulusSender)
    {
        _db = db;
        _workflowRuntime = workflowRuntime;
        _stimulusSender = stimulusSender;
    }

    public async Task<Guid> StartAsync(
        Guid documentId,
        string documentTypeCode,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        // 文档类型编码即工作流定义 ID（编程式定义时 DefinitionId 设为类型编码）。
        var definitionId = documentTypeCode;

        // 启动 Elsa 工作流实例；首个审批步骤 StepKey 作为 Input 传入，供 ApprovalStepActivity 设置 Bookmark stimulus。
        var client = await _workflowRuntime.CreateClientAsync(cancellationToken);
        var result = await client.CreateAndRunInstanceAsync(
            new CreateAndRunWorkflowInstanceRequest
            {
                WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionId(definitionId),
                CorrelationId = correlationId,
                Input = new Dictionary<string, object>
                {
                    ["DocumentId"] = documentId,
                    ["StepKey"] = "manager-approval",
                },
            },
            cancellationToken);

        var elsaInstanceId = result.WorkflowInstanceId
            ?? throw new Domain.Common.BusinessRuleViolationException("Elsa 工作流启动失败：未返回实例 Id。");

        // 查找业务侧的定义版本（用于版本绑定）。
        var definitionVersionId = await GetOrCreateDefinitionVersionAsync(definitionId, cancellationToken);

        // 在业务侧保存映射（不替代 Elsa 内部实例表）。
        var workflowInstance = new WorkflowInstance(documentId, definitionVersionId, elsaInstanceId, correlationId);
        _db.WorkflowInstances.Add(workflowInstance);
        await _db.SaveChangesAsync(cancellationToken);

        return workflowInstance.Id;
    }

    public async Task<bool> AdvanceAsync(Guid workflowInstanceId, string completedStepKey, CancellationToken cancellationToken = default)
    {
        var instance = await _db.WorkflowInstances
            .FirstOrDefaultAsync(x => x.Id == workflowInstanceId, cancellationToken);

        if (instance is null)
            return true;

        // 通过 IStimulusSender 向指定工作流实例投递刺激，恢复审批 Bookmark。
        // stimulus 即当前完成的 StepKey，供挂起的 ApprovalStepActivity 匹配恢复（其 Bookmark stimulus 即 StepKey）。
        // StimulusMetadata.WorkflowInstanceId 精确锁定实例，避免并发审批时误恢复其他实例。
        // 恢复后，ApprovalStepActivity 因 AutoComplete=true 自动完成，工作流继续到 Sequence 末尾并自然结束。
        // 返回值「流程是否结束」的最终判定由业务侧 IApprovalFlowProvider 决定（此处引擎侧骨架已恢复完成）。
        try
        {
            await _stimulusSender.SendAsync(
                ApprovalStepActivityTypeName,
                completedStepKey,
                new StimulusMetadata
                {
                    WorkflowInstanceId = instance.ElsaInstanceId,
                    CorrelationId = instance.CorrelationId,
                },
                cancellationToken);

            // 更新业务侧实例状态为已完成（引擎骨架结束后）。
            instance.UpdateStatus(WorkflowStatus.Completed);
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception)
        {
            // 恢复失败不阻断业务推进：业务侧以 ApprovalTask 表为权威，引擎仅作运行载体。
            // 此处不抛出，避免引擎细节影响审批闭环。
        }

        return true;
    }

    public async Task TerminateAsync(Guid workflowInstanceId, CancellationToken cancellationToken = default)
    {
        var instance = await _db.WorkflowInstances
            .FirstOrDefaultAsync(x => x.Id == workflowInstanceId, cancellationToken);

        if (instance is null)
            return;

        // 取消 Elsa 实例（使用 client API，避免过时的 CancelWorkflowAsync）。
        var client = await _workflowRuntime.CreateClientAsync(instance.ElsaInstanceId, cancellationToken);
        await client.CancelAsync(cancellationToken);

        // 更新业务侧状态。
        instance.UpdateStatus(WorkflowStatus.Cancelled);
        await _db.SaveChangesAsync(cancellationToken);
    }

    // ===================== 私有辅助 =====================

    /// <summary>
    /// 获取或创建业务侧工作流定义版本（用于版本绑定）。
    /// 阶段 7 简化：一个定义一个 Active 版本；阶段后续完善真正的版本发布与绑定。
    /// </summary>
    private async Task<Guid> GetOrCreateDefinitionVersionAsync(string definitionId, CancellationToken cancellationToken)
    {
        var definition = await _db.WorkflowDefinitions
            .FirstOrDefaultAsync(x => x.Name == definitionId, cancellationToken);

        if (definition is null)
        {
            definition = new WorkflowDefinition(definitionId);
            definition.Publish();
            _db.WorkflowDefinitions.Add(definition);
            await _db.SaveChangesAsync(cancellationToken);
        }

        var version = await _db.WorkflowDefinitionVersions
            .FirstOrDefaultAsync(x => x.WorkflowDefinitionId == definition.Id && x.IsActive, cancellationToken);

        if (version is null)
        {
            version = new WorkflowDefinitionVersion(definition.Id, 1, definitionId);
            _db.WorkflowDefinitionVersions.Add(version);
            await _db.SaveChangesAsync(cancellationToken);
        }

        return version.Id;
    }
}
