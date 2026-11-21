using Elsa.Workflows;
using Elsa.Workflows.Activities;
using WorkflowSystem.Workflows.Activities;

namespace WorkflowSystem.Workflows.Definitions;

/// <summary>
/// EOL 产品停产申请工作流定义（Elsa 3 编程式定义）。
/// 流程：经理审批 → 品质确认 → 完成。
/// 节点序列由 EolApprovalFlowProvider 在业务侧解析，此处用单个可挂起的审批 Activity 承载运行态。
/// </summary>
public class EolWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        builder.DefinitionId = "EOL";
        builder.Name = "EOL 申请审批流程";
        builder.Description = "EOL 申请：经理审批 → 品质确认 → 完成";

        builder.Root = new Sequence
        {
            Activities =
            {
                new ManagerApprovalStep(),
            },
        };
    }
}
