using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WorkflowSystem.Application.Notifications;
using WorkflowSystem.Domain.Entities.Audit;
using WorkflowSystem.Domain.Entities.Workflow;
using WorkflowSystem.Domain.Enums;
using WorkflowSystem.Infrastructure.Persistence;

namespace WorkflowSystem.Infrastructure.Services;

/// <summary>
/// 审批催办后台服务：扫描超时未处理的审批任务，按间隔发送催办邮件并记录审计。
///
/// 需求 7：超时默认 48 小时，每 24 小时催办一次。
/// - 超时：任务 DueAtUtc 已过且仍为 Pending。
/// - 催办间隔：距上次催办（LastReminderAtUtc）满 24 小时再次催办。
/// </summary>
public class ApprovalReminderService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ApprovalReminderService> _logger;
    private readonly ApprovalReminderOptions _options;
    private static readonly TimeSpan ScanInterval = TimeSpan.FromMinutes(10);

    public ApprovalReminderService(
        IServiceScopeFactory scopeFactory,
        ILogger<ApprovalReminderService> logger,
        IOptions<ApprovalReminderOptions> options)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options.Value;
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
                _logger.LogError(ex, "审批催办处理异常。");
            }

            await Task.Delay(ScanInterval, stoppingToken);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WorkflowDbContext>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
        var now = DateTime.UtcNow;

        var overdueCutoff = now.Subtract(_options.Timeout);
        var reminderInterval = _options.ReminderInterval;

        // 超时且仍未处理、且距上次催办已满间隔的任务。
        var tasksToRemind = await db.ApprovalTasks
            .Include(t => t.Document).ThenInclude(d => d!.ApplicantUser)
            .Include(t => t.AssignedUser)
            .Where(t => t.Status == ApprovalTaskStatus.Pending
                        && t.DueAtUtc != null
                        && t.DueAtUtc < overdueCutoff
                        && (t.LastReminderAtUtc == null || t.LastReminderAtUtc <= now.Subtract(reminderInterval)))
            .ToListAsync(cancellationToken);

        foreach (var task in tasksToRemind)
        {
            var assigneeEmail = task.AssignedUser?.Email ?? task.Document?.ApplicantUser?.Email;
            if (string.IsNullOrWhiteSpace(assigneeEmail))
                continue;

            var documentNo = task.Document?.DocumentNo ?? task.DocumentId.ToString();
            var subject = $"【催办】审批待处理：{documentNo} - {task.StepName}";
            var body = $"您有一项审批任务已超时未处理，请尽快处理。\n文档编号：{documentNo}\n审批步骤：{task.StepName}";

            // 幂等键：任务 + 催办时间戳，确保同一轮不重复。
            var idempotencyKey = $"reminder:{task.Id}:{now:yyyyMMddHH}";

            await emailService.EnqueueAsync(assigneeEmail, subject, body, task.DocumentId.ToString("N"), idempotencyKey, cancellationToken);

            // 记录审计 + 更新上次催办时间。
            task.MarkReminded(now);
            db.AuditLogs.Add(new AuditLog(
                "Remind",
                "ApprovalTask",
                userId: null,
                entityId: task.Id,
                detailJson: $"{{ \"DocumentNo\": \"{documentNo}\", \"Step\": \"{task.StepName}\" }}",
                correlationId: task.DocumentId.ToString("N")));

            _logger.LogInformation("已催办审批任务：TaskId={TaskId} DocumentNo={DocumentNo} Step={Step}", task.Id, documentNo, task.StepName);
        }

        if (tasksToRemind.Count > 0)
            await db.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>
/// 审批催办配置：超时时长与催办间隔（需求 7 默认 48h / 24h）。
/// </summary>
public class ApprovalReminderOptions
{
    /// <summary>审批超时时长（默认 48 小时）。</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromHours(48);

    /// <summary>催办间隔（默认 24 小时）。</summary>
    public TimeSpan ReminderInterval { get; set; } = TimeSpan.FromHours(24);
}
