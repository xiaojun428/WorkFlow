using WorkflowSystem.Domain.Enums;

namespace WorkflowSystem.Application.Documents.Dtos;

/// <summary>文档类型列表项。</summary>
public sealed record DocumentTypeDto(Guid Id, string Code, string Name, string NumberPrefix, bool IsActive);

/// <summary>文档字段定义 DTO。</summary>
public sealed record DocumentFieldDefinitionDto(
    Guid Id,
    string FieldKey,
    string DisplayName,
    FieldType FieldType,
    bool IsRequired,
    int DisplayOrder);

/// <summary>文档字段值 DTO（用于表单展示与提交）。</summary>
public sealed record DocumentFieldValueDto(
    Guid FieldDefinitionId,
    string FieldKey,
    string? ValueText,
    decimal? ValueNumber,
    DateTime? ValueDate,
    bool? ValueBoolean);

/// <summary>文档列表项（列表页展示，不含敏感字段）。</summary>
public sealed record DocumentListItemDto(
    Guid Id,
    string DocumentNo,
    string Title,
    Guid DocumentTypeId,
    string DocumentTypeName,
    Guid ApplicantUserId,
    string ApplicantName,
    Guid OwnerDepartmentId,
    string OwnerDepartmentName,
    DocumentStatus DocumentStatus,
    WorkflowStatus WorkflowStatus,
    int CurrentVersion,
    DateTime CreatedAtUtc);

/// <summary>文档详情 DTO（含动态字段值）。</summary>
public sealed record DocumentDetailDto(
    Guid Id,
    string DocumentNo,
    string Title,
    Guid DocumentTypeId,
    string DocumentTypeName,
    Guid ApplicantUserId,
    string ApplicantName,
    Guid OwnerDepartmentId,
    string OwnerDepartmentName,
    DocumentStatus DocumentStatus,
    WorkflowStatus WorkflowStatus,
    string? CurrentWorkflowStep,
    int CurrentVersion,
    DateTime? SubmittedAtUtc,
    DateTime? CompletedAtUtc,
    DateTime CreatedAtUtc,
    IReadOnlyList<DocumentFieldValueDto> FieldValues);

/// <summary>创建文档请求。</summary>
public sealed class CreateDocumentDto
{
    public Guid DocumentTypeId { get; set; }
    public string Title { get; set; } = string.Empty;
    public Guid OwnerDepartmentId { get; set; }
    public List<DocumentFieldValueInputDto> FieldValues { get; set; } = new();
}

/// <summary>更新文档请求（编辑草稿/退回文档）。</summary>
public sealed class UpdateDocumentDto
{
    public string Title { get; set; } = string.Empty;
    public List<DocumentFieldValueInputDto> FieldValues { get; set; } = new();
}

/// <summary>字段值输入（提交时由前端提供）。</summary>
public sealed class DocumentFieldValueInputDto
{
    public Guid FieldDefinitionId { get; set; }
    public string? ValueText { get; set; }
    public decimal? ValueNumber { get; set; }
    public DateTime? ValueDate { get; set; }
    public bool? ValueBoolean { get; set; }
}

/// <summary>文档附件 DTO（仅元数据，不含物理文件内容）。</summary>
public sealed record DocumentFileDto(
    Guid Id,
    string FileName,
    long SizeBytes,
    string MimeType,
    Guid UploadedByUserId,
    string UploadedByName,
    DateTime CreatedAtUtc);

/// <summary>文档查询参数（分页 + 筛选 + 排序）。</summary>
public sealed class DocumentQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public Guid? DocumentTypeId { get; set; }
    public DocumentStatus? Status { get; set; }
    public Guid? ApplicantUserId { get; set; }
    public Guid? OwnerDepartmentId { get; set; }
    public string? Keyword { get; set; }
    public string SortBy { get; set; } = "CreatedAtUtc";
    public bool SortDescending { get; set; } = true;
}

/// <summary>分页结果。</summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);
