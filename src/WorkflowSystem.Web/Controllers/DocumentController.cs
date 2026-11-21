using Microsoft.AspNetCore.Mvc;
using WorkflowSystem.Application.Documents;
using WorkflowSystem.Application.Documents.Dtos;

namespace WorkflowSystem.Web.Controllers;

/// <summary>
/// 文档管理控制器。
/// 只负责：接收请求、校验 ModelState、调用应用服务、返回结果。
/// 业务逻辑不堆积在此，全部委托给 IDocumentService。
/// </summary>
public class DocumentController : Controller
{
    private readonly IDocumentService _documentService;

    public DocumentController(IDocumentService documentService)
    {
        _documentService = documentService;
    }

    // GET: /Document —— 文档列表（我的申请 / 全部，本阶段先做全部列表）。
    [HttpGet]
    public async Task<IActionResult> Index(DocumentQuery query, CancellationToken cancellationToken)
    {
        var result = await _documentService.QueryAsync(query, cancellationToken);
        return View(result);
    }

    // GET: /Document/Create —— 新建文档（草稿）。
    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var documentTypes = await _documentService.GetDocumentTypesAsync(cancellationToken);
        ViewBag.DocumentTypes = documentTypes;
        return View(new CreateDocumentDto());
    }

    // POST: /Document/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
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

    // GET: /Document/Details/{id} —— 文档详情。
    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var document = await _documentService.GetDocumentAsync(id, cancellationToken);
        if (document is null) return NotFound();
        return View(document);
    }

    // GET: /Document/Edit/{id} —— 编辑草稿/退回文档。
    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var document = await _documentService.GetDocumentAsync(id, cancellationToken);
        if (document is null) return NotFound();

        var model = new UpdateDocumentDto
        {
            Title = document.Title,
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

    // POST: /Document/Edit/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, UpdateDocumentDto dto, CancellationToken cancellationToken)
    {
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

    // POST: /Document/Delete/{id} —— 软删除草稿。
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _documentService.SoftDeleteAsync(id, cancellationToken);
        return RedirectToAction(nameof(Index));
    }
}
