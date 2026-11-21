using Microsoft.EntityFrameworkCore;
using WorkflowSystem.Application.Abstractions;
using WorkflowSystem.Application.Permissions;
using WorkflowSystem.Domain.Entities.Identity;
using WorkflowSystem.Domain.Enums;
using WorkflowSystem.Infrastructure.Persistence;

namespace WorkflowSystem.Infrastructure.Services;

/// <summary>
/// 权限服务实现：基于 EF Core 计算用户角色、操作权限、数据范围与字段权限。
/// 所有判定均在后端完成，不信任前端传入的任何权限声明。
/// </summary>
public class PermissionService : IPermissionService
{
    private readonly WorkflowDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IClock _clock;

    public PermissionService(WorkflowDbContext db, ICurrentUserService currentUser, IClock clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<PermissionContext> GetCurrentUserContextAsync(CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId;
        if (userId is null)
            return new PermissionContext(); // 未认证：空权限。

        var now = _clock.UtcNow;

        // 1) 有效角色（含有效期校验）。
        var userRoles = await _db.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == userId.Value)
            .Include(ur => ur.Role)
            .ToListAsync(cancellationToken);

        var activeRoleNames = userRoles
            .Where(ur => ur.IsActiveAt(now))
            .Select(ur => ur.Role!.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var activeRoleIds = userRoles
            .Where(ur => ur.IsActiveAt(now))
            .Select(ur => ur.RoleId)
            .ToHashSet();

        // 2) 操作权限码（经 RolePermission → Permission）。
        var permissionCodes = await _db.RolePermissions
            .AsNoTracking()
            .Where(rp => activeRoleIds.Contains(rp.RoleId))
            .Join(_db.Permissions, rp => rp.PermissionId, p => p.Id, (rp, p) => p.Code)
            .Distinct()
            .ToListAsync(cancellationToken);

        // 3) 部门（含兼任）。
        var departmentIds = await _db.UserDepartments
            .AsNoTracking()
            .Where(ud => ud.UserId == userId.Value)
            .Select(ud => ud.DepartmentId)
            .ToListAsync(cancellationToken);

        // 4) 数据范围：取最宽松（All > Department > Self）。
        var scopeTypes = await _db.DataScopeRules
            .AsNoTracking()
            .Where(r => activeRoleIds.Contains(r.RoleId))
            .Select(r => r.ScopeType)
            .ToListAsync(cancellationToken);

        var effectiveScope = DataScopeType.Self;
        if (scopeTypes.Contains(DataScopeType.All)) effectiveScope = DataScopeType.All;
        else if (scopeTypes.Contains(DataScopeType.Department)) effectiveScope = DataScopeType.Department;

        return new PermissionContext
        {
            RoleNames = activeRoleNames,
            PermissionCodes = permissionCodes.ToHashSet(StringComparer.OrdinalIgnoreCase),
            DepartmentIds = departmentIds.ToHashSet(),
            EffectiveScope = effectiveScope,
        };
    }

    public async Task<bool> HasPermissionAsync(string permissionCode, CancellationToken cancellationToken = default)
    {
        var ctx = await GetCurrentUserContextAsync(cancellationToken);
        return ctx.HasPermission(permissionCode);
    }

    public async Task<bool> IsInRoleAsync(string roleName, CancellationToken cancellationToken = default)
    {
        var ctx = await GetCurrentUserContextAsync(cancellationToken);
        return ctx.HasRole(roleName);
    }

    public async Task<FieldPermissionType?> GetFieldPermissionAsync(
        Guid documentTypeId,
        Guid fieldDefinitionId,
        CancellationToken cancellationToken = default)
    {
        var ctx = await GetCurrentUserContextAsync(cancellationToken);
        var roleIds = await _db.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == _currentUser.UserId)
            .Select(ur => ur.RoleId)
            .ToListAsync(cancellationToken);

        // 查询当前用户所有角色在该字段上的权限，取最严格（Hidden 最严格，其次 ReadOnly，再次 Required，最宽松 Editable）。
        var permissions = await _db.FieldPermissions
            .AsNoTracking()
            .Where(fp => fp.DocumentTypeId == documentTypeId
                         && fp.FieldDefinitionId == fieldDefinitionId
                         && roleIds.Contains(fp.RoleId))
            .Select(fp => fp.PermissionType)
            .ToListAsync(cancellationToken);

        if (permissions.Count == 0) return null;

        // 优先级：Hidden(0) 最严格。枚举值越小越严格，取最小值即最严格权限。
        return permissions.Min();
    }

    public async Task<IReadOnlyDictionary<Guid, FieldPermissionType>> GetFieldPermissionsAsync(
        Guid documentTypeId,
        IEnumerable<Guid> fieldDefinitionIds,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId;
        if (userId is null)
            return new Dictionary<Guid, FieldPermissionType>();

        var fieldIds = fieldDefinitionIds.ToList();
        if (fieldIds.Count == 0)
            return new Dictionary<Guid, FieldPermissionType>();

        var roleIds = await _db.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == userId.Value)
            .Select(ur => ur.RoleId)
            .ToListAsync(cancellationToken);

        var rows = await _db.FieldPermissions
            .AsNoTracking()
            .Where(fp => fp.DocumentTypeId == documentTypeId
                         && fieldIds.Contains(fp.FieldDefinitionId)
                         && roleIds.Contains(fp.RoleId))
            .Select(fp => new { fp.FieldDefinitionId, fp.PermissionType })
            .ToListAsync(cancellationToken);

        // 每个字段取最严格权限（枚举值最小者）。
        return rows
            .GroupBy(x => x.FieldDefinitionId)
            .ToDictionary(g => g.Key, g => g.Min(x => x.PermissionType));
    }
}
