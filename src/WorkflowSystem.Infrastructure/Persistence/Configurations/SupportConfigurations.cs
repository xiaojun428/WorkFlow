using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkflowSystem.Domain.Entities.Audit;
using WorkflowSystem.Domain.Entities.Notification;
using WorkflowSystem.Domain.Entities.Permission;
using WorkflowSystem.Domain.Enums;

namespace WorkflowSystem.Infrastructure.Persistence.Configurations;

public class FieldPermissionConfiguration : IEntityTypeConfiguration<FieldPermission>
{
    public void Configure(EntityTypeBuilder<FieldPermission> builder)
    {
        builder.ToTable("FieldPermissions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.PermissionType).HasConversion<int>();

        builder.HasOne(x => x.Role).WithMany().HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.DocumentType).WithMany().HasForeignKey(x => x.DocumentTypeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.FieldDefinition).WithMany().HasForeignKey(x => x.FieldDefinitionId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.DocumentTypeId, x.FieldDefinitionId });
    }
}

public class MailTemplateConfiguration : IEntityTypeConfiguration<MailTemplate>
{
    public void Configure(EntityTypeBuilder<MailTemplate> builder)
    {
        builder.ToTable("MailTemplates");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code).HasMaxLength(64).IsRequired();
        builder.Property(x => x.SubjectTemplate).HasColumnType("nvarchar(max)");
        builder.Property(x => x.BodyTemplate).HasColumnType("nvarchar(max)");

        builder.HasIndex(x => x.Code).IsUnique();
    }
}

public class MailOutboxConfiguration : IEntityTypeConfiguration<MailOutbox>
{
    public void Configure(EntityTypeBuilder<MailOutbox> builder)
    {
        builder.ToTable("MailOutbox");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ToAddress).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Subject).HasColumnType("nvarchar(max)");
        builder.Property(x => x.Body).HasColumnType("nvarchar(max)");
        builder.Property(x => x.Status).HasConversion<int>();
        builder.Property(x => x.CorrelationId).HasMaxLength(128);
        builder.Property(x => x.IdempotencyKey).HasMaxLength(128).IsRequired();

        // 幂等键唯一：防止重复发送。
        builder.HasIndex(x => x.IdempotencyKey).IsUnique();
        // 后台服务扫描待发送/待重试邮件。
        builder.HasIndex(x => new { x.Status, x.NextRetryAtUtc });
    }
}

public class MailHistoryConfiguration : IEntityTypeConfiguration<MailHistory>
{
    public void Configure(EntityTypeBuilder<MailHistory> builder)
    {
        builder.ToTable("MailHistory");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Status).HasConversion<int>();
        builder.Property(x => x.ErrorMessage).HasColumnType("nvarchar(max)");
        builder.Property(x => x.MessageId).HasMaxLength(256);

        builder.HasOne(x => x.Outbox).WithMany().HasForeignKey(x => x.OutboxId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => x.OutboxId);
    }
}

public class ExchangeRateConfiguration : IEntityTypeConfiguration<ExchangeRate>
{
    public void Configure(EntityTypeBuilder<ExchangeRate> builder)
    {
        builder.ToTable("ExchangeRates");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.FromCurrency).HasConversion<int>();
        builder.Property(x => x.ToCurrency).HasConversion<int>();
        builder.Property(x => x.Rate).HasPrecision(18, 6);

        // 同一货币对 + 生效时间唯一。
        builder.HasIndex(x => new { x.FromCurrency, x.ToCurrency, x.EffectiveFromUtc });
    }
}

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Action).HasMaxLength(128).IsRequired();
        builder.Property(x => x.EntityType).HasMaxLength(128).IsRequired();
        builder.Property(x => x.DetailJson).HasColumnType("nvarchar(max)");
        builder.Property(x => x.CorrelationId).HasMaxLength(128);
        builder.Property(x => x.IpAddress).HasMaxLength(64);

        // 按实体查询审计、按用户时间查询。
        builder.HasIndex(x => new { x.EntityType, x.EntityId });
        builder.HasIndex(x => new { x.UserId, x.OccurredAtUtc });
    }
}
