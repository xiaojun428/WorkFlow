using Microsoft.EntityFrameworkCore;
using WorkflowSystem.Application.Documents.Dtos;
using WorkflowSystem.Domain.Entities.Identity;
using WorkflowSystem.Domain.Entities.Permission;
using WorkflowSystem.Infrastructure.Persistence;

namespace WorkflowSystem.Infrastructure.Services;

/// <summary>
/// 权限管理服务实现：管理员配置角色操作权限与字段权限。
/// 使用全量覆盖语义（先删后增），保证提交即最终状态，避免前端增删合并的复杂性与不一致。
/// </summary>
public class PermissionAdminService : Application.Permissions.IPermissionAdminService
{
    private readonly WorkflowDbContext _db;

    public PermissionAdminService(WorkflowDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Application.Permissions.RoleListItemDto>> GetRolesAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Roles
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new Application.Permissions.RoleListItemDto(x.Id, x.Name, x.IsSystem))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Application.Permissions.PermissionListItemDto>> GetPermissionsAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Permissions
            .AsNoTracking()
            .OrderBy(x => x.Code)
            .Select(x => new Application.Permissions.PermissionListItemDto(x.Id, x.Code, x.Description))
            .ToListAsync(cancellationToken);
    }

    public async Task<Application.Permissions.RolePermissionConfigDto?> GetRolePermissionConfigAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        var role = await _db.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == roleId, cancellationToken);
        if (role is null) return null;

        var allPermissions = await GetPermissionsAsync(cancellationToken);
        var grantedIds = await _db.RolePermissions
            .AsNoTracking()
            .Where(rp => rp.RoleId == roleId)
            .Select(rp => rp.PermissionId)
            .ToHashSetAsync(cancellationToken);

        return new Application.Permissions.RolePermissionConfigDto(
            role.Id, role.Name, role.IsSystem, allPermissions, grantedIds);
    }

    public async Task SaveRolePermissionsAsync(Application.Permissions.SaveRolePermissionsRequestDto dto, CancellationToken cancellationToken = default)
    {
        var role = await _db.Roles
            .FirstOrDefaultAsync(x => x.Id == dto.RoleId, cancellationToken)
            ?? throw new Domain.Common.NotFoundException("Role", dto.RoleId);

        // 校验权限 Id 均有效。
        var validIds = await _db.Permissions
            .AsNoTracking()
            .Where(p => dto.PermissionIds.Contains(p.Id))
            .Select(p => p.Id)
            .ToHashSetAsync(cancellationToken);

        if (validIds.Count != dto.PermissionIds.Distinct().Count())
            throw new Domain.Common.BusinessRuleViolationException("包含无效的权限 Id。");

        // 全量覆盖：删除旧关联，插入新关联。
        var existing = await _db.RolePermissions
            .Where(rp => rp.RoleId == dto.RoleId)
            .ToListAsync(cancellationToken);
        _db.RolePermissions.RemoveRange(existing);

        foreach (var permissionId in validIds)
        {
            _db.RolePermissions.Add(new RolePermission(dto.RoleId, permissionId));
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Application.Permissions.FieldPermissionListItemDto>> GetFieldPermissionsAsync(
        Guid roleId,
        Guid documentTypeId,
        CancellationToken cancellationToken = default)
    {
        // 取该文档类型下所有有效字段定义。
        var fields = await _db.DocumentFieldDefinitions
            .AsNoTracking()
            .Where(f => f.DocumentTypeId == documentTypeId && f.IsActive)
            .OrderBy(f => f.DisplayOrder)
            .ToListAsync(cancellationToken);

        // 取该角色已配置的字段权限。
        var configured = await _db.FieldPermissions
            .AsNoTracking()
            .Where(fp => fp.RoleId == roleId && fp.DocumentTypeId == documentTypeId)
            .ToDictionaryAsync(fp => fp.FieldDefinitionId, fp => fp.PermissionType, cancellationToken);

        var documentTypeName = await _db.DocumentTypes
            .AsNoTracking()
            .Where(dt => dt.Id == documentTypeId)
            .Select(dt => dt.Name)
            .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;

        var roleName = await _db.Roles
            .AsNoTracking()
            .Where(r => r.Id == roleId)
            .Select(r => r.Name)
            .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;

        return fields
            .Select(f => new Application.Permissions.FieldPermissionListItemDto(
                roleId,
                roleName,
                documentTypeId,
                documentTypeName,
                f.Id,
                f.FieldKey,
                f.DisplayName,
                configured.TryGetValue(f.Id, out var pt) ? pt : null))
            .ToList();
    }

    public async Task SaveFieldPermissionsAsync(Application.Permissions.SaveFieldPermissionsRequestDto dto, CancellationToken cancellationToken = default)
    {
        // 校验角色与文档类型存在。
        var roleExists = await _db.Roles.AnyAsync(r => r.Id == dto.RoleId, cancellationToken);
        if (!roleExists) throw new Domain.Common.NotFoundException("Role", dto.RoleId);

        var docTypeExists = await _db.DocumentTypes.AnyAsync(dt => dt.Id == dto.DocumentTypeId, cancellationToken);
        if (!docTypeExists) throw new Domain.Common.NotFoundException("DocumentType", dto.DocumentTypeId);

        // 校验字段 Id 均属于该文档类型。
        var validFieldIds = await _db.DocumentFieldDefinitions
            .AsNoTracking()
            .Where(f => f.DocumentTypeId == dto.DocumentTypeId && f.IsActive && dto.FieldPermissions.Keys.Contains(f.Id))
            .Select(f => f.Id)
            .ToHashSetAsync(cancellationToken);

        // 全量覆盖：删除旧配置，写入新配置（仅写入有值的字段）。
        var existing = await _db.FieldPermissions
            .Where(fp => fp.RoleId == dto.RoleId && fp.DocumentTypeId == dto.DocumentTypeId)
            .ToListAsync(cancellationToken);
        _db.FieldPermissions.RemoveRange(existing);

        foreach (var (fieldId, permissionType) in dto.FieldPermissions)
        {
            if (!validFieldIds.Contains(fieldId)) continue;
            _db.FieldPermissions.Add(new FieldPermission(dto.RoleId, dto.DocumentTypeId, fieldId, permissionType));
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DocumentTypeDto>> GetDocumentTypesAsync(CancellationToken cancellationToken = default)
    {
        return await _db.DocumentTypes
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Code)
            .Select(x => new DocumentTypeDto(x.Id, x.Code, x.Name, x.NumberPrefix, x.IsActive))
            .ToListAsync(cancellationToken);
    }
}
