using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkflowSystem.Application.Audit;
using WorkflowSystem.Application.Audit.Dtos;

namespace WorkflowSystem.Web.Controllers;

/// <summary>
/// 审计日志控制器：查看审计记录（含催办记录）。
/// 需要 ViewAuditLog 权限（管理员/工作流管理员）。
/// </summary>
[Authorize(Policy = "permission:ViewAuditLog")]
public class AuditController : Controller
{
    private readonly IAuditService _auditService;

    public AuditController(IAuditService auditService)
    {
        _auditService = auditService;
    }

    // GET: /Audit —— 审计日志列表（可按动作/实体类型筛选，含催办记录）。
    [HttpGet]
    public async Task<IActionResult> Index(AuditQuery query, CancellationToken cancellationToken)
    {
        var result = await _auditService.QueryAsync(query, cancellationToken);
        return View(result);
    }
}
