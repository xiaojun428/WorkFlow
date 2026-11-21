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
/// 通过 Elsa 的 IWorkflowRuntime 客户端 API 启动/推进/终止工作流，
/// 同时在业务侧 WorkflowInstances 表保存 DocumentId ↔ ElsaInstanceId ↔ DefinitionVersionId 映射。
///
/// 业务查询永远不直接触碰 Elsa 内部表，仅通过本服务的映射表与 Elsa 交互。
/// </summary>
public class ElsaWorkflowService : IWorkflowService
{
    private readonly WorkflowDbContext _db;
    private readonly IWorkflowRuntime _workflowRuntime;

    public ElsaWorkflowService(WorkflowDbContext db, IWorkflowRuntime workflowRuntime)
    {
        _db = db;
        _workflowRuntime = workflowRuntime;
    }

    public async Task<Guid> StartAsync(
        Guid documentId,
        string documentTypeCode,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        // 文档类型编码即工作流定义 ID（编程式定义时 DefinitionId 设为类型编码）。
        var definitionId = documentTypeCode;

        // 启动 Elsa 工作流实例。
        var client = await _workflowRuntime.CreateClientAsync(cancellationToken);
        var result = await client.CreateAndRunInstanceAsync(
            new CreateAndRunWorkflowInstanceRequest
            {
                WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionId(definitionId),
                CorrelationId = correlationId,
                Input = new Dictionary<string, object>
                {
                    ["DocumentId"] = documentId,
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

    public async Task<string?> AdvanceAsync(Guid workflowInstanceId, CancellationToken cancellationToken = default)
    {
        // 阶段 7：节点推进由业务侧（ApprovalService）根据文档 CurrentWorkflowStep 驱动。
        // Elsa 负责持久化实例状态；此处返回 null 表示"流程结束"的判定交由业务侧，
        // 后续可扩展为读取 Elsa 实例的 Bookmark/状态来精确推进。
        var instance = await _db.WorkflowInstances
            .FirstOrDefaultAsync(x => x.Id == workflowInstanceId, cancellationToken);

        if (instance is null)
            return null;

        // 阶段 7 简化：业务侧已有 BudgetSteps 序列逻辑（在 ApprovalService 中）。
        // 这里返回当前映射存在即视为可推进，具体下一节点由 ApprovalService 决定。
        return null;
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
