using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkflowSystem.Domain.Entities.Document;
using WorkflowSystem.Domain.Enums;

namespace WorkflowSystem.Infrastructure.Persistence.Configurations;

public class DocumentTypeConfiguration : IEntityTypeConfiguration<DocumentType>
{
    public void Configure(EntityTypeBuilder<DocumentType> builder)
    {
        builder.ToTable("DocumentTypes");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(128).IsRequired();
        builder.Property(x => x.NumberPrefix).HasMaxLength(32).IsRequired();

        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable("Documents");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.DocumentNo).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(256).IsRequired();
        builder.Property(x => x.DocumentStatus).HasConversion<int>();
        builder.Property(x => x.WorkflowStatus).HasConversion<int>();
        builder.Property(x => x.CurrentWorkflowStep).HasMaxLength(128);

        // RowVersion 乐观并发控制。
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasOne(x => x.DocumentType).WithMany().HasForeignKey(x => x.DocumentTypeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ApplicantUser).WithMany().HasForeignKey(x => x.ApplicantUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.OwnerDepartment).WithMany().HasForeignKey(x => x.OwnerDepartmentId).OnDelete(DeleteBehavior.Restrict);

        // DocumentNo 唯一索引。
        builder.HasIndex(x => x.DocumentNo).IsUnique();

        // 常用查询复合索引。
        builder.HasIndex(x => new { x.ApplicantUserId, x.DocumentStatus });
        builder.HasIndex(x => new { x.OwnerDepartmentId, x.DocumentStatus });
        builder.HasIndex(x => new { x.DocumentTypeId, x.CreatedAtUtc });

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class DocumentVersionConfiguration : IEntityTypeConfiguration<DocumentVersion>
{
    public void Configure(EntityTypeBuilder<DocumentVersion> builder)
    {
        builder.ToTable("DocumentVersions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SnapshotJson).HasColumnType("nvarchar(max)");
        builder.Property(x => x.ChangeReason).HasMaxLength(512);

        builder.HasOne(x => x.Document).WithMany(x => x.Versions).HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => x.DocumentId);
    }
}

public class DocumentFieldDefinitionConfiguration : IEntityTypeConfiguration<DocumentFieldDefinition>
{
    public void Configure(EntityTypeBuilder<DocumentFieldDefinition> builder)
    {
        builder.ToTable("DocumentFieldDefinitions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.FieldKey).HasMaxLength(128).IsRequired();
        builder.Property(x => x.DisplayName).HasMaxLength(128).IsRequired();
        builder.Property(x => x.FieldType).HasConversion<int>();

        builder.HasOne(x => x.DocumentType).WithMany(x => x.FieldDefinitions).HasForeignKey(x => x.DocumentTypeId).OnDelete(DeleteBehavior.Cascade);

        // 同一文档类型内 FieldKey 唯一。
        builder.HasIndex(x => new { x.DocumentTypeId, x.FieldKey }).IsUnique();
    }
}

public class DocumentFieldValueConfiguration : IEntityTypeConfiguration<DocumentFieldValue>
{
    public void Configure(EntityTypeBuilder<DocumentFieldValue> builder)
    {
        builder.ToTable("DocumentFieldValues");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.FieldValueText).HasColumnType("nvarchar(max)");
        builder.Property(x => x.FieldValueNumber).HasPrecision(18, 4);

        builder.HasOne(x => x.Document).WithMany(x => x.FieldValues).HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.FieldDefinition).WithMany().HasForeignKey(x => x.FieldDefinitionId).OnDelete(DeleteBehavior.Restrict);

        // 按文档查询字段值。
        builder.HasIndex(x => x.DocumentId);
        // 支持按字段定义查询（条件分支、报表）。
        builder.HasIndex(x => x.FieldDefinitionId);
    }
}

public class DocumentFileConfiguration : IEntityTypeConfiguration<DocumentFile>
{
    public void Configure(EntityTypeBuilder<DocumentFile> builder)
    {
        builder.ToTable("DocumentFiles");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.FileName).HasMaxLength(256).IsRequired();
        builder.Property(x => x.StorageKey).HasMaxLength(512).IsRequired();
        builder.Property(x => x.MimeType).HasMaxLength(128);
        builder.Property(x => x.Sha256Hash).HasMaxLength(64).IsRequired();

        builder.HasOne(x => x.Document).WithMany(x => x.Files).HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.UploadedByUser).WithMany().HasForeignKey(x => x.UploadedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.DocumentId);
        builder.HasIndex(x => x.UploadedByUserId);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class DocumentNumberSequenceConfiguration : IEntityTypeConfiguration<DocumentNumberSequence>
{
    public void Configure(EntityTypeBuilder<DocumentNumberSequence> builder)
    {
        builder.ToTable("DocumentNumberSequences");
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.DocumentType).WithMany().HasForeignKey(x => x.DocumentTypeId).OnDelete(DeleteBehavior.Restrict);

        // 类型+年+月唯一，保证并发下编号不重复。
        builder.HasIndex(x => new { x.DocumentTypeId, x.Year, x.Month }).IsUnique();
    }
}
