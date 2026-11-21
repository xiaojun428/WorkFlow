using Elsa.Workflows;
using Elsa.Workflows.Models;

namespace WorkflowSystem.Workflows.Activities;

/// <summary>
/// 审批步骤 Activity：代表流程中的一个人工审批等待点（经理审批等）。
///
/// 阶段 8 起使用 Elsa 原生 Bookmark 机制实现真正的长运行人工审批：
/// - ExecuteAsync 创建一个 Bookmark 并挂起工作流（工作流自动 suspend，状态持久化到 Elsa）。
/// - 审批完成时，业务侧通过 IWorkflowService.AdvanceAsync 触发对应 Bookmark 恢复，
///   OnResumeAsync 被回调，本节点完成，工作流继续（Sequence 结束后自然完成）。
///
/// 说明：流程中"下一审批节点是谁"由业务侧 IApprovalFlowProvider 解析（金额条件分支/多级审批/会签），
/// 本 Activity 只表达"此处需要一次人工审批"的运行载体，不硬编码节点顺序。
/// </summary>
public class ManagerApprovalStep : Activity
{
    /// <summary>Bookmark 名称（恢复时用其定位挂起点）。</summary>
    public const string BookmarkName = "approval";

    public ManagerApprovalStep()
    {
        Name = "经理审批";
        Version = 1;
    }

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        // 创建 Bookmark 挂起工作流，等待人工审批。
        // Callback 指定恢复时回调 OnResumeAsync；AutoComplete=true 表示恢复后自动完成本节点。
        context.CreateBookmark(new CreateBookmarkArgs
        {
            BookmarkName = BookmarkName,
            // Stimulus 即当前审批步骤的 StepKey，作为恢复时的匹配依据（由业务侧投递）。
            Stimulus = GetStepKey(context),
            Callback = OnResumeAsync,
            AutoComplete = true,
        });

        // 挂起后不完成节点（等待恢复）。
        await Task.CompletedTask;
    }

    private ValueTask OnResumeAsync(ActivityExecutionContext context)
    {
        // 恢复回调：此处可读取输入、写回工作流变量等。
        // AutoComplete=true 时，Elsa 会在回调后完成本节点，工作流继续到 Sequence 的下一个元素。
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// 从输入或工作流属性读取当前审批步骤的 StepKey（作为 Bookmark 的 stimulus）。
    /// 阶段 8：StepKey 由业务侧在启动时作为 Input 传入；默认 "manager-approval"。
    /// </summary>
    private static string GetStepKey(ActivityExecutionContext context)
    {
        if (context.WorkflowInput.TryGetValue("StepKey", out var stepKey) && stepKey is string s)
            return s;
        return "manager-approval";
    }
}
