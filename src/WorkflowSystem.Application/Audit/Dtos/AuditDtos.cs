namespace WorkflowSystem.Application.Audit.Dtos;

/// <summary>审计日志查询参数。</summary>
public sealed class AuditQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Action { get; set; }
    public string? EntityType { get; set; }
}

/// <summary>审计日志条目。</summary>
public sealed record AuditLogItemDto(
    Guid Id,
    Guid? UserId,
    string? UserName,
    string Action,
    string EntityType,
    Guid? EntityId,
    string DetailJson,
    string CorrelationId,
    DateTime OccurredAtUtc);
