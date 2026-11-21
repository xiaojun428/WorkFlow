using WorkflowSystem.Domain.Enums;

namespace WorkflowSystem.Application.Permissions;

/// <summary>角色列表项（权限管理界面）。</summary>
public sealed record RoleListItemDto(Guid Id, string Name, bool IsSystem);

/// <summary>权限（操作权限码）列表项。</summary>
public sealed record PermissionListItemDto(Guid Id, string Code, string Description);

/// <summary>角色权限配置视图：角色信息 + 全部权限码 + 已授予的权限码集合。</summary>
public sealed record RolePermissionConfigDto(
    Guid RoleId,
    string RoleName,
    bool IsSystem,
    IReadOnlyList<PermissionListItemDto> AllPermissions,
    IReadOnlySet<Guid> GrantedPermissionIds);

/// <summary>保存角色操作权限请求：一次性提交该角色的完整权限码集合（全量覆盖）。</summary>
public sealed class SaveRolePermissionsRequestDto
{
    public Guid RoleId { get; set; }
    public List<Guid> PermissionIds { get; set; } = new();
}

/// <summary>字段权限列表项（某角色在某文档类型下所有字段的权限配置）。</summary>
public sealed record FieldPermissionListItemDto(
    Guid RoleId,
    string RoleName,
    Guid DocumentTypeId,
    string DocumentTypeName,
    Guid FieldDefinitionId,
    string FieldKey,
    string FieldDisplayName,
    FieldPermissionType? PermissionType);

/// <summary>保存字段权限请求：为某角色在某文档类型下设置字段权限（未列出的字段视为无配置=默认 Editable）。</summary>
public sealed class SaveFieldPermissionsRequestDto
{
    public Guid RoleId { get; set; }
    public Guid DocumentTypeId { get; set; }
    public Dictionary<Guid, FieldPermissionType> FieldPermissions { get; set; } = new();
}
