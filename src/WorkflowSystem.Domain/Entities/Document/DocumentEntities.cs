using WorkflowSystem.Domain.Common;
using WorkflowSystem.Domain.Entities.Identity;
using WorkflowSystem.Domain.Enums;

namespace WorkflowSystem.Domain.Entities.Document;

/// <summary>
/// 文档类型：定义一种文档的元数据（编号前缀、默认流程等）。
/// 不同文档类型拥有不同字段定义、不同流程、不同权限。
/// </summary>
public class DocumentType : SoftDeletableEntity
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string NumberPrefix { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public Guid? DefaultWorkflowDefinitionId { get; private set; }

    private readonly List<DocumentFieldDefinition> _fieldDefinitions = new();
    public IReadOnlyCollection<DocumentFieldDefinition> FieldDefinitions => _fieldDefinitions;

    private DocumentType() { }

    public DocumentType(string code, string name, string numberPrefix)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new BusinessRuleViolationException("文档类型编码不能为空。");
        if (string.IsNullOrWhiteSpace(name)) throw new BusinessRuleViolationException("文档类型名称不能为空。");
        Code = code;
        Name = name;
        NumberPrefix = string.IsNullOrWhiteSpace(numberPrefix) ? code : numberPrefix;
    }

    public void AddFieldDefinition(DocumentFieldDefinition fieldDefinition)
        => _fieldDefinitions.Add(fieldDefinition);
}

/// <summary>
/// 文档字段定义：定义文档类型下的动态字段（类型、是否必填、显示顺序等）。
/// </summary>
public class DocumentFieldDefinition : Entity
{
    public Guid DocumentTypeId { get; private set; }
    public string FieldKey { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public FieldType FieldType { get; private set; }
    public bool IsRequired { get; private set; }
    public int DisplayOrder { get; private set; }
    public bool IsActive { get; private set; } = true;

    public DocumentType? DocumentType { get; private set; }

    private DocumentFieldDefinition() { }

    public DocumentFieldDefinition(Guid documentTypeId, string fieldKey, string displayName, FieldType fieldType, bool isRequired = false, int displayOrder = 0)
    {
        if (string.IsNullOrWhiteSpace(fieldKey)) throw new BusinessRuleViolationException("字段 Key 不能为空。");
        DocumentTypeId = documentTypeId;
        FieldKey = fieldKey;
        DisplayName = displayName;
        FieldType = fieldType;
        IsRequired = isRequired;
        DisplayOrder = displayOrder;
    }
}

/// <summary>
/// 文档：核心业务实体。公共固定字段 + 动态字段值分离存储。
/// 文档状态(DocumentStatus)与工作流状态(WorkflowStatus)分离。
/// </summary>
public class Document : SoftDeletableEntity
{
    public string DocumentNo { get; private set; } = string.Empty;
    public Guid DocumentTypeId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public Guid ApplicantUserId { get; private set; }
    public Guid OwnerDepartmentId { get; private set; }

    public DocumentStatus DocumentStatus { get; private set; } = DocumentStatus.Draft;
    public WorkflowStatus WorkflowStatus { get; private set; } = WorkflowStatus.NotStarted;
    public string? CurrentWorkflowStep { get; private set; }
    public Guid? WorkflowInstanceId { get; private set; }

    public int CurrentVersion { get; private set; } = 1;

    public DateTime? SubmittedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }

    /// <summary>乐观并发控制（SQL Server rowversion）。</summary>
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    public DocumentType? DocumentType { get; private set; }
    public User? ApplicantUser { get; private set; }
    public Department? OwnerDepartment { get; private set; }

    private readonly List<DocumentVersion> _versions = new();
    private readonly List<DocumentFieldValue> _fieldValues = new();
    private readonly List<DocumentFile> _files = new();

    public IReadOnlyCollection<DocumentVersion> Versions => _versions;
    public IReadOnlyCollection<DocumentFieldValue> FieldValues => _fieldValues;
    public IReadOnlyCollection<DocumentFile> Files => _files;

    private Document() { }

    public Document(string documentNo, Guid documentTypeId, string title, Guid applicantUserId, Guid ownerDepartmentId)
    {
        if (string.IsNullOrWhiteSpace(documentNo)) throw new BusinessRuleViolationException("文档编号不能为空。");
        if (string.IsNullOrWhiteSpace(title)) throw new BusinessRuleViolationException("标题不能为空。");
        DocumentNo = documentNo;
        DocumentTypeId = documentTypeId;
        Title = title;
        ApplicantUserId = applicantUserId;
        OwnerDepartmentId = ownerDepartmentId;
    }

    public void UpdateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new BusinessRuleViolationException("标题不能为空。");
        Title = title;
        MarkUpdated();
    }

    /// <summary>提交申请：仅草稿/退回状态可提交，防止重复提交。</summary>
    public void Submit()
    {
        if (DocumentStatus is not (DocumentStatus.Draft or DocumentStatus.Returned))
            throw new BusinessRuleViolationException("只有草稿或退回状态的文档才能提交。");

        DocumentStatus = DocumentStatus.InApproval;
        WorkflowStatus = WorkflowStatus.Running;
        SubmittedAtUtc = DateTime.UtcNow;
        MarkUpdated();
    }

    public void SetWorkflowInstance(Guid workflowInstanceId)
    {
        WorkflowInstanceId = workflowInstanceId;
        MarkUpdated();
    }

    public void UpdateCurrentStep(string? stepKey)
    {
        CurrentWorkflowStep = stepKey;
        MarkUpdated();
    }

    public void MarkApproved() => MarkCompleted(DocumentStatus.Approved);

    public void MarkRejected() => MarkCompleted(DocumentStatus.Rejected);

    public void MarkWithdrawn() => MarkCompleted(DocumentStatus.Withdrawn);

    public void MarkCompletedFlow() => MarkCompleted(DocumentStatus.Completed);

    private void MarkCompleted(DocumentStatus finalStatus)
    {
        DocumentStatus = finalStatus;
        WorkflowStatus = WorkflowStatus.Completed;
        CompletedAtUtc = DateTime.UtcNow;
        MarkUpdated();
    }

    /// <summary>退回申请人修改。</summary>
    public void MarkReturned()
    {
        DocumentStatus = DocumentStatus.Returned;
        WorkflowStatus = WorkflowStatus.Running;
        MarkUpdated();
    }

    /// <summary>增加版本号（提交后关键字段变更时调用）。</summary>
    public void IncrementVersion() => CurrentVersion++;
}

/// <summary>
/// 文档版本快照：提交后关键字段变更时记录版本或审计。
/// </summary>
public class DocumentVersion : Entity
{
    public Guid DocumentId { get; private set; }
    public int VersionNo { get; private set; }
    public string SnapshotJson { get; private set; } = "{}";
    public string ChangeReason { get; private set; } = string.Empty;
    public Guid CreatedByUserId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;

    public Document? Document { get; private set; }

    private DocumentVersion() { }

    public DocumentVersion(Guid documentId, int versionNo, string snapshotJson, Guid createdByUserId, string changeReason = "")
    {
        DocumentId = documentId;
        VersionNo = versionNo;
        SnapshotJson = snapshotJson;
        CreatedByUserId = createdByUserId;
        ChangeReason = changeReason;
    }
}

/// <summary>
/// 文档字段值：动态字段的实际存储（混合模式：强类型列便于查询与条件判断）。
/// </summary>
public class DocumentFieldValue : Entity
{
    public Guid DocumentId { get; private set; }
    public Guid FieldDefinitionId { get; private set; }
    public string? FieldValueText { get; private set; }
    public decimal? FieldValueNumber { get; private set; }
    public DateTime? FieldValueDate { get; private set; }
    public bool? FieldValueBoolean { get; private set; }

    public Document? Document { get; private set; }
    public DocumentFieldDefinition? FieldDefinition { get; private set; }

    private DocumentFieldValue() { }

    public DocumentFieldValue(Guid documentId, Guid fieldDefinitionId)
    {
        DocumentId = documentId;
        FieldDefinitionId = fieldDefinitionId;
    }

    public void SetText(string? value) => FieldValueText = value;
    public void SetNumber(decimal? value) => FieldValueNumber = value;
    public void SetDate(DateTime? value) => FieldValueDate = value;
    public void SetBoolean(bool? value) => FieldValueBoolean = value;
}

/// <summary>
/// 附件元数据：只保存元数据与存储键，物理文件通过 IFileStorageService 存取。
/// </summary>
public class DocumentFile : SoftDeletableEntity
{
    public Guid DocumentId { get; private set; }
    public string FileName { get; private set; } = string.Empty;
    public string StorageKey { get; private set; } = string.Empty;
    public long SizeBytes { get; private set; }
    public string MimeType { get; private set; } = string.Empty;
    public string Sha256Hash { get; private set; } = string.Empty;
    public Guid UploadedByUserId { get; private set; }

    public Document? Document { get; private set; }
    public User? UploadedByUser { get; private set; }

    private DocumentFile() { }

    public DocumentFile(Guid documentId, string fileName, string storageKey, long sizeBytes, string mimeType, string sha256Hash, Guid uploadedByUserId)
    {
        if (string.IsNullOrWhiteSpace(fileName)) throw new BusinessRuleViolationException("文件名不能为空。");
        if (string.IsNullOrWhiteSpace(storageKey)) throw new BusinessRuleViolationException("存储键不能为空。");
        DocumentId = documentId;
        FileName = fileName;
        StorageKey = storageKey;
        SizeBytes = sizeBytes;
        MimeType = mimeType;
        Sha256Hash = sha256Hash;
        UploadedByUserId = uploadedByUserId;
    }
}

/// <summary>
/// 文档编号序列：保证并发下编号不重复（类型码-YYYY-NNNN，每年重置）。
/// </summary>
public class DocumentNumberSequence : Entity
{
    public Guid DocumentTypeId { get; private set; }
    public int Year { get; private set; }
    public int Month { get; private set; }
    public long LastSequenceNo { get; private set; }

    public DocumentType? DocumentType { get; private set; }

    private DocumentNumberSequence() { }

    public DocumentNumberSequence(Guid documentTypeId, int year, int month)
    {
        DocumentTypeId = documentTypeId;
        Year = year;
        Month = month;
        LastSequenceNo = 0;
    }

    public long Next() => ++LastSequenceNo;
}
