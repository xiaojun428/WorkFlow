using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkflowSystem.Domain.Entities.Workflow;
using WorkflowSystem.Domain.Enums;

namespace WorkflowSystem.Infrastructure.Persistence.Configurations;

public class WorkflowDefinitionConfiguration : IEntityTypeConfiguration<WorkflowDefinition>
{
    public void Configure(EntityTypeBuilder<WorkflowDefinition> builder)
    {
        builder.ToTable("WorkflowDefinitions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(128).IsRequired();
    }
}

public class WorkflowDefinitionVersionConfiguration : IEntityTypeConfiguration<WorkflowDefinitionVersion>
{
    public void Configure(EntityTypeBuilder<WorkflowDefinitionVersion> builder)
    {
        builder.ToTable("WorkflowDefinitionVersions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ElsaDefinitionId).HasMaxLength(128).IsRequired();

        builder.HasOne(x => x.WorkflowDefinition).WithMany(x => x.Versions).HasForeignKey(x => x.WorkflowDefinitionId).OnDelete(DeleteBehavior.Cascade);

        // 定义 + 版本号唯一。
        builder.HasIndex(x => new { x.WorkflowDefinitionId, x.VersionNo }).IsUnique();
    }
}

public class WorkflowDocumentMappingConfiguration : IEntityTypeConfiguration<WorkflowDocumentMapping>
{
    public void Configure(EntityTypeBuilder<WorkflowDocumentMapping> builder)
    {
        builder.ToTable("WorkflowDocumentMappings");
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.DocumentType).WithMany().HasForeignKey(x => x.DocumentTypeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.WorkflowDefinition).WithMany().HasForeignKey(x => x.WorkflowDefinitionId).OnDelete(DeleteBehavior.Restrict);

        // 一种文档类型只绑定一个工作流定义（唯一）。
        builder.HasIndex(x => x.DocumentTypeId).IsUnique();
    }
}

public class WorkflowInstanceConfiguration : IEntityTypeConfiguration<WorkflowInstance>
{
    public void Configure(EntityTypeBuilder<WorkflowInstance> builder)
    {
        builder.ToTable("WorkflowInstances");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ElsaInstanceId).HasMaxLength(128).IsRequired();
        builder.Property(x => x.CorrelationId).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Status).HasConversion<int>();

        builder.HasOne(x => x.Document).WithMany().HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.WorkflowDefinitionVersion).WithMany().HasForeignKey(x => x.WorkflowDefinitionVersionId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.DocumentId);
        builder.HasIndex(x => x.CorrelationId).IsUnique();
    }
}

public class ApprovalTaskConfiguration : IEntityTypeConfiguration<ApprovalTask>
{
    public void Configure(EntityTypeBuilder<ApprovalTask> builder)
    {
        builder.ToTable("ApprovalTasks");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.StepKey).HasMaxLength(128).IsRequired();
        builder.Property(x => x.StepName).HasMaxLength(128).IsRequired();
        builder.Property(x => x.AssignmentType).HasConversion<int>();
        builder.Property(x => x.Status).HasConversion<int>();

        // RowVersion 乐观并发：防止同一任务被并发重复审批。
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasOne(x => x.Document).WithMany().HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.AssignedUser).WithMany().HasForeignKey(x => x.AssignedUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.AssignedRole).WithMany().HasForeignKey(x => x.AssignedRoleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.AssignedDepartment).WithMany().HasForeignKey(x => x.AssignedDepartmentId).OnDelete(DeleteBehavior.Restrict);

        // 待我审批、按文档查任务等常用查询。
        builder.HasIndex(x => new { x.AssignedUserId, x.Status });
        builder.HasIndex(x => x.DocumentId);
    }
}

public class ApprovalCandidateConfiguration : IEntityTypeConfiguration<ApprovalCandidate>
{
    public void Configure(EntityTypeBuilder<ApprovalCandidate> builder)
    {
        builder.ToTable("ApprovalCandidates");
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.ApprovalTask).WithMany(x => x.Candidates).HasForeignKey(x => x.ApprovalTaskId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.CandidateUser).WithMany().HasForeignKey(x => x.CandidateUserId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.ApprovalTaskId);
    }
}

public class ApprovalHistoryConfiguration : IEntityTypeConfiguration<ApprovalHistory>
{
    public void Configure(EntityTypeBuilder<ApprovalHistory> builder)
    {
        builder.ToTable("ApprovalHistory");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Action).HasConversion<int>();
        builder.Property(x => x.FromStatus).HasMaxLength(64);
        builder.Property(x => x.ToStatus).HasMaxLength(64);
        builder.Property(x => x.Comment).HasColumnType("nvarchar(max)");

        builder.HasOne(x => x.Document).WithMany().HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.OperatorUser).WithMany().HasForeignKey(x => x.OperatorUserId).OnDelete(DeleteBehavior.Restrict);

        // 审批时间线查询。
        builder.HasIndex(x => new { x.DocumentId, x.OccurredAtUtc });
    }
}

public class DelegationRuleConfiguration : IEntityTypeConfiguration<DelegationRule>
{
    public void Configure(EntityTypeBuilder<DelegationRule> builder)
    {
        builder.ToTable("DelegationRules");
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.DelegatorUser).WithMany().HasForeignKey(x => x.DelegatorUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.DelegateUser).WithMany().HasForeignKey(x => x.DelegateUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.DocumentType).WithMany().HasForeignKey(x => x.DocumentTypeId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.DelegatorUserId);
    }
}
