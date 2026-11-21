using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkflowSystem.Application.Documents;
using WorkflowSystem.Application.Documents.Dtos;
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

    public DocumentController(IDocumentService documentService, IAuthorizationService authorizationService)
    {
        _documentService = documentService;
        _authorizationService = authorizationService;
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

        ViewBag.FieldDefinitions = await _documentService.GetFieldDefinitionsAsync(document.DocumentTypeId, cancellationToken);
        ViewBag.Document = document;
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
            ViewBag.FieldDefinitions = await _documentService.GetFieldDefinitionsAsync(document!.DocumentTypeId, cancellationToken);
            ViewBag.Document = document;
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
}
