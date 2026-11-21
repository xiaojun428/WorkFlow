using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WorkflowSystem.Domain.Entities.Notification;
using WorkflowSystem.Domain.Enums;
using WorkflowSystem.Infrastructure.Persistence;

namespace WorkflowSystem.Infrastructure.Services;

/// <summary>
/// 邮件发件箱后台处理器：周期扫描 MailOutbox 中待发送（Pending）或可重试（Failed 且到期）的邮件，
/// 调用 IMailSender 投递，并记录发送结果到 MailHistory。
/// 邮件发送与业务事务解耦（Transactional Outbox 的消费端）。
/// </summary>
public class MailOutboxProcessor : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MailOutboxProcessor> _logger;
    private static readonly TimeSpan ScanInterval = TimeSpan.FromSeconds(15);

    public MailOutboxProcessor(IServiceScopeFactory scopeFactory, ILogger<MailOutboxProcessor> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "邮件发件箱处理异常。");
            }

            await Task.Delay(ScanInterval, stoppingToken);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WorkflowDbContext>();
        var sender = scope.ServiceProvider.GetRequiredService<IMailSender>();
        var now = DateTime.UtcNow;

        // 待发送：Pending；可重试：Failed 且 NextRetryAtUtc 已到期。
        var pendingMails = await db.MailOutbox
            .Where(m => m.Status == MailStatus.Pending
                        || (m.Status == MailStatus.Failed && m.NextRetryAtUtc != null && m.NextRetryAtUtc <= now))
            .OrderBy(m => m.CreatedAtUtc)
            .Take(50)
            .ToListAsync(cancellationToken);

        foreach (var mail in pendingMails)
        {
            try
            {
                await sender.SendAsync(mail.ToAddress, mail.Subject, mail.Body, cancellationToken);

                mail.MarkSent();
                db.MailHistories.Add(new MailHistory(mail.Id, MailStatus.Sent));
                _logger.LogInformation("邮件发送成功：Id={Id} To={To}", mail.Id, mail.ToAddress);
            }
            catch (Exception ex)
            {
                mail.MarkFailed(maxRetries: 5);
                db.MailHistories.Add(new MailHistory(mail.Id, mail.Status, errorMessage: ex.Message));
                _logger.LogWarning(ex, "邮件发送失败：Id={Id} To={To} Retry={Retry}", mail.Id, mail.ToAddress, mail.RetryCount);
            }
        }

        if (pendingMails.Count > 0)
            await db.SaveChangesAsync(cancellationToken);
    }
}
