using WorkflowSystem.Domain.Common;
using WorkflowSystem.Domain.Enums;

namespace WorkflowSystem.Domain.Entities.Identity;

/// <summary>
/// 用户。Entra Object ID 作为外部身份稳定标识（唯一），Email 仅作可变属性。
/// 内部角色、部门、权限由本系统维护。
/// </summary>
public class User : SoftDeletableEntity
{
    public string EntraObjectId { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;

    private readonly List<UserDepartment> _userDepartments = new();
    private readonly List<UserRole> _userRoles = new();
    private readonly List<UserPosition> _userPositions = new();

    public IReadOnlyCollection<UserDepartment> UserDepartments => _userDepartments;
    public IReadOnlyCollection<UserRole> UserRoles => _userRoles;
    public IReadOnlyCollection<UserPosition> UserPositions => _userPositions;

    private User() { }

    public User(string entraObjectId, string email, string displayName)
    {
        if (string.IsNullOrWhiteSpace(entraObjectId))
            throw new BusinessRuleViolationException("EntraObjectId 不能为空。");
        if (string.IsNullOrWhiteSpace(email))
            throw new BusinessRuleViolationException("Email 不能为空。");

        EntraObjectId = entraObjectId;
        Email = email;
        DisplayName = displayName;
    }

    public void UpdateProfile(string email, string displayName)
    {
        Email = string.IsNullOrWhiteSpace(email) ? Email : email;
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? DisplayName : displayName;
        MarkUpdated();
    }

    public void SetActive(bool active)
    {
        IsActive = active;
        MarkUpdated();
    }
}

/// <summary>
/// 部门，支持自引用树形结构。
/// </summary>
public class Department : SoftDeletableEntity
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public Guid? ParentId { get; private set; }
    public Department? Parent { get; private set; }
    public bool IsActive { get; private set; } = true;

    private readonly List<Department> _children = new();
    public IReadOnlyCollection<Department> Children => _children;

    private Department() { }

    public Department(string code, string name, Guid? parentId = null)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new BusinessRuleViolationException("部门编码不能为空。");
        if (string.IsNullOrWhiteSpace(name)) throw new BusinessRuleViolationException("部门名称不能为空。");
        Code = code;
        Name = name;
        ParentId = parentId;
    }
}

/// <summary>用户-部门（支持兼任，IsPrimary 标记主部门）。复合主键，无独立 Id。</summary>
public class UserDepartment
{
    public Guid UserId { get; private set; }
    public Guid DepartmentId { get; private set; }
    public bool IsPrimary { get; private set; }

    public User? User { get; private set; }
    public Department? Department { get; private set; }

    private UserDepartment() { }

    public UserDepartment(Guid userId, Guid departmentId, bool isPrimary = false)
    {
        UserId = userId;
        DepartmentId = departmentId;
        IsPrimary = isPrimary;
    }
}

/// <summary>岗位。</summary>
public class Position : Entity
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;

    private Position() { }

    public Position(string code, string name)
    {
        Code = code;
        Name = name;
    }
}

/// <summary>用户-岗位（岗位绑定到具体部门）。</summary>
public class UserPosition : Entity
{
    public Guid UserId { get; private set; }
    public Guid PositionId { get; private set; }
    public Guid DepartmentId { get; private set; }

    public User? User { get; private set; }
    public Position? Position { get; private set; }
    public Department? Department { get; private set; }

    private UserPosition() { }

    public UserPosition(Guid userId, Guid positionId, Guid departmentId)
    {
        UserId = userId;
        PositionId = positionId;
        DepartmentId = departmentId;
    }
}

/// <summary>
/// 角色。角色与岗位、部门、用户是不同概念，不得混淆。
/// </summary>
public class Role : Entity
{
    public string Name { get; private set; } = string.Empty;
    public bool IsSystem { get; private set; }

    private readonly List<RolePermission> _rolePermissions = new();
    public IReadOnlyCollection<RolePermission> RolePermissions => _rolePermissions;

    private Role() { }

    public Role(string name, bool isSystem = false)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new BusinessRuleViolationException("角色名不能为空。");
        Name = name;
        IsSystem = isSystem;
    }
}

/// <summary>用户-角色（带有效期，支持角色过期）。</summary>
public class UserRole : Entity
{
    public Guid UserId { get; private set; }
    public Guid RoleId { get; private set; }
    public DateTime? ValidFromUtc { get; private set; }
    public DateTime? ValidToUtc { get; private set; }

    public User? User { get; private set; }
    public Role? Role { get; private set; }

    private UserRole() { }

    public UserRole(Guid userId, Guid roleId, DateTime? validFromUtc = null, DateTime? validToUtc = null)
    {
        UserId = userId;
        RoleId = roleId;
        ValidFromUtc = validFromUtc;
        ValidToUtc = validToUtc;
    }

    /// <summary>判断角色在当前时间是否有效。</summary>
    public bool IsActiveAt(DateTime nowUtc)
    {
        if (ValidFromUtc.HasValue && nowUtc < ValidFromUtc.Value) return false;
        if (ValidToUtc.HasValue && nowUtc > ValidToUtc.Value) return false;
        return true;
    }
}

/// <summary>
/// 权限（操作权限的原子定义，如 Create、Approve、DownloadAttachment 等）。
/// </summary>
public class Permission : Entity
{
    public string Code { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;

    private Permission() { }

    public Permission(string code, string description = "")
    {
        if (string.IsNullOrWhiteSpace(code)) throw new BusinessRuleViolationException("权限编码不能为空。");
        Code = code;
        Description = description;
    }
}

/// <summary>角色-权限关联。复合主键，无独立 Id。</summary>
public class RolePermission
{
    public Guid RoleId { get; private set; }
    public Guid PermissionId { get; private set; }

    public Role? Role { get; private set; }
    public Permission? Permission { get; private set; }

    private RolePermission() { }

    public RolePermission(Guid roleId, Guid permissionId)
    {
        RoleId = roleId;
        PermissionId = permissionId;
    }
}

/// <summary>数据范围规则（角色在某数据范围下可见的数据范围）。</summary>
public class DataScopeRule : Entity
{
    public Guid RoleId { get; private set; }
    public DataScopeType ScopeType { get; private set; }
    public Guid? DepartmentId { get; private set; }

    public Role? Role { get; private set; }
    public Department? Department { get; private set; }

    private DataScopeRule() { }

    public DataScopeRule(Guid roleId, DataScopeType scopeType, Guid? departmentId = null)
    {
        RoleId = roleId;
        ScopeType = scopeType;
        DepartmentId = departmentId;
    }
}
