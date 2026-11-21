using Microsoft.EntityFrameworkCore;
using WorkflowSystem.Domain.Entities.Audit;
using WorkflowSystem.Domain.Entities.Document;
using WorkflowSystem.Domain.Entities.Identity;
using WorkflowSystem.Domain.Entities.Notification;
using WorkflowSystem.Domain.Entities.Permission;
using WorkflowSystem.Domain.Entities.Workflow;

namespace WorkflowSystem.Infrastructure.Persistence;

/// <summary>
/// 系统业务数据库上下文。
/// 仅包含业务表；Elsa 内部表由 Elsa 自己的 DbContext 管理，两者互不依赖。
/// </summary>
public class WorkflowDbContext : DbContext
{
    public WorkflowDbContext(DbContextOptions<WorkflowDbContext> options) : base(options) { }

    // 身份与组织
    public DbSet<User> Users => Set<User>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<UserDepartment> UserDepartments => Set<UserDepartment>();
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<UserPosition> UserPositions => Set<UserPosition>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<DataScopeRule> DataScopeRules => Set<DataScopeRule>();

    // 文档
    public DbSet<DocumentType> DocumentTypes => Set<DocumentType>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentVersion> DocumentVersions => Set<DocumentVersion>();
    public DbSet<DocumentFieldDefinition> DocumentFieldDefinitions => Set<DocumentFieldDefinition>();
    public DbSet<DocumentFieldValue> DocumentFieldValues => Set<DocumentFieldValue>();
    public DbSet<DocumentFile> DocumentFiles => Set<DocumentFile>();
    public DbSet<DocumentNumberSequence> DocumentNumberSequences => Set<DocumentNumberSequence>();

    // 流程与审批
    public DbSet<WorkflowDefinition> WorkflowDefinitions => Set<WorkflowDefinition>();
    public DbSet<WorkflowDefinitionVersion> WorkflowDefinitionVersions => Set<WorkflowDefinitionVersion>();
    public DbSet<WorkflowDocumentMapping> WorkflowDocumentMappings => Set<WorkflowDocumentMapping>();
    public DbSet<WorkflowInstance> WorkflowInstances => Set<WorkflowInstance>();
    public DbSet<ApprovalTask> ApprovalTasks => Set<ApprovalTask>();
    public DbSet<ApprovalCandidate> ApprovalCandidates => Set<ApprovalCandidate>();
    public DbSet<ApprovalHistory> ApprovalHistories => Set<ApprovalHistory>();
    public DbSet<DelegationRule> DelegationRules => Set<DelegationRule>();

    // 权限、通知与审计
    public DbSet<FieldPermission> FieldPermissions => Set<FieldPermission>();
    public DbSet<MailTemplate> MailTemplates => Set<MailTemplate>();
    public DbSet<MailOutbox> MailOutbox => Set<MailOutbox>();
    public DbSet<MailHistory> MailHistories => Set<MailHistory>();
    public DbSet<ExchangeRate> ExchangeRates => Set<ExchangeRate>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // 应用所有 IEntityTypeConfiguration 配置。
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WorkflowDbContext).Assembly);

        // 开发环境种子数据（通过 HasData 静态种子，幂等）。
        Seed.SeedData.Seed(modelBuilder);

        base.OnModelCreating(modelBuilder);
    }
}
