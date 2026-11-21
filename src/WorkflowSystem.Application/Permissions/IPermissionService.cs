using WorkflowSystem.Domain.Enums;

namespace WorkflowSystem.Application.Permissions;

/// <summary>
/// 当前用户权限上下文：缓存某次请求中计算出的用户权限快照，供授权处理器与字段裁剪使用。
/// </summary>
public sealed class PermissionContext
{
    /// <summary>用户持有的角色名集合（仅含当前有效的角色）。</summary>
    public IReadOnlySet<string> RoleNames { get; init; } = new HashSet<string>();

    /// <summary>用户持有的操作权限码集合。</summary>
    public IReadOnlySet<string> PermissionCodes { get; init; } = new HashSet<string>();

    /// <summary>用户所属部门 Id 集合（含兼任部门）。</summary>
    public IReadOnlySet<Guid> DepartmentIds { get; init; } = new HashSet<Guid>();

    /// <summary>数据范围（取用户各角色中最宽松的一种，All &gt; Department &gt; Self）。</summary>
    public DataScopeType EffectiveScope { get; init; } = DataScopeType.Self;

    /// <summary>用户是否有某个操作权限码。</summary>
    public bool HasPermission(string code) => PermissionCodes.Contains(code);

    /// <summary>用户是否有某个角色。</summary>
    public bool HasRole(string roleName) => RoleNames.Contains(roleName);
}

/// <summary>
/// 字段权限查询结果：某个字段对当前用户的最终生效权限。
/// 优先级：Hidden &gt; ReadOnly &gt; Required &gt; Editable（更严格的覆盖更宽松的）。
/// </summary>
public sealed record FieldPermissionResult(Guid FieldDefinitionId, FieldPermissionType PermissionType);

/// <summary>
/// 权限服务：负责计算当前用户的角色、操作权限、数据范围与字段权限。
/// 这是后端权限判定的唯一权威来源，Controller / AuthorizationHandler 均通过它判定。
/// </summary>
public interface IPermissionService
{
    /// <summary>
    /// 获取当前用户的权限上下文（角色、操作权限、部门、数据范围）。
    /// </summary>
    Task<PermissionContext> GetCurrentUserContextAsync(CancellationToken cancellationToken = default);

    /// <summary>判断当前用户是否拥有某操作权限码。</summary>
    Task<bool> HasPermissionAsync(string permissionCode, CancellationToken cancellationToken = default);

    /// <summary>判断当前用户是否属于某角色（含有效期校验）。</summary>
    Task<bool> IsInRoleAsync(string roleName, CancellationToken cancellationToken = default);

    /// <summary>
    /// 查询指定文档类型下，某字段对当前用户的生效字段权限（无配置时返回 null，表示默认 Editable）。
    /// </summary>
    Task<FieldPermissionType?> GetFieldPermissionAsync(
        Guid documentTypeId,
        Guid fieldDefinitionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量查询指定文档类型下多个字段的生效权限，返回「字段定义 Id → 权限」映射（无配置的字段不包含在内，表示默认 Editable）。
    /// 用于视图层字段裁剪，避免逐字段查询。
    /// </summary>
    Task<IReadOnlyDictionary<Guid, FieldPermissionType>> GetFieldPermissionsAsync(
        Guid documentTypeId,
        IEnumerable<Guid> fieldDefinitionIds,
        CancellationToken cancellationToken = default);
}
