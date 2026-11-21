using WorkflowSystem.Application.Documents.Dtos;

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

    // 列表查询
    Task<PagedResult<DocumentListItemDto>> QueryAsync(DocumentQuery query, CancellationToken cancellationToken = default);
}
