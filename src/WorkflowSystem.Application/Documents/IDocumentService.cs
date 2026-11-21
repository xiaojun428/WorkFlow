using WorkflowSystem.Application.Documents.Dtos;
using WorkflowSystem.Domain.Entities.Document;

namespace WorkflowSystem.Application.Documents;

/// <summary>
/// 文档应用服务：文档类型、文档、动态字段、附件的用例编排。
/// Controller 只调用此接口，业务逻辑不堆积在 Controller。
/// </summary>
public interface IDocumentService
{
    // 文档类型
    Task<IReadOnlyList<DocumentTypeDto>> GetDocumentTypesAsync(CancellationToken cancellationToken = default);
    Task<DocumentTypeDto?> GetDocumentTypeAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DocumentFieldDefinitionDto>> GetFieldDefinitionsAsync(Guid documentTypeId, CancellationToken cancellationToken = default);

    // 文档 CRUD
    Task<DocumentDetailDto> CreateDraftAsync(CreateDocumentDto dto, CancellationToken cancellationToken = default);
    Task<DocumentDetailDto?> GetDocumentAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DocumentDetailDto> UpdateDraftAsync(Guid id, UpdateDocumentDto dto, CancellationToken cancellationToken = default);
    Task SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 加载文档领域实体（供资源级授权使用，不返回 DTO）。
    /// 用于 Controller 在调用 IAuthorizationService 前取得资源对象。
    /// </summary>
    Task<Document?> GetDocumentEntityAsync(Guid id, CancellationToken cancellationToken = default);

    // 列表查询
    Task<PagedResult<DocumentListItemDto>> QueryAsync(DocumentQuery query, CancellationToken cancellationToken = default);

    // ===================== 附件 =====================

    /// <summary>查询文档的附件列表（元数据）。</summary>
    Task<IReadOnlyList<DocumentFileDto>> GetFilesAsync(Guid documentId, CancellationToken cancellationToken = default);

    /// <summary>上传附件：保存物理文件并写入附件元数据。</summary>
    Task<DocumentFileDto> UploadFileAsync(Guid documentId, Stream content, string fileName, string mimeType, CancellationToken cancellationToken = default);

    /// <summary>下载附件：返回物理文件流与元数据（文件名、MIME）。</summary>
    Task<(Stream Content, DocumentFileDto File)> DownloadFileAsync(Guid documentId, Guid fileId, CancellationToken cancellationToken = default);

    /// <summary>删除附件（软删除元数据，物理文件延迟删除）。</summary>
    Task DeleteFileAsync(Guid documentId, Guid fileId, CancellationToken cancellationToken = default);
}
