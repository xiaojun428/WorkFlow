using WorkflowSystem.Application.Workflows;
using WorkflowSystem.Domain.Constants;

namespace WorkflowSystem.Application.Services;

/// <summary>
/// 供应商申请的审批流程定义提供者。
/// 流程：采购部审批 → 经理审批 → 财务审批 → 完成。
/// 供应商准入涉及采购、管理、财务三级会签（各节点单人审批）。
/// </summary>
public class VendorApprovalFlowProvider : IApprovalFlowProvider
{
    public bool Supports(string documentTypeCode)
        => string.Equals(documentTypeCode, DocumentTypeCodes.Vendor, StringComparison.OrdinalIgnoreCase);

    public Task<IReadOnlyList<ApprovalStepDefinition>> ResolveStepsAsync(
        string documentTypeCode,
        IReadOnlyDictionary<string, DocumentFieldValueSnapshot> fieldValues,
        CancellationToken cancellationToken = default)
    {
        var steps = new List<ApprovalStepDefinition>
        {
            new("purchasing-approval", "采购审批", RoleNames.Purchasing, 100),
            new("manager-approval", "经理审批", RoleNames.Manager, 100),
            new("finance-approval", "财务审批", RoleNames.Finance, 100),
        };

        return Task.FromResult<IReadOnlyList<ApprovalStepDefinition>>(steps);
    }
}
