using Elsa.Workflows;
using Elsa.Workflows.Activities;
using WorkflowSystem.Workflows.Activities;

namespace WorkflowSystem.Workflows.Definitions;

/// <summary>
/// 采购申请工作流定义（Elsa 3 编程式定义）。
/// 流程：经理审批 → （按金额）财务 → 完成。
/// 节点序列由 PurchaseApprovalFlowProvider 在业务侧解析，此处用单个可挂起的审批 Activity 承载运行态。
/// </summary>
public class PurchaseWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        builder.DefinitionId = "Purchase";
        builder.Name = "采购申请审批流程";
        builder.Description = "采购申请：经理审批 → （按金额）财务 → 完成";

        builder.Root = new Sequence
        {
            Activities =
            {
                new ManagerApprovalStep(),
            },
        };
    }
}
