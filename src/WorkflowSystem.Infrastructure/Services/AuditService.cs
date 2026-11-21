using Microsoft.EntityFrameworkCore;
using WorkflowSystem.Application.Abstractions;
using WorkflowSystem.Application.Audit;
using WorkflowSystem.Application.Audit.Dtos;
using WorkflowSystem.Application.Documents.Dtos;
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

    public async Task<PagedResult<AuditLogItemDto>> QueryAsync(AuditQuery query, CancellationToken cancellationToken = default)
    {
        var q = _db.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Action))
            q = q.Where(x => x.Action.Contains(query.Action));
        if (!string.IsNullOrWhiteSpace(query.EntityType))
            q = q.Where(x => x.EntityType.Contains(query.EntityType));

        var totalCount = await q.CountAsync(cancellationToken);
        var items = await q
            .OrderByDescending(x => x.OccurredAtUtc)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(x => new AuditLogItemDto(
                x.Id,
                x.UserId,
                null,
                x.Action,
                x.EntityType,
                x.EntityId,
                x.DetailJson,
                x.CorrelationId,
                x.OccurredAtUtc))
            .ToListAsync(cancellationToken);

        return new PagedResult<AuditLogItemDto>(items, totalCount, query.Page, query.PageSize);
    }
}
