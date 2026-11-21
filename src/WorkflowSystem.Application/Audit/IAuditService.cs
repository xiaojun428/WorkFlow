namespace WorkflowSystem.Application.Audit;

using WorkflowSystem.Application.Audit.Dtos;
using WorkflowSystem.Application.Documents.Dtos;

/// <summary>
/// 审计服务抽象：记录登录、授权失败、审批、权限变更、流程发布和管理员操作等安全事件。
/// 审计日志只追加写入，普通业务角色不允许修改或删除。
/// </summary>
public interface IAuditService
{
    /// <summary>
    /// 记录一条审计日志。
    /// </summary>
    /// <param name="action">动作名称（如 Approve、Reject、Submit）。</param>
    /// <param name="entityType">实体类型（如 Document、ApprovalTask）。</param>
    /// <param name="entityId">实体 Id。</param>
    /// <param name="detail">详情（JSON）。</param>
    /// <param name="correlationId">关联 Id（可选）。</param>
    Task LogAsync(string action, string entityType, Guid entityId, string? detail = null, string? correlationId = null, CancellationToken cancellationToken = default);

    /// <summary>分页查询审计日志（用于催办记录、审计查询界面）。</summary>
    Task<PagedResult<AuditLogItemDto>> QueryAsync(AuditQuery query, CancellationToken cancellationToken = default);
}
