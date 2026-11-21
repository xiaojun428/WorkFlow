using Microsoft.EntityFrameworkCore;
using WorkflowSystem.Domain.Constants;
using WorkflowSystem.Domain.Entities.Document;
using WorkflowSystem.Domain.Entities.Identity;
using WorkflowSystem.Domain.Enums;

namespace WorkflowSystem.Infrastructure.Persistence.Seed;

/// <summary>
/// 开发环境种子数据。
/// 使用固定 Guid，保证 `HasData` 幂等。
/// 注意：仅用于 Development 环境，生产环境不得启用（由调用方控制）。
/// </summary>
public static class SeedData
{
    // 固定时间戳：种子数据必须使用确定值，否则每次模型快照都会产生 pending changes。
    private static readonly DateTime FixedNowUtc = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    // 固定 Guid（部门）。
    public static readonly Guid DeptIsd = new("00000000-0000-0000-0000-000000000001");
    public static readonly Guid DeptFinance = new("00000000-0000-0000-0000-000000000002");
    public static readonly Guid DeptQa = new("00000000-0000-0000-0000-000000000003");
    public static readonly Guid DeptPurchasing = new("00000000-0000-0000-0000-000000000004");

    // 固定 Guid（角色）。
    public static readonly Guid RoleUser = new("00000000-0000-0000-0000-000000000011");
    public static readonly Guid RoleApprover = new("00000000-0000-0000-0000-000000000012");
    public static readonly Guid RoleManager = new("00000000-0000-0000-0000-000000000013");
    public static readonly Guid RoleFinance = new("00000000-0000-0000-0000-000000000014");
    public static readonly Guid RoleQa = new("00000000-0000-0000-0000-000000000015");
    public static readonly Guid RolePurchasing = new("00000000-0000-0000-0000-000000000016");
    public static readonly Guid RoleWorkflowAdmin = new("00000000-0000-0000-0000-000000000017");
    public static readonly Guid RoleSystemAdmin = new("00000000-0000-0000-0000-000000000018");

    // 固定 Guid（文档类型）。
    public static readonly Guid DocTypeBudget = new("00000000-0000-0000-0000-000000000021");
    public static readonly Guid DocTypeEol = new("00000000-0000-0000-0000-000000000022");
    public static readonly Guid DocTypePurchase = new("00000000-0000-0000-0000-000000000023");
    public static readonly Guid DocTypeVendor = new("00000000-0000-0000-0000-000000000024");

    // 固定 Guid（测试用户）。
    public static readonly Guid UserAlice = new("00000000-0000-0000-0000-000000000031");
    public static readonly Guid UserBob = new("00000000-0000-0000-0000-000000000032");
    public static readonly Guid UserCarol = new("00000000-0000-0000-0000-000000000033");
    public static readonly Guid UserDave = new("00000000-0000-0000-0000-000000000034");
    public static readonly Guid UserEve = new("00000000-0000-0000-0000-000000000035");
    public static readonly Guid UserFrank = new("00000000-0000-0000-0000-000000000036");

    public static void Seed(ModelBuilder modelBuilder)
    {
        SeedDepartments(modelBuilder);
        SeedRoles(modelBuilder);
        SeedPermissions(modelBuilder);
        SeedRolePermissions(modelBuilder);
        SeedUsers(modelBuilder);
        SeedDocumentTypes(modelBuilder);
        SeedBudgetFieldDefinitions(modelBuilder);
    }

    private static void SeedDepartments(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Department>().HasData(
            new { Id = DeptIsd, Code = "ISD", Name = "信息系统部", ParentId = (Guid?)null, IsActive = true, IsDeleted = false, CreatedAtUtc = FixedNowUtc, UpdatedAtUtc = FixedNowUtc, DeletedAtUtc = (DateTime?)null },
            new { Id = DeptFinance, Code = "Finance", Name = "财务部", ParentId = (Guid?)null, IsActive = true, IsDeleted = false, CreatedAtUtc = FixedNowUtc, UpdatedAtUtc = FixedNowUtc, DeletedAtUtc = (DateTime?)null },
            new { Id = DeptQa, Code = "QA", Name = "品质部", ParentId = (Guid?)null, IsActive = true, IsDeleted = false, CreatedAtUtc = FixedNowUtc, UpdatedAtUtc = FixedNowUtc, DeletedAtUtc = (DateTime?)null },
            new { Id = DeptPurchasing, Code = "Purchasing", Name = "采购部", ParentId = (Guid?)null, IsActive = true, IsDeleted = false, CreatedAtUtc = FixedNowUtc, UpdatedAtUtc = FixedNowUtc, DeletedAtUtc = (DateTime?)null }
        );
    }

    private static void SeedRoles(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>().HasData(
            new { Id = RoleUser, Name = RoleNames.User, IsSystem = true },
            new { Id = RoleApprover, Name = RoleNames.Approver, IsSystem = true },
            new { Id = RoleManager, Name = RoleNames.Manager, IsSystem = true },
            new { Id = RoleFinance, Name = RoleNames.Finance, IsSystem = true },
            new { Id = RoleQa, Name = RoleNames.QA, IsSystem = true },
            new { Id = RolePurchasing, Name = RoleNames.Purchasing, IsSystem = true },
            new { Id = RoleWorkflowAdmin, Name = RoleNames.WorkflowAdmin, IsSystem = true },
            new { Id = RoleSystemAdmin, Name = RoleNames.SystemAdmin, IsSystem = true }
        );
    }

    private static void SeedPermissions(ModelBuilder modelBuilder)
    {
        var codes = new[]
        {
            PermissionCodes.Create, PermissionCodes.ViewOwn, PermissionCodes.ViewDepartment,
            PermissionCodes.ViewAll, PermissionCodes.Edit, PermissionCodes.Delete,
            PermissionCodes.Submit, PermissionCodes.Withdraw, PermissionCodes.Approve,
            PermissionCodes.Reject, PermissionCodes.Return, PermissionCodes.Transfer,
            PermissionCodes.DownloadAttachment, PermissionCodes.ManageWorkflow,
            PermissionCodes.ManagePermission, PermissionCodes.ViewAuditLog
        };

        var permissions = codes.Select((code, i) => new
        {
            Id = new Guid($"00000000-0000-0000-0000-{(i + 1):D12}"),
            Code = code,
            Description = string.Empty
        }).ToArray();

        modelBuilder.Entity<Permission>().HasData(permissions);
    }

    private static void SeedRolePermissions(ModelBuilder modelBuilder)
    {
        // 为演示，给 Manager 角色授予核心审批相关权限（其余权限在阶段 5 完整配置）。
        // 这里使用固定 Guid 保证幂等。
        var rolePermissions = new[]
        {
            new { RoleId = RoleManager, PermissionId = ApprovePermissionId },
            new { RoleId = RoleManager, PermissionId = RejectPermissionId },
            new { RoleId = RoleManager, PermissionId = ReturnPermissionId },
            new { RoleId = RoleManager, PermissionId = TransferPermissionId },
            new { RoleId = RoleManager, PermissionId = ViewDepartment },
        };

        var items = rolePermissions.Select((rp, i) => new
        {
            rp.RoleId,
            rp.PermissionId
        }).ToArray();

        modelBuilder.Entity<RolePermission>().HasData(items);
    }

    // 权限固定 Guid（与 SeedPermissions 中的顺序对应：Create=1, ViewOwn=2, ViewDepartment=3, ViewAll=4, Edit=5, Delete=6, Submit=7, Withdraw=8, Approve=9, Reject=10, Return=11, Transfer=12, DownloadAttachment=13, ManageWorkflow=14, ManagePermission=15, ViewAuditLog=16）。
    private static readonly Guid ApprovePermissionId = new("00000000-0000-0000-0000-000000000009");
    private static readonly Guid RejectPermissionId = new("00000000-0000-0000-0000-000000000010");
    private static readonly Guid ReturnPermissionId = new("00000000-0000-0000-0000-000000000011");
    private static readonly Guid TransferPermissionId = new("00000000-0000-0000-0000-000000000012");
    private static readonly Guid ViewDepartment = new("00000000-0000-0000-0000-000000000003");
    private static readonly Guid ViewOwn = new("00000000-0000-0000-0000-000000000002");
    private static readonly Guid CreatePermissionId = new("00000000-0000-0000-0000-000000000001");

    private static void SeedUsers(ModelBuilder modelBuilder)
    {
        // 测试用户（仅 Development）。EntraObjectId 用占位值，后续阶段接 Entra 时替换。
        modelBuilder.Entity<User>().HasData(
            new { Id = UserAlice, EntraObjectId = "dev-alice", Email = "alice@example.com", DisplayName = "Alice（申请人）", IsActive = true, IsDeleted = false, CreatedAtUtc = FixedNowUtc, UpdatedAtUtc = FixedNowUtc, DeletedAtUtc = (DateTime?)null },
            new { Id = UserBob, EntraObjectId = "dev-bob", Email = "bob@example.com", DisplayName = "Bob（Manager）", IsActive = true, IsDeleted = false, CreatedAtUtc = FixedNowUtc, UpdatedAtUtc = FixedNowUtc, DeletedAtUtc = (DateTime?)null },
            new { Id = UserCarol, EntraObjectId = "dev-carol", Email = "carol@example.com", DisplayName = "Carol（财务）", IsActive = true, IsDeleted = false, CreatedAtUtc = FixedNowUtc, UpdatedAtUtc = FixedNowUtc, DeletedAtUtc = (DateTime?)null },
            new { Id = UserDave, EntraObjectId = "dev-dave", Email = "dave@example.com", DisplayName = "Dave（QA）", IsActive = true, IsDeleted = false, CreatedAtUtc = FixedNowUtc, UpdatedAtUtc = FixedNowUtc, DeletedAtUtc = (DateTime?)null },
            new { Id = UserEve, EntraObjectId = "dev-eve", Email = "eve@example.com", DisplayName = "Eve（采购）", IsActive = true, IsDeleted = false, CreatedAtUtc = FixedNowUtc, UpdatedAtUtc = FixedNowUtc, DeletedAtUtc = (DateTime?)null },
            new { Id = UserFrank, EntraObjectId = "dev-frank", Email = "frank@example.com", DisplayName = "Frank（管理员）", IsActive = true, IsDeleted = false, CreatedAtUtc = FixedNowUtc, UpdatedAtUtc = FixedNowUtc, DeletedAtUtc = (DateTime?)null }
        );

        // 用户-部门映射。
        modelBuilder.Entity<UserDepartment>().HasData(
            new { UserId = UserAlice, DepartmentId = DeptIsd, IsPrimary = true },
            new { UserId = UserBob, DepartmentId = DeptIsd, IsPrimary = true },
            new { UserId = UserCarol, DepartmentId = DeptFinance, IsPrimary = true },
            new { UserId = UserDave, DepartmentId = DeptQa, IsPrimary = true },
            new { UserId = UserEve, DepartmentId = DeptPurchasing, IsPrimary = true },
            new { UserId = UserFrank, DepartmentId = DeptIsd, IsPrimary = true }
        );

        // 用户-角色映射。
        modelBuilder.Entity<UserRole>().HasData(
            new { Id = new Guid("00000000-0000-0000-0000-000000000041"), UserId = UserAlice, RoleId = RoleUser, ValidFromUtc = (DateTime?)null, ValidToUtc = (DateTime?)null },
            new { Id = new Guid("00000000-0000-0000-0000-000000000042"), UserId = UserBob, RoleId = RoleManager, ValidFromUtc = (DateTime?)null, ValidToUtc = (DateTime?)null },
            new { Id = new Guid("00000000-0000-0000-0000-000000000043"), UserId = UserCarol, RoleId = RoleFinance, ValidFromUtc = (DateTime?)null, ValidToUtc = (DateTime?)null },
            new { Id = new Guid("00000000-0000-0000-0000-000000000044"), UserId = UserDave, RoleId = RoleQa, ValidFromUtc = (DateTime?)null, ValidToUtc = (DateTime?)null },
            new { Id = new Guid("00000000-0000-0000-0000-000000000045"), UserId = UserEve, RoleId = RolePurchasing, ValidFromUtc = (DateTime?)null, ValidToUtc = (DateTime?)null },
            new { Id = new Guid("00000000-0000-0000-0000-000000000046"), UserId = UserFrank, RoleId = RoleSystemAdmin, ValidFromUtc = (DateTime?)null, ValidToUtc = (DateTime?)null }
        );
    }

    private static void SeedDocumentTypes(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DocumentType>().HasData(
            new { Id = DocTypeBudget, Code = DocumentTypeCodes.Budget, Name = "预算申请", NumberPrefix = "BG", IsActive = true, IsDeleted = false, DefaultWorkflowDefinitionId = (Guid?)null, CreatedAtUtc = FixedNowUtc, UpdatedAtUtc = FixedNowUtc, DeletedAtUtc = (DateTime?)null },
            new { Id = DocTypeEol, Code = DocumentTypeCodes.EOL, Name = "EOL 申请", NumberPrefix = "EOL", IsActive = true, IsDeleted = false, DefaultWorkflowDefinitionId = (Guid?)null, CreatedAtUtc = FixedNowUtc, UpdatedAtUtc = FixedNowUtc, DeletedAtUtc = (DateTime?)null },
            new { Id = DocTypePurchase, Code = DocumentTypeCodes.Purchase, Name = "采购申请", NumberPrefix = "PR", IsActive = true, IsDeleted = false, DefaultWorkflowDefinitionId = (Guid?)null, CreatedAtUtc = FixedNowUtc, UpdatedAtUtc = FixedNowUtc, DeletedAtUtc = (DateTime?)null },
            new { Id = DocTypeVendor, Code = DocumentTypeCodes.Vendor, Name = "供应商申请", NumberPrefix = "VD", IsActive = true, IsDeleted = false, DefaultWorkflowDefinitionId = (Guid?)null, CreatedAtUtc = FixedNowUtc, UpdatedAtUtc = FixedNowUtc, DeletedAtUtc = (DateTime?)null }
        );
    }

    private static void SeedBudgetFieldDefinitions(ModelBuilder modelBuilder)
    {
        // 预算申请的动态字段：金额（多币种）、币种、预算说明。
        modelBuilder.Entity<DocumentFieldDefinition>().HasData(
            new { Id = new Guid("00000000-0000-0000-0000-000000000051"), DocumentTypeId = DocTypeBudget, FieldKey = "Amount", DisplayName = "金额", FieldType = FieldType.Amount, IsRequired = true, DisplayOrder = 1, IsActive = true },
            new { Id = new Guid("00000000-0000-0000-0000-000000000052"), DocumentTypeId = DocTypeBudget, FieldKey = "Currency", DisplayName = "币种", FieldType = FieldType.Currency, IsRequired = true, DisplayOrder = 2, IsActive = true },
            new { Id = new Guid("00000000-0000-0000-0000-000000000053"), DocumentTypeId = DocTypeBudget, FieldKey = "Description", DisplayName = "预算说明", FieldType = FieldType.Text, IsRequired = false, DisplayOrder = 3, IsActive = true }
        );
    }
}
