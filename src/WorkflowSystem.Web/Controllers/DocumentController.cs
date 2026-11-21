using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkflowSystem.Application.Documents;
using WorkflowSystem.Application.Documents.Dtos;
using WorkflowSystem.Application.Permissions;
using WorkflowSystem.Domain.Common;
using WorkflowSystem.Domain.Enums;
using WorkflowSystem.Web.Authorization;

namespace WorkflowSystem.Web.Controllers;

/// <summary>
/// 文档管理控制器。
/// 只负责：接收请求、校验 ModelState、调用应用服务、返回结果。
/// 权限通过 [Authorize(Policy=...)]（粗粒度）+ 资源级 AuthorizationHandler（细粒度）双重校验。
/// </summary>
[Authorize]
public class DocumentController : Controller
{
    private readonly IDocumentService _documentService;
    private readonly IAuthorizationService _authorizationService;
    private readonly IPermissionService _permissionService;

    public DocumentController(IDocumentService documentService, IAuthorizationService authorizationService, IPermissionService permissionService)
    {
        _documentService = documentService;
        _authorizationService = authorizationService;
        _permissionService = permissionService;
    }

    // GET: /Document —— 文档列表。
    [HttpGet]
    [Authorize(Policy = "permission:ViewOwn")]
    public async Task<IActionResult> Index(DocumentQuery query, CancellationToken cancellationToken)
    {
        // 列表按数据范围过滤在应用服务内完成（阶段 5 后补充），此处先返回全部可访问项。
        var result = await _documentService.QueryAsync(query, cancellationToken);
        return View(result);
    }

    // GET: /Document/Create —— 新建文档（草稿）。
    [HttpGet]
    [Authorize(Policy = "permission:Create")]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var documentTypes = await _documentService.GetDocumentTypesAsync(cancellationToken);
        ViewBag.DocumentTypes = documentTypes;
        return View(new CreateDocumentDto());
    }

    // GET: /Document/FieldDefinitions?documentTypeId=xxx —— 按类型返回字段定义（供前端动态渲染）。
    [HttpGet]
    [Authorize(Policy = "permission:Create")]
    public async Task<IActionResult> FieldDefinitions(Guid documentTypeId, CancellationToken cancellationToken)
    {
        var fields = await _documentService.GetFieldDefinitionsAsync(documentTypeId, cancellationToken);
        return Json(fields);
    }

    // POST: /Document/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "permission:Create")]
    public async Task<IActionResult> Create(CreateDocumentDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var types = await _documentService.GetDocumentTypesAsync(cancellationToken);
            ViewBag.DocumentTypes = types;
            return View(dto);
        }

        var created = await _documentService.CreateDraftAsync(dto, cancellationToken);
        return RedirectToAction(nameof(Details), new { id = created.Id });
    }

    // GET: /Document/Details/{id} —— 文档详情（资源级授权）。
    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var document = await _documentService.GetDocumentEntityAsync(id, cancellationToken);
        if (document is null) return NotFound();

        // 资源级授权：查看权限。
        var auth = await _authorizationService.AuthorizeAsync(User, document, new DocumentOperationRequirement("View"));
        if (!auth.Succeeded) return Forbid();

        var detail = await _documentService.GetDocumentAsync(id, cancellationToken);

        // 加载字段定义（用于显示中文名）与字段权限（用于裁剪 Hidden 字段）。
        var fieldDefinitions = await _documentService.GetFieldDefinitionsAsync(detail!.DocumentTypeId, cancellationToken);
        ViewBag.FieldDefinitions = fieldDefinitions;
        ViewBag.FieldPermissions = await LoadFieldPermissionsAsync(detail.DocumentTypeId, fieldDefinitions, cancellationToken);

        // 加载附件列表。
        ViewBag.Files = await _documentService.GetFilesAsync(id, cancellationToken);

        return View(detail);
    }

    // GET: /Document/Edit/{id} —— 编辑草稿/退回文档（资源级授权）。
    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var documentEntity = await _documentService.GetDocumentEntityAsync(id, cancellationToken);
        if (documentEntity is null) return NotFound();

        var auth = await _authorizationService.AuthorizeAsync(User, documentEntity, new DocumentOperationRequirement("Edit"));
        if (!auth.Succeeded) return Forbid();

        var document = await _documentService.GetDocumentAsync(id, cancellationToken);
        var model = new UpdateDocumentDto
        {
            Title = document!.Title,
            FieldValues = document.FieldValues.Select(x => new DocumentFieldValueInputDto
            {
                FieldDefinitionId = x.FieldDefinitionId,
                ValueText = x.ValueText,
                ValueNumber = x.ValueNumber,
                ValueDate = x.ValueDate,
                ValueBoolean = x.ValueBoolean,
            }).ToList(),
        };

        var fieldDefinitions = await _documentService.GetFieldDefinitionsAsync(document.DocumentTypeId, cancellationToken);
        ViewBag.FieldDefinitions = fieldDefinitions;
        ViewBag.Document = document;
        ViewBag.FieldPermissions = await LoadFieldPermissionsAsync(document.DocumentTypeId, fieldDefinitions, cancellationToken);
        return View(model);
    }

    // POST: /Document/Edit/{id}（资源级授权 + Anti-forgery）。
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, UpdateDocumentDto dto, CancellationToken cancellationToken)
    {
        var documentEntity = await _documentService.GetDocumentEntityAsync(id, cancellationToken);
        if (documentEntity is null) return NotFound();

        var auth = await _authorizationService.AuthorizeAsync(User, documentEntity, new DocumentOperationRequirement("Edit"));
        if (!auth.Succeeded) return Forbid();

        if (!ModelState.IsValid)
        {
            var document = await _documentService.GetDocumentAsync(id, cancellationToken);
            var fieldDefinitions = await _documentService.GetFieldDefinitionsAsync(document!.DocumentTypeId, cancellationToken);
            ViewBag.FieldDefinitions = fieldDefinitions;
            ViewBag.Document = document;
            ViewBag.FieldPermissions = await LoadFieldPermissionsAsync(document.DocumentTypeId, fieldDefinitions, cancellationToken);
            return View(dto);
        }

        await _documentService.UpdateDraftAsync(id, dto, cancellationToken);
        return RedirectToAction(nameof(Details), new { id });
    }

    // POST: /Document/Delete/{id} —— 软删除草稿（资源级授权）。
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var documentEntity = await _documentService.GetDocumentEntityAsync(id, cancellationToken);
        if (documentEntity is null) return NotFound();

        var auth = await _authorizationService.AuthorizeAsync(User, documentEntity, new DocumentOperationRequirement("Delete"));
        if (!auth.Succeeded) return Forbid();

        await _documentService.SoftDeleteAsync(id, cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    // ===================== 附件 =====================

    // POST: /Document/UploadFile —— 上传附件（资源级授权 + Anti-forgery）。
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadFile(Guid documentId, IFormFile file, CancellationToken cancellationToken)
    {
        var documentEntity = await _documentService.GetDocumentEntityAsync(documentId, cancellationToken);
        if (documentEntity is null) return NotFound();

        var auth = await _authorizationService.AuthorizeAsync(User, documentEntity, new DocumentOperationRequirement("Edit"));
        if (!auth.Succeeded) return Forbid();

        if (file is null || file.Length == 0)
        {
            TempData["Error"] = "请选择要上传的文件。";
            return RedirectToAction(nameof(Details), new { id = documentId });
        }

        try
        {
            await using var stream = file.OpenReadStream();
            await _documentService.UploadFileAsync(documentId, stream, file.FileName, file.ContentType, cancellationToken);
            TempData["Success"] = "附件上传成功。";
        }
        catch (BusinessRuleViolationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Details), new { id = documentId });
    }

    // GET: /Document/DownloadFile?documentId=xxx&fileId=yyy —— 下载附件（资源级授权）。
    [HttpGet]
    public async Task<IActionResult> DownloadFile(Guid documentId, Guid fileId, CancellationToken cancellationToken)
    {
        var documentEntity = await _documentService.GetDocumentEntityAsync(documentId, cancellationToken);
        if (documentEntity is null) return NotFound();

        var auth = await _authorizationService.AuthorizeAsync(User, documentEntity, new DocumentOperationRequirement("View"));
        if (!auth.Succeeded) return Forbid();

        var (content, file) = await _documentService.DownloadFileAsync(documentId, fileId, cancellationToken);
        return File(content, file.MimeType, file.FileName);
    }

    // POST: /Document/DeleteFile —— 删除附件（资源级授权 + Anti-forgery）。
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteFile(Guid documentId, Guid fileId, CancellationToken cancellationToken)
    {
        var documentEntity = await _documentService.GetDocumentEntityAsync(documentId, cancellationToken);
        if (documentEntity is null) return NotFound();

        var auth = await _authorizationService.AuthorizeAsync(User, documentEntity, new DocumentOperationRequirement("Edit"));
        if (!auth.Succeeded) return Forbid();

        try
        {
            await _documentService.DeleteFileAsync(documentId, fileId, cancellationToken);
            TempData["Success"] = "附件已删除。";
        }
        catch (BusinessRuleViolationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Details), new { id = documentId });
    }

    /// <summary>批量加载字段权限（视图层字段裁剪用，避免 N+1）。</summary>
    private async Task<IReadOnlyDictionary<Guid, FieldPermissionType>> LoadFieldPermissionsAsync(
        Guid documentTypeId,
        IReadOnlyList<DocumentFieldDefinitionDto> fieldDefinitions,
        CancellationToken cancellationToken)
    {
        return await _permissionService.GetFieldPermissionsAsync(
            documentTypeId,
            fieldDefinitions.Select(f => f.Id),
            cancellationToken);
    }
}
