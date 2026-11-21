using WorkflowSystem.Domain.Common;

namespace WorkflowSystem.Domain.Entities.Audit;

/// <summary>
/// 审计日志：追加写入、不可修改、不可删除（普通角色）。
/// 记录关键操作：登录、授权失败、审批、权限变更、流程发布、管理员操作等。
/// </summary>
public class AuditLog : Entity
{
    public Guid? UserId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string EntityType { get; private set; } = string.Empty;
    public Guid? EntityId { get; private set; }
    public string DetailJson { get; private set; } = "{}";
    public string CorrelationId { get; private set; } = string.Empty;
    public string? IpAddress { get; private set; }
    public DateTime OccurredAtUtc { get; private set; } = DateTime.UtcNow;

    private AuditLog() { }

    public AuditLog(string action, string entityType, Guid? userId = null, Guid? entityId = null, string detailJson = "{}", string correlationId = "", string? ipAddress = null)
    {
        Action = action;
        EntityType = entityType;
        UserId = userId;
        EntityId = entityId;
        DetailJson = detailJson;
        CorrelationId = correlationId;
        IpAddress = ipAddress;
    }
}
