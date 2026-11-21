using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkflowSystem.Application.Approvals;
using WorkflowSystem.Application.Approvals.Dtos;
using WorkflowSystem.Application.Documents;
using WorkflowSystem.Application.Documents.Dtos;
using WorkflowSystem.Domain.Entities.Workflow;
using WorkflowSystem.Infrastructure.Persistence;
using WorkflowSystem.Web.Authorization;

namespace WorkflowSystem.Web.Controllers;

/// <summary>
/// 审批控制器：待我审批、我已审批、审批操作（通过/驳回/退回/退回上一步/撤回/转交）。
/// 只负责接收请求、校验、调用应用服务；资源级授权由 ApprovalTaskAuthorizationHandler 保证。
/// </summary>
[Authorize]
public class ApprovalController : Controller
{
    private readonly IApprovalService _approvalService;
    private readonly IAuthorizationService _authorizationService;
    private readonly WorkflowDbContext _db;

    public ApprovalController(IApprovalService approvalService, IAuthorizationService authorizationService, WorkflowDbContext db)
    {
        _approvalService = approvalService;
        _authorizationService = authorizationService;
        _db = db;
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
        if (!await AuthorizeTaskAsync(dto.ApprovalTaskId, "Approve", cancellationToken))
            return Forbid();
        await _approvalService.ApproveAsync(dto, cancellationToken);
        return RedirectToAction(nameof(Pending));
    }

    // POST: /Approval/BatchApprove —— 批量审批通过。
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "permission:Approve")]
    public async Task<IActionResult> BatchApprove(BatchApproveRequestDto dto, CancellationToken cancellationToken)
    {
        var result = await _approvalService.BatchApproveAsync(dto, cancellationToken);

        // 用 TempData 汇总结果，重定向回待审批列表展示。
        TempData["BatchSuccessCount"] = result.SuccessCount.ToString();
        TempData["BatchFailCount"] = result.FailCount.ToString();
        TempData["BatchFailDetails"] = string.Join("; ",
            result.Items.Where(x => !x.Success)
                .Select(x => $"{x.DocumentNo}：{x.Error}"));

        return RedirectToAction(nameof(Pending));
    }

    // POST: /Approval/Reject —— 驳回。
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "permission:Reject")]
    public async Task<IActionResult> Reject(ApprovalActionDto dto, CancellationToken cancellationToken)
    {
        if (!await AuthorizeTaskAsync(dto.ApprovalTaskId, "Reject", cancellationToken))
            return Forbid();
        await _approvalService.RejectAsync(dto, cancellationToken);
        return RedirectToAction(nameof(Pending));
    }

    // POST: /Approval/Return —— 退回申请人。
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "permission:Return")]
    public async Task<IActionResult> Return(ApprovalActionDto dto, CancellationToken cancellationToken)
    {
        if (!await AuthorizeTaskAsync(dto.ApprovalTaskId, "Return", cancellationToken))
            return Forbid();
        await _approvalService.ReturnAsync(dto, cancellationToken);
        return RedirectToAction(nameof(Pending));
    }

    // POST: /Approval/ReturnToPrevious —— 退回上一步。
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "permission:Return")]
    public async Task<IActionResult> ReturnToPrevious(ApprovalActionDto dto, CancellationToken cancellationToken)
    {
        if (!await AuthorizeTaskAsync(dto.ApprovalTaskId, "Return", cancellationToken))
            return Forbid();
        await _approvalService.ReturnToPreviousAsync(dto, cancellationToken);
        return RedirectToAction(nameof(Pending));
    }

    // POST: /Approval/Transfer —— 转交。
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "permission:Transfer")]
    public async Task<IActionResult> Transfer(TransferRequestDto dto, CancellationToken cancellationToken)
    {
        if (!await AuthorizeTaskAsync(dto.ApprovalTaskId, "Transfer", cancellationToken))
            return Forbid();
        await _approvalService.TransferAsync(dto, cancellationToken);
        return RedirectToAction(nameof(Pending));
    }

    // POST: /Approval/Submit —— 申请人提交审批（草稿/退回 → 审批中）。
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "permission:Submit")]
    public async Task<IActionResult> Submit(Guid documentId, SubmitDocumentDto dto, CancellationToken cancellationToken)
    {
        await _approvalService.SubmitAsync(documentId, dto, cancellationToken);
        return RedirectToAction("Details", "Document", new { id = documentId });
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

    // ===================== 私有辅助 =====================

    /// <summary>
    /// 资源级授权：加载 ApprovalTask 资源，交由 ApprovalTaskAuthorizationHandler 判定
    /// 当前用户能否对指定任务执行审批动作（真正审批时重新校验候选人与委托有效期）。
    /// </summary>
    private async Task<bool> AuthorizeTaskAsync(Guid taskId, string operation, CancellationToken cancellationToken)
    {
        var task = await _db.ApprovalTasks
            .Include(x => x.Document)
            .FirstOrDefaultAsync(x => x.Id == taskId, cancellationToken);
        if (task is null)
            return false;

        var result = await _authorizationService.AuthorizeAsync(
            User,
            task,
            new ApprovalTaskOperationRequirement(operation));

        return result.Succeeded;
    }
}
