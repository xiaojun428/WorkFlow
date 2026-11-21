using Elsa.Workflows;
using Elsa.Workflows.Activities;
using WorkflowSystem.Workflows.Activities;

namespace WorkflowSystem.Workflows.Definitions;

/// <summary>
/// 供应商申请工作流定义（Elsa 3 编程式定义）。
/// 流程：采购审批 → 经理审批 → 财务审批 → 完成。
/// 节点序列由 VendorApprovalFlowProvider 在业务侧解析，此处用单个可挂起的审批 Activity 承载运行态。
/// </summary>
public class VendorWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        builder.DefinitionId = "Vendor";
        builder.Name = "供应商申请审批流程";
        builder.Description = "供应商申请：采购审批 → 经理审批 → 财务审批 → 完成";

        builder.Root = new Sequence
        {
            Activities =
            {
                new ManagerApprovalStep(),
            },
        };
    }
}
