namespace WorkflowSystem.Application.Notifications;

/// <summary>
/// 邮件服务抽象：负责把通知邮件写入发件箱（Transactional Outbox），由后台服务异步发送。
/// 业务代码只调用此接口入队，不直接发送，避免邮件发送阻塞业务事务。
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// 入队一封通知邮件到发件箱。
    /// </summary>
    /// <param name="toAddress">收件人邮箱。</param>
    /// <param name="subject">邮件主题。</param>
    /// <param name="body">邮件正文。</param>
    /// <param name="correlationId">关联 Id（贯穿文档/工作流/邮件）。</param>
    /// <param name="idempotencyKey">幂等键：同一业务事件只入队一次，防止重复通知。</param>
    Task EnqueueAsync(string toAddress, string subject, string body, string correlationId, string idempotencyKey, CancellationToken cancellationToken = default);
}
