using Microsoft.EntityFrameworkCore;
using WorkflowSystem.Application.Workflows;
using WorkflowSystem.Domain.Entities.Workflow;
using WorkflowSystem.Domain.Enums;
using WorkflowSystem.Infrastructure.Persistence;

namespace WorkflowSystem.Infrastructure.Services;

/// <summary>
/// 最小工作流服务实现（阶段 6）：使用内存状态机模拟流程推进，不依赖 Elsa。
/// 阶段 7 将替换为 Elsa Workflows 3 实现（接口签名保持不变）。
///
/// 当前实现：Budget 流程固定节点序列 [Manager 审批 → 部长审批 → 完成]。
/// 金额条件分支（<1万 / 1万-10万 / ≥10万）在阶段 7 集成 Elsa 时完整实现。
/// </summary>
public class MinimalWorkflowService : IWorkflowService
{
    private readonly WorkflowDbContext _db;

    // 简化流程定义：节点序列（StepKey, StepName）。
    private static readonly (string Key, string Name)[] BudgetSteps =
    {
        ("manager-approval", "经理审批"),
        ("director-approval", "部长审批"),
    };

    public MinimalWorkflowService(WorkflowDbContext db)
    {
        _db = db;
    }

    public async Task<Guid> StartAsync(
        Guid documentId,
        string documentTypeCode,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        // 阶段 6：不接 Elsa，创建业务侧 WorkflowInstance 映射，用固定定义版本 Id 占位。
        // 找到或创建一个 WorkflowDefinitionVersion 用于绑定（简化：直接创建）。
        var definition = await GetOrCreateDefinitionAsync(documentTypeCode, cancellationToken);
        var version = await GetOrCreateActiveVersionAsync(definition.Id, cancellationToken);

        var workflowInstance = new WorkflowInstance(documentId, version.Id, Guid.NewGuid().ToString("N"), correlationId);
        _db.WorkflowInstances.Add(workflowInstance);
        await _db.SaveChangesAsync(cancellationToken);

        return workflowInstance.Id;
    }

    public async Task<string?> AdvanceAsync(Guid workflowInstanceId, CancellationToken cancellationToken = default)
    {
        var instance = await _db.WorkflowInstances
            .FirstOrDefaultAsync(x => x.Id == workflowInstanceId, cancellationToken)
            ?? throw new Domain.Common.NotFoundException("WorkflowInstance", workflowInstanceId);

        // 阶段 6 简化：基于文档当前步骤推进到下一个固定节点。
        var document = await _db.Documents
            .FirstOrDefaultAsync(x => x.Id == instance.DocumentId, cancellationToken)
            ?? throw new Domain.Common.NotFoundException("Document", instance.DocumentId);

        var currentStep = document.CurrentWorkflowStep;
        var steps = BudgetSteps;

        // 找到当前步骤索引。
        var index = string.IsNullOrEmpty(currentStep) ? -1 : Array.FindIndex(steps, s => s.Key == currentStep);
        var nextIndex = index + 1;

        if (nextIndex >= steps.Length)
        {
            // 所有节点完成。
            instance.UpdateStatus(WorkflowStatus.Completed);
            await _db.SaveChangesAsync(cancellationToken);
            return null;
        }

        document.UpdateCurrentStep(steps[nextIndex].Key);
        await _db.SaveChangesAsync(cancellationToken);
        return steps[nextIndex].Key;
    }

    public async Task TerminateAsync(Guid workflowInstanceId, CancellationToken cancellationToken = default)
    {
        var instance = await _db.WorkflowInstances
            .FirstOrDefaultAsync(x => x.Id == workflowInstanceId, cancellationToken);

        if (instance is null) return;
        instance.UpdateStatus(WorkflowStatus.Cancelled);
        await _db.SaveChangesAsync(cancellationToken);
    }

    // ===================== 私有辅助 =====================

    private async Task<WorkflowDefinition> GetOrCreateDefinitionAsync(string documentTypeCode, CancellationToken cancellationToken)
    {
        var name = $"{documentTypeCode}-workflow";
        var definition = await _db.WorkflowDefinitions
            .FirstOrDefaultAsync(x => x.Name == name, cancellationToken);

        if (definition is null)
        {
            definition = new WorkflowDefinition(name);
            definition.Publish();
            _db.WorkflowDefinitions.Add(definition);
            await _db.SaveChangesAsync(cancellationToken);
        }

        return definition;
    }

    private async Task<WorkflowDefinitionVersion> GetOrCreateActiveVersionAsync(Guid definitionId, CancellationToken cancellationToken)
    {
        var version = await _db.WorkflowDefinitionVersions
            .FirstOrDefaultAsync(x => x.WorkflowDefinitionId == definitionId && x.IsActive, cancellationToken);

        if (version is null)
        {
            version = new WorkflowDefinitionVersion(definitionId, 1, Guid.NewGuid().ToString("N"));
            _db.WorkflowDefinitionVersions.Add(version);
            await _db.SaveChangesAsync(cancellationToken);
        }

        return version;
    }
}
