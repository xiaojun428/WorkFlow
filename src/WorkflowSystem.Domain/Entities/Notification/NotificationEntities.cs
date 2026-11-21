using WorkflowSystem.Domain.Common;
using WorkflowSystem.Domain.Enums;

namespace WorkflowSystem.Domain.Entities.Notification;

/// <summary>
/// 邮件模板：用于审批通知等邮件的主题与正文模板。
/// </summary>
public class MailTemplate : Entity
{
    public string Code { get; private set; } = string.Empty;
    public string SubjectTemplate { get; private set; } = string.Empty;
    public string BodyTemplate { get; private set; } = string.Empty;

    private MailTemplate() { }

    public MailTemplate(string code, string subjectTemplate, string bodyTemplate)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new BusinessRuleViolationException("邮件模板编码不能为空。");
        Code = code;
        SubjectTemplate = subjectTemplate;
        BodyTemplate = bodyTemplate;
    }
}

/// <summary>
/// 邮件发件箱（Transactional Outbox）：业务事务写入，后台服务异步发送。
/// 邮件发送不与数据库事务强耦合。
/// </summary>
public class MailOutbox : Entity
{
    public string ToAddress { get; private set; } = string.Empty;
    public string Subject { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public MailStatus Status { get; private set; } = MailStatus.Pending;
    public int RetryCount { get; private set; }
    public DateTime? NextRetryAtUtc { get; private set; }
    public string CorrelationId { get; private set; } = string.Empty;
    public string IdempotencyKey { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;

    private MailOutbox() { }

    public MailOutbox(string toAddress, string subject, string body, string correlationId, string idempotencyKey)
    {
        ToAddress = toAddress;
        Subject = subject;
        Body = body;
        CorrelationId = correlationId;
        IdempotencyKey = idempotencyKey;
    }

    public void MarkSent() => Status = MailStatus.Sent;

    /// <summary>发送失败：增加重试次数，并按退避策略计算下次重试时间。</summary>
    public void MarkFailed(int maxRetries)
    {
        RetryCount++;
        if (RetryCount >= maxRetries)
        {
            Status = MailStatus.DeadLetter;   // 超过最大重试次数进入死信，人工处理
        }
        else
        {
            Status = MailStatus.Failed;
            // 指数退避：2^n 分钟（2, 4, 8, 16...）
            var backoffMinutes = Math.Pow(2, RetryCount);
            NextRetryAtUtc = DateTime.UtcNow.AddMinutes(backoffMinutes);
        }
    }
}

/// <summary>
/// 邮件发送历史：记录发送结果与错误，不泄露 Token 或敏感正文。
/// </summary>
public class MailHistory : Entity
{
    public Guid OutboxId { get; private set; }
    public DateTime SentAtUtc { get; private set; } = DateTime.UtcNow;
    public MailStatus Status { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? MessageId { get; private set; }

    public MailOutbox? Outbox { get; private set; }

    private MailHistory() { }

    public MailHistory(Guid outboxId, MailStatus status, string? errorMessage = null, string? messageId = null)
    {
        OutboxId = outboxId;
        Status = status;
        ErrorMessage = errorMessage;
        MessageId = messageId;
    }
}

/// <summary>
/// 汇率：支持多币种金额按汇率转换（你确认的业务规则 6）。
/// 基准币种默认为 CNY。
/// </summary>
public class ExchangeRate : Entity
{
    public CurrencyCode FromCurrency { get; private set; }
    public CurrencyCode ToCurrency { get; private set; }
    public decimal Rate { get; private set; }
    public DateTime EffectiveFromUtc { get; private set; }

    private ExchangeRate() { }

    public ExchangeRate(CurrencyCode fromCurrency, CurrencyCode toCurrency, decimal rate, DateTime effectiveFromUtc)
    {
        if (rate <= 0) throw new BusinessRuleViolationException("汇率必须大于 0。");
        FromCurrency = fromCurrency;
        ToCurrency = toCurrency;
        Rate = rate;
        EffectiveFromUtc = effectiveFromUtc;
    }
}
