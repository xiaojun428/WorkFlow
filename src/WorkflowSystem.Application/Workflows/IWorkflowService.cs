namespace WorkflowSystem.Application.Workflows;

/// <summary>
/// 工作流服务抽象：封装与工作流引擎（Elsa）的交互。
/// 业务代码永远通过此接口与引擎交互，不直接查询 Elsa 内部表。
/// 阶段 6 提供内存状态机实现；阶段 7 替换为 Elsa Workflows 3 实现；阶段 8 增加 Bookmark 挂起/恢复。
/// </summary>
public interface IWorkflowService
{
    /// <summary>
    /// 启动一个工作流实例，返回业务侧 WorkflowInstanceId。
    /// 启动时工作流运行到第一个审批节点即挂起（Bookmark），等待人工审批。
    /// </summary>
    /// <param name="documentId">文档 Id。</param>
    /// <param name="documentTypeCode">文档类型编码，用于定位绑定的流程定义。</param>
    /// <param name="correlationId">关联 Id（贯穿文档、工作流、邮件、日志）。</param>
    Task<Guid> StartAsync(Guid documentId, string documentTypeCode, string correlationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 推进工作流：完成当前审批节点并恢复到下一审批节点（若有）。
    /// </summary>
    /// <param name="workflowInstanceId">业务侧工作流实例 Id。</param>
    /// <param name="completedStepKey">刚完成审批的节点 StepKey（用于定位对应 Bookmark 并恢复）。</param>
    /// <returns>推进结果：是否所有节点已完成（流程结束）。</returns>
    Task<bool> AdvanceAsync(Guid workflowInstanceId, string completedStepKey, CancellationToken cancellationToken = default);

    /// <summary>终止工作流（Reject / Withdraw 时调用）。</summary>
    Task TerminateAsync(Guid workflowInstanceId, CancellationToken cancellationToken = default);
}
