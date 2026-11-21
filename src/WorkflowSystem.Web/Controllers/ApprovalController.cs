using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkflowSystem.Application.Approvals;
using WorkflowSystem.Application.Approvals.Dtos;
using WorkflowSystem.Application.Documents;
using WorkflowSystem.Application.Documents.Dtos;
using WorkflowSystem.Domain.Entities.Workflow;
using WorkflowSystem.Web.Authorization;

namespace WorkflowSystem.Web.Controllers;

/// <summary>
/// 审批控制器：待我审批、我已审批、审批操作（通过/驳回/退回/撤回/转交）。
/// 只负责接收请求、校验、调用应用服务；资源级授权由 ApprovalTaskAuthorizationHandler 保证。
/// </summary>
[Authorize]
public class ApprovalController : Controller
{
    private readonly IApprovalService _approvalService;
    private readonly IAuthorizationService _authorizationService;

    public ApprovalController(IApprovalService approvalService, IAuthorizationService authorizationService)
    {
        _approvalService = approvalService;
        _authorizationService = authorizationService;
    }

    // GET: /Approval/Pending —— 待我审批列表。
    [HttpGet]
    [Authorize(Policy = "permission:Approve")]
    public async Task<IActionResult> Pending(DocumentQuery query, CancellationToken cancellationToken)
    {
        var result = await _approvalService.GetPendingTasksAsync(query, cancellationToken);
        return View(result);
    }

    // GET: /Approval/Processed —— 我已审批列表。
    [HttpGet]
    public async Task<IActionResult> Processed(DocumentQuery query, CancellationToken cancellationToken)
    {
        var result = await _approvalService.GetProcessedTasksAsync(query, cancellationToken);
        return View(result);
    }

    // GET: /Approval/Details/{id} —— 审批任务详情（含文档与历史时间线）。
    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var detail = await _approvalService.GetTaskDetailAsync(id, cancellationToken);
        if (detail is null) return NotFound();
        return View(detail);
    }

    // POST: /Approval/Approve —— 审批通过。
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "permission:Approve")]
    public async Task<IActionResult> Approve(ApprovalActionDto dto, CancellationToken cancellationToken)
    {
        await _approvalService.ApproveAsync(dto, cancellationToken);
        return RedirectToAction(nameof(Pending));
    }

    // POST: /Approval/Reject —— 驳回。
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "permission:Reject")]
    public async Task<IActionResult> Reject(ApprovalActionDto dto, CancellationToken cancellationToken)
    {
        await _approvalService.RejectAsync(dto, cancellationToken);
        return RedirectToAction(nameof(Pending));
    }

    // POST: /Approval/Return —— 退回申请人。
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "permission:Return")]
    public async Task<IActionResult> Return(ApprovalActionDto dto, CancellationToken cancellationToken)
    {
        await _approvalService.ReturnAsync(dto, cancellationToken);
        return RedirectToAction(nameof(Pending));
    }

    // POST: /Approval/Transfer —— 转交。
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "permission:Transfer")]
    public async Task<IActionResult> Transfer(TransferRequestDto dto, CancellationToken cancellationToken)
    {
        await _approvalService.TransferAsync(dto, cancellationToken);
        return RedirectToAction(nameof(Pending));
    }

    // POST: /Approval/Withdraw —— 申请人撤回。
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "permission:Withdraw")]
    public async Task<IActionResult> Withdraw(Guid documentId, CancellationToken cancellationToken)
    {
        await _approvalService.WithdrawAsync(documentId, cancellationToken);
        return RedirectToAction("Index", "Document");
    }
}
