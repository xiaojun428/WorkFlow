using Microsoft.EntityFrameworkCore;
using WorkflowSystem.Application.Notifications;
using WorkflowSystem.Domain.Entities.Notification;
using WorkflowSystem.Infrastructure.Persistence;

namespace WorkflowSystem.Infrastructure.Services;

/// <summary>
/// 邮件服务实现：把通知邮件写入发件箱（MailOutbox），后台服务异步发送。
/// 幂等：通过 IdempotencyKey 唯一约束防止同一业务事件重复入队。
/// </summary>
public class EmailService : IEmailService
{
    private readonly WorkflowDbContext _db;

    public EmailService(WorkflowDbContext db)
    {
        _db = db;
    }

    public async Task EnqueueAsync(
        string toAddress,
        string subject,
        string body,
        string correlationId,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        // 幂等保护：已存在相同幂等键的邮件则跳过（重复通知）。
        var exists = await _db.MailOutbox
            .AnyAsync(x => x.IdempotencyKey == idempotencyKey, cancellationToken);
        if (exists)
            return;

        var mail = new MailOutbox(toAddress, subject, body, correlationId, idempotencyKey);
        _db.MailOutbox.Add(mail);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
