using WorkflowSystem.Domain.Common;
using WorkflowSystem.Domain.Entities.Document;
using WorkflowSystem.Domain.Entities.Permission;
using WorkflowSystem.Domain.Enums;
using Xunit;

namespace WorkflowSystem.UnitTests;

/// <summary>
/// 附件与字段权限领域规则测试：验证实体构造约束（阶段 12）。
/// </summary>
public class DocumentFileAndFieldPermissionTests
{
    private static readonly Guid DocId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public void DocumentFile_Constructs_WithValidArguments()
    {
        var file = new DocumentFile(DocId, "合同.pdf", "abc123.pdf", 1024, "application/pdf", "HASH", UserId);

        Assert.Equal(DocId, file.DocumentId);
        Assert.Equal("合同.pdf", file.FileName);
        Assert.Equal("abc123.pdf", file.StorageKey);
        Assert.Equal(1024, file.SizeBytes);
        Assert.Equal("application/pdf", file.MimeType);
        Assert.Equal("HASH", file.Sha256Hash);
        Assert.Equal(UserId, file.UploadedByUserId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void DocumentFile_WhenFileNameEmpty_Throws(string fileName)
    {
        Assert.Throws<BusinessRuleViolationException>(() =>
            new DocumentFile(DocId, fileName, "key", 1, "application/pdf", "HASH", UserId));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void DocumentFile_WhenStorageKeyEmpty_Throws(string storageKey)
    {
        Assert.Throws<BusinessRuleViolationException>(() =>
            new DocumentFile(DocId, "a.pdf", storageKey, 1, "application/pdf", "HASH", UserId));
    }

    [Fact]
    public void FieldPermission_Constructs_WithValidArguments()
    {
        var roleId = Guid.NewGuid();
        var documentTypeId = Guid.NewGuid();
        var fieldDefinitionId = Guid.NewGuid();

        var fp = new FieldPermission(roleId, documentTypeId, fieldDefinitionId, FieldPermissionType.ReadOnly);

        Assert.Equal(roleId, fp.RoleId);
        Assert.Equal(documentTypeId, fp.DocumentTypeId);
        Assert.Equal(fieldDefinitionId, fp.FieldDefinitionId);
        Assert.Equal(FieldPermissionType.ReadOnly, fp.PermissionType);
    }
}
