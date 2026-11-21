namespace WorkflowSystem.Infrastructure.Services;

/// <summary>
/// 邮件发送器抽象：真正执行 SMTP 投递。
/// 阶段 9 提供日志实现（开发环境），生产环境替换为 SMTP（如 Microsoft Graph / SendGrid / SMTP 客户端）。
/// </summary>
public interface IMailSender
{
    /// <summary>
    /// 发送一封邮件，返回是否成功。失败时抛出异常由调用方捕获并记录历史。
    /// </summary>
    Task SendAsync(string toAddress, string subject, string body, CancellationToken cancellationToken = default);
}
