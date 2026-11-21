using Microsoft.Extensions.Logging;

namespace WorkflowSystem.Infrastructure.Services;

/// <summary>
/// 日志邮件发送器（开发/演示环境）：不真正投递，仅记录日志。
/// 生产环境替换为 SMTP / Graph / SendGrid 实现。
/// </summary>
public class LoggingMailSender : IMailSender
{
    private readonly ILogger<LoggingMailSender> _logger;

    public LoggingMailSender(ILogger<LoggingMailSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string toAddress, string subject, string body, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[MAIL] To={ToAddress} Subject={Subject} Body={Body}",
            toAddress,
            subject,
            body);
        return Task.CompletedTask;
    }
}
