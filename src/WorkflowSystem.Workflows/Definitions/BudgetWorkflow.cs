using Elsa.Workflows;
using Elsa.Workflows.Activities;
using WorkflowSystem.Workflows.Activities;

namespace WorkflowSystem.Workflows.Definitions;

/// <summary>
/// Budget 预算申请工作流定义（Elsa 3 编程式定义）。
/// 流程：申请人 → 经理审批 → （金额条件分支）→ 完成。
///
/// 金额条件分支（需求示例）：
/// - Amount < 10,000：经理审批后完成。
/// - 10,000 <= Amount < 100,000：经理 → 部长。
/// - Amount >= 100,000：经理 → 部长 → 财务 → 事业部负责人。
///
/// 说明：阶段 7 先定义流程骨架，金额条件分支通过自定义审批 Activity + 业务侧判定完成
/// （审批节点推进由 IApprovalService 通过 IWorkflowService.AdvanceAsync 驱动）。
/// 此处用 Sequence 表达顺序节点，每个节点是一个自定义 ApprovalStepActivity 占位。
/// </summary>
public class BudgetWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        builder.DefinitionId = "Budget";
        builder.Name = "预算申请审批流程";
        builder.Description = "预算申请：经理审批 → （按金额）部长/财务/事业部负责人 → 完成";

        builder.Root = new Sequence
        {
            Activities =
            {
                // 第一步：经理审批。后续节点由业务侧（金额条件）动态推进，避免在引擎内硬编码分支。
                new ManagerApprovalStep(),
            },
        };
    }
}

/// <summary>
/// 标记类：仅用于 AddActivitiesFrom/AddWorkflowsFrom 扫描所在程序集。
/// </summary>
public sealed class WorkflowsMarker
{
}
