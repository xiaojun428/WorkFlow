namespace WorkflowSystem.Application.Workflows;

/// <summary>
/// 工作流服务抽象：封装与工作流引擎（Elsa）的交互。
/// 业务代码永远通过此接口与引擎交互，不直接查询 Elsa 内部表。
/// 阶段 6 提供内存状态机实现；阶段 7 替换为 Elsa Workflows 3 实现。
/// </summary>
public interface IWorkflowService
{
    /// <summary>
    /// 启动一个工作流实例，返回业务侧 WorkflowInstanceId。
    /// </summary>
    /// <param name="documentId">文档 Id。</param>
    /// <param name="documentTypeCode">文档类型编码，用于定位绑定的流程定义。</param>
    /// <param name="correlationId">关联 Id（贯穿文档、工作流、邮件、日志）。</param>
    Task<Guid> StartAsync(Guid documentId, string documentTypeCode, string correlationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 推进工作流到下一节点，返回下一节点的 StepKey（若流程结束则返回 null）。
    /// </summary>
    Task<string?> AdvanceAsync(Guid workflowInstanceId, CancellationToken cancellationToken = default);

    /// <summary>终止工作流（Reject / Withdraw 时调用）。</summary>
    Task TerminateAsync(Guid workflowInstanceId, CancellationToken cancellationToken = default);
}
