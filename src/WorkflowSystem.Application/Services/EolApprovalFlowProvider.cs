using WorkflowSystem.Application.Workflows;
using WorkflowSystem.Domain.Constants;

namespace WorkflowSystem.Application.Services;

/// <summary>
/// EOL（产品停产）申请的审批流程定义提供者。
/// 流程：经理审批 → 品质部（QA）确认 → 完成。
/// 无金额条件，固定两级审批；品质部负责确认替代物料与库存处理方式。
/// </summary>
public class EolApprovalFlowProvider : IApprovalFlowProvider
{
    public bool Supports(string documentTypeCode)
        => string.Equals(documentTypeCode, DocumentTypeCodes.EOL, StringComparison.OrdinalIgnoreCase);

    public Task<IReadOnlyList<ApprovalStepDefinition>> ResolveStepsAsync(
        string documentTypeCode,
        IReadOnlyDictionary<string, DocumentFieldValueSnapshot> fieldValues,
        CancellationToken cancellationToken = default)
    {
        var steps = new List<ApprovalStepDefinition>
        {
            new("manager-approval", "经理审批", RoleNames.Manager, 100),
            new("qa-approval", "品质确认", RoleNames.QA, 100),
        };

        return Task.FromResult<IReadOnlyList<ApprovalStepDefinition>>(steps);
    }
}
