using WorkflowSystem.Application.Abstractions;
using WorkflowSystem.Application.Audit;
using WorkflowSystem.Domain.Entities.Audit;
using WorkflowSystem.Infrastructure.Persistence;

namespace WorkflowSystem.Infrastructure.Services;

/// <summary>
/// 审计服务实现：记录关键操作到 AuditLogs（只追加写入）。
/// 审计日志不参与业务事务回滚（与业务分离），失败不阻断主流程。
/// </summary>
public class AuditService : IAuditService
{
    private readonly WorkflowDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IClock _clock;

    public AuditService(WorkflowDbContext db, ICurrentUserService currentUser, IClock clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task LogAsync(
        string action,
        string entityType,
        Guid entityId,
        string? detail = null,
        string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        var log = new AuditLog(
            action,
            entityType,
            userId: _currentUser.UserId,
            entityId: entityId,
            detailJson: detail ?? "{}",
            correlationId: correlationId ?? string.Empty);

        _db.AuditLogs.Add(log);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
