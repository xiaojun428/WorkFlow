using Elsa.Workflows;

namespace WorkflowSystem.Workflows.Activities;

/// <summary>
/// 审批节点 Activity：代表流程中的一个人工审批步骤（经理审批）。
///
/// 设计说明：审批的"等待人工操作"由业务侧 ApprovalTask 表 + Document.CurrentWorkflowStep 表达，
/// 本 Activity 在 Elsa 工作流中标记该审批步骤并立即完成节点，流程骨架由 Elsa 持久化。
/// 真正的审批通过/驳回由 IApprovalService 驱动 IWorkflowService 推进。
///
/// 后续可扩展：在本 Activity 内创建 Bookmark 挂起工作流，审批完成时通过 Bookmark 恢复，
/// 实现 Elsa 原生的长运行人工审批等待（阶段 8 完善）。
/// </summary>
public class ManagerApprovalStep : Activity
{
    public ManagerApprovalStep()
    {
        // 设置节点显示名称（Elsa 设计器中可见）。Activity Type 由 Elsa 从类名自动派生。
        Name = "经理审批";
        Version = 1;
    }

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        // 阶段 7 简化：审批等待由业务侧管理，节点立即完成，流程骨架持久化到 Elsa。
        // 后续阶段改为创建 Bookmark 挂起，实现真正的人工审批等待。
        await CompleteAsync(context);
    }
}
