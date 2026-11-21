using Microsoft.EntityFrameworkCore;
using WorkflowSystem.Application.Abstractions;
using WorkflowSystem.Application.Documents;
using WorkflowSystem.Application.Documents.Dtos;
using WorkflowSystem.Domain.Common;
using WorkflowSystem.Domain.Entities.Document;
using WorkflowSystem.Domain.Enums;
using WorkflowSystem.Infrastructure.Persistence;

namespace WorkflowSystem.Infrastructure.Services;

/// <summary>
/// 文档应用服务实现：文档类型、文档、动态字段的用例编排。
/// 所有数据库操作使用异步 API，并传递 CancellationToken。
/// 注意：本阶段为"基础管理"，尚未接入权限校验（阶段 5 完成权限模型）。
/// </summary>
public class DocumentService : IDocumentService
{
    private readonly WorkflowDbContext _db;
    private readonly IDocumentNumberService _numberService;
    private readonly ICurrentUserService _currentUser;
    private readonly IClock _clock;

    public DocumentService(
        WorkflowDbContext db,
        IDocumentNumberService numberService,
        ICurrentUserService currentUser,
        IClock clock)
    {
        _db = db;
        _numberService = numberService;
        _currentUser = currentUser;
        _clock = clock;
    }

    // ===================== 文档类型 =====================

    public async Task<IReadOnlyList<DocumentTypeDto>> GetDocumentTypesAsync(CancellationToken cancellationToken = default)
    {
        return await _db.DocumentTypes
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Code)
            .Select(x => new DocumentTypeDto(x.Id, x.Code, x.Name, x.NumberPrefix, x.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<DocumentTypeDto?> GetDocumentTypeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.DocumentTypes
            .AsNoTracking()
            .Where(x => x.Id == id && x.IsActive)
            .Select(x => new DocumentTypeDto(x.Id, x.Code, x.Name, x.NumberPrefix, x.IsActive))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DocumentFieldDefinitionDto>> GetFieldDefinitionsAsync(Guid documentTypeId, CancellationToken cancellationToken = default)
    {
        return await _db.DocumentFieldDefinitions
            .AsNoTracking()
            .Where(x => x.DocumentTypeId == documentTypeId && x.IsActive)
            .OrderBy(x => x.DisplayOrder)
            .Select(x => new DocumentFieldDefinitionDto(x.Id, x.FieldKey, x.DisplayName, x.FieldType, x.IsRequired, x.DisplayOrder))
            .ToListAsync(cancellationToken);
    }

    // ===================== 文档 CRUD =====================

    public async Task<DocumentDetailDto> CreateDraftAsync(CreateDocumentDto dto, CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId
            ?? throw new BusinessRuleViolationException("当前用户未认证，无法创建文档。");

        var documentType = await _db.DocumentTypes
            .FirstOrDefaultAsync(x => x.Id == dto.DocumentTypeId && x.IsActive, cancellationToken)
            ?? throw new NotFoundException("DocumentType", dto.DocumentTypeId);

        // 校验必填字段。
        await ValidateRequiredFieldsAsync(dto.DocumentTypeId, dto.FieldValues, cancellationToken);

        // 生成文档编号（并发安全）。
        var documentNo = await _numberService.GenerateAsync(dto.DocumentTypeId, cancellationToken);

        var document = new Document(documentNo, dto.DocumentTypeId, dto.Title, userId, dto.OwnerDepartmentId);
        _db.Documents.Add(document);

        // 写入动态字段值。
        SetFieldValues(document.Id, dto.FieldValues);

        await _db.SaveChangesAsync(cancellationToken);

        return await GetDocumentAsync(document.Id, cancellationToken)
            ?? throw new BusinessRuleViolationException("文档创建后读取失败。");
    }

    public async Task<DocumentDetailDto?> GetDocumentAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var document = await _db.Documents
            .AsNoTracking()
            .Include(x => x.DocumentType)
            .Include(x => x.ApplicantUser)
            .Include(x => x.OwnerDepartment)
            .Include(x => x.FieldValues).ThenInclude(x => x.FieldDefinition)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (document is null) return null;

        var fieldValues = document.FieldValues
            .OrderBy(x => x.FieldDefinition?.DisplayOrder ?? 0)
            .Select(x => new DocumentFieldValueDto(
                x.FieldDefinitionId,
                x.FieldDefinition?.FieldKey ?? string.Empty,
                x.FieldValueText,
                x.FieldValueNumber,
                x.FieldValueDate,
                x.FieldValueBoolean))
            .ToList();

        return new DocumentDetailDto(
            document.Id,
            document.DocumentNo,
            document.Title,
            document.DocumentTypeId,
            document.DocumentType?.Name ?? string.Empty,
            document.ApplicantUserId,
            document.ApplicantUser?.DisplayName ?? string.Empty,
            document.OwnerDepartmentId,
            document.OwnerDepartment?.Name ?? string.Empty,
            document.DocumentStatus,
            document.WorkflowStatus,
            document.CurrentWorkflowStep,
            document.CurrentVersion,
            document.SubmittedAtUtc,
            document.CompletedAtUtc,
            document.CreatedAtUtc,
            fieldValues);
    }

    public async Task<DocumentDetailDto> UpdateDraftAsync(Guid id, UpdateDocumentDto dto, CancellationToken cancellationToken = default)
    {
        var document = await _db.Documents
            .Include(x => x.FieldValues)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Document", id);

        // 仅草稿或退回状态可编辑（业务规则）。
        if (document.DocumentStatus is not (DocumentStatus.Draft or DocumentStatus.Returned))
            throw new BusinessRuleViolationException("只有草稿或退回状态的文档才能编辑。");

        document.UpdateTitle(dto.Title);

        // 校验必填字段。
        await ValidateRequiredFieldsAsync(document.DocumentTypeId, dto.FieldValues, cancellationToken);

        // 更新动态字段值。
        var existing = document.FieldValues.ToDictionary(x => x.FieldDefinitionId);
        foreach (var input in dto.FieldValues)
        {
            if (existing.TryGetValue(input.FieldDefinitionId, out var fieldValue))
            {
                SetFieldValue(fieldValue, input);
            }
            else
            {
                var newValue = new DocumentFieldValue(document.Id, input.FieldDefinitionId);
                SetFieldValue(newValue, input);
                _db.DocumentFieldValues.Add(newValue);
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        return await GetDocumentAsync(id, cancellationToken)
            ?? throw new NotFoundException("Document", id);
    }

    public async Task SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var document = await _db.Documents
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Document", id);

        // 已提交或已完成文档不得由普通用户删除（本阶段先统一禁止删除非草稿）。
        if (document.DocumentStatus != DocumentStatus.Draft)
            throw new BusinessRuleViolationException("只有草稿状态的文档才能删除。");

        document.SoftDelete();
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<Document?> GetDocumentEntityAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.Documents
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    // ===================== 列表查询 =====================

    public async Task<PagedResult<DocumentListItemDto>> QueryAsync(DocumentQuery query, CancellationToken cancellationToken = default)
    {
        var q = _db.Documents.AsNoTracking().AsQueryable();

        // 软删除数据不出现在正常查询中（HasQueryFilter 已处理，此处冗余保护）。
        if (query.DocumentTypeId.HasValue)
            q = q.Where(x => x.DocumentTypeId == query.DocumentTypeId.Value);
        if (query.Status.HasValue)
            q = q.Where(x => x.DocumentStatus == query.Status.Value);
        if (query.ApplicantUserId.HasValue)
            q = q.Where(x => x.ApplicantUserId == query.ApplicantUserId.Value);
        if (query.OwnerDepartmentId.HasValue)
            q = q.Where(x => x.OwnerDepartmentId == query.OwnerDepartmentId.Value);
        if (!string.IsNullOrWhiteSpace(query.Keyword))
            q = q.Where(x => x.Title.Contains(query.Keyword) || x.DocumentNo.Contains(query.Keyword));

        var totalCount = await q.CountAsync(cancellationToken);

        // 排序（白名单列，防止注入非法排序列）。
        q = query.SortBy switch
        {
            "DocumentNo" => query.SortDescending ? q.OrderByDescending(x => x.DocumentNo) : q.OrderBy(x => x.DocumentNo),
            "Title" => query.SortDescending ? q.OrderByDescending(x => x.Title) : q.OrderBy(x => x.Title),
            "DocumentStatus" => query.SortDescending ? q.OrderByDescending(x => x.DocumentStatus) : q.OrderBy(x => x.DocumentStatus),
            _ => query.SortDescending ? q.OrderByDescending(x => x.CreatedAtUtc) : q.OrderBy(x => x.CreatedAtUtc),
        };

        var items = await q
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(x => new DocumentListItemDto(
                x.Id,
                x.DocumentNo,
                x.Title,
                x.DocumentTypeId,
                x.DocumentType != null ? x.DocumentType.Name : string.Empty,
                x.ApplicantUserId,
                x.ApplicantUser != null ? x.ApplicantUser.DisplayName : string.Empty,
                x.OwnerDepartmentId,
                x.OwnerDepartment != null ? x.OwnerDepartment.Name : string.Empty,
                x.DocumentStatus,
                x.WorkflowStatus,
                x.CurrentVersion,
                x.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return new PagedResult<DocumentListItemDto>(items, totalCount, query.Page, query.PageSize);
    }

    // ===================== 私有辅助 =====================

    /// <summary>校验必填字段。</summary>
    private async Task ValidateRequiredFieldsAsync(Guid documentTypeId, List<DocumentFieldValueInputDto> inputs, CancellationToken cancellationToken)
    {
        var requiredFields = await _db.DocumentFieldDefinitions
            .AsNoTracking()
            .Where(x => x.DocumentTypeId == documentTypeId && x.IsActive && x.IsRequired)
            .ToListAsync(cancellationToken);

        foreach (var field in requiredFields)
        {
            var input = inputs.FirstOrDefault(x => x.FieldDefinitionId == field.Id);
            var hasValue = input != null && HasValue(input);
            if (!hasValue)
                throw new BusinessRuleViolationException($"字段「{field.DisplayName}」为必填项。");
        }
    }

    private static bool HasValue(DocumentFieldValueInputDto input)
        => input.ValueText != null || input.ValueNumber.HasValue || input.ValueDate.HasValue || input.ValueBoolean.HasValue;

    private static void SetFieldValue(DocumentFieldValue fieldValue, DocumentFieldValueInputDto input)
    {
        fieldValue.SetText(input.ValueText);
        fieldValue.SetNumber(input.ValueNumber);
        fieldValue.SetDate(input.ValueDate);
        fieldValue.SetBoolean(input.ValueBoolean);
    }

    private void SetFieldValues(Guid documentId, List<DocumentFieldValueInputDto> inputs)
    {
        foreach (var input in inputs)
        {
            var fieldValue = new DocumentFieldValue(documentId, input.FieldDefinitionId);
            SetFieldValue(fieldValue, input);
            _db.DocumentFieldValues.Add(fieldValue);
        }
    }
}
