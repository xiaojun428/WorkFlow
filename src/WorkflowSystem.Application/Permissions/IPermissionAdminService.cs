using WorkflowSystem.Application.Documents.Dtos;
using WorkflowSystem.Application.Permissions;

namespace WorkflowSystem.Application.Permissions;

/// <summary>
/// 权限管理服务抽象：供管理员界面配置「角色 → 操作权限码」与「角色 → 字段权限」。
/// 仅面向管理员，与运行时权限判定（IPermissionService）分离。
/// </summary>
public interface IPermissionAdminService
{
    /// <summary>查询全部角色。</summary>
    Task<IReadOnlyList<RoleListItemDto>> GetRolesAsync(CancellationToken cancellationToken = default);

    /// <summary>查询全部操作权限码。</summary>
    Task<IReadOnlyList<PermissionListItemDto>> GetPermissionsAsync(CancellationToken cancellationToken = default);

    /// <summary>查询某角色的权限配置（角色 + 全部权限码 + 已授予权限）。</summary>
    Task<RolePermissionConfigDto?> GetRolePermissionConfigAsync(Guid roleId, CancellationToken cancellationToken = default);

    /// <summary>保存某角色的操作权限（全量覆盖）。</summary>
    Task SaveRolePermissionsAsync(SaveRolePermissionsRequestDto dto, CancellationToken cancellationToken = default);

    /// <summary>查询字段权限配置（某角色在某文档类型下的所有字段权限）。</summary>
    Task<IReadOnlyList<FieldPermissionListItemDto>> GetFieldPermissionsAsync(
        Guid roleId,
        Guid documentTypeId,
        CancellationToken cancellationToken = default);

    /// <summary>保存某角色在某文档类型下的字段权限（全量覆盖）。</summary>
    Task SaveFieldPermissionsAsync(SaveFieldPermissionsRequestDto dto, CancellationToken cancellationToken = default);

    /// <summary>查询文档类型列表（供字段权限界面选择）。</summary>
    Task<IReadOnlyList<DocumentTypeDto>> GetDocumentTypesAsync(CancellationToken cancellationToken = default);
}
