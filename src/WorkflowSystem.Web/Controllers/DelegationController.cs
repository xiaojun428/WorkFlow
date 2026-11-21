using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkflowSystem.Application.Delegations;
using WorkflowSystem.Application.Delegations.Dtos;
using WorkflowSystem.Application.Documents;

namespace WorkflowSystem.Web.Controllers;

/// <summary>
/// 代理委托控制器：管理委托审批规则（列表、新建、停用）。
/// 委托审批规则带有效期，代理期内委托人与被委托人可并行审批。
/// </summary>
[Authorize]
public class DelegationController : Controller
{
    private readonly IDelegationService _delegationService;
    private readonly IDocumentService _documentService;

    public DelegationController(IDelegationService delegationService, IDocumentService documentService)
    {
        _delegationService = delegationService;
        _documentService = documentService;
    }

    // GET: /Delegation —— 我的委托规则列表。
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var delegations = await _delegationService.GetMyDelegationsAsync(cancellationToken);
        return View(delegations);
    }

    // GET: /Delegation/Create —— 新建委托。
    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        await LoadCreateViewDataAsync(cancellationToken);
        return View(new DelegationRequestDto
        {
            ValidFrom = DateTime.Today,
            ValidTo = DateTime.Today.AddDays(7),
        });
    }

    // POST: /Delegation/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DelegationRequestDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await LoadCreateViewDataAsync(cancellationToken);
            return View(dto);
        }

        await _delegationService.CreateAsync(dto, cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    // POST: /Delegation/Deactivate —— 停用委托。
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        await _delegationService.DeactivateAsync(id, cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadCreateViewDataAsync(CancellationToken cancellationToken)
    {
        ViewBag.DocumentTypes = await _documentService.GetDocumentTypesAsync(cancellationToken);
        ViewBag.Delegates = await _delegationService.GetAvailableDelegatesAsync(cancellationToken);
    }
}
