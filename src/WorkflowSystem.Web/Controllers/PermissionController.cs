using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkflowSystem.Application.Permissions;
using WorkflowSystem.Domain.Enums;

namespace WorkflowSystem.Web.Controllers;

/// <summary>
/// 权限管理控制器：管理员配置「角色 → 操作权限码」与「角色 → 字段权限」。
/// 仅拥有 ManagePermission 权限的用户可访问（粗粒度策略 + 后端校验）。
/// </summary>
[Authorize(Policy = "permission:ManagePermission")]
public class PermissionController : Controller
{
    private readonly IPermissionAdminService _permissionAdminService;

    public PermissionController(IPermissionAdminService permissionAdminService)
    {
        _permissionAdminService = permissionAdminService;
    }

    // GET: /Permission —— 权限管理首页（角色列表 + 文档类型）。
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewBag.Roles = await _permissionAdminService.GetRolesAsync(cancellationToken);
        ViewBag.DocumentTypes = await _permissionAdminService.GetDocumentTypesAsync(cancellationToken);
        return View();
    }

    // GET: /Permission/RolePermissions/{roleId} —— 配置某角色的操作权限码。
    [HttpGet]
    public async Task<IActionResult> RolePermissions(Guid roleId, CancellationToken cancellationToken)
    {
        var config = await _permissionAdminService.GetRolePermissionConfigAsync(roleId, cancellationToken);
        if (config is null) return NotFound();

        ViewBag.Roles = await _permissionAdminService.GetRolesAsync(cancellationToken);
        return View(config);
    }

    // POST: /Permission/RolePermissions —— 保存角色的操作权限（全量覆盖）。
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RolePermissions(SaveRolePermissionsRequestDto dto, CancellationToken cancellationToken)
    {
        await _permissionAdminService.SaveRolePermissionsAsync(dto, cancellationToken);
        TempData["Success"] = "角色权限已保存。";
        return RedirectToAction(nameof(RolePermissions), new { roleId = dto.RoleId });
    }

    // GET: /Permission/FieldPermissions?roleId=xxx&documentTypeId=yyy —— 配置字段权限。
    [HttpGet]
    public async Task<IActionResult> FieldPermissions(Guid roleId, Guid documentTypeId, CancellationToken cancellationToken)
    {
        ViewBag.Roles = await _permissionAdminService.GetRolesAsync(cancellationToken);
        ViewBag.DocumentTypes = await _permissionAdminService.GetDocumentTypesAsync(cancellationToken);

        var items = await _permissionAdminService.GetFieldPermissionsAsync(roleId, documentTypeId, cancellationToken);
        ViewBag.RoleId = roleId;
        ViewBag.DocumentTypeId = documentTypeId;
        ViewBag.FieldPermissionTypes = Enum.GetValues<FieldPermissionType>()
            .Select(e => new { Value = e, Name = GetFieldPermissionDisplayName(e) })
            .ToList();

        return View(items);
    }

    // POST: /Permission/FieldPermissions —— 保存字段权限（全量覆盖）。
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FieldPermissions(SaveFieldPermissionsRequestDto dto, CancellationToken cancellationToken)
    {
        await _permissionAdminService.SaveFieldPermissionsAsync(dto, cancellationToken);
        TempData["Success"] = "字段权限已保存。";
        return RedirectToAction(nameof(FieldPermissions), new { roleId = dto.RoleId, documentTypeId = dto.DocumentTypeId });
    }

    /// <summary>字段权限枚举的中文显示名。</summary>
    private static string GetFieldPermissionDisplayName(FieldPermissionType type) => type switch
    {
        FieldPermissionType.Hidden => "隐藏（Hidden）",
        FieldPermissionType.ReadOnly => "只读（ReadOnly）",
        FieldPermissionType.Editable => "可编辑（Editable）",
        FieldPermissionType.Required => "必填（Required）",
        _ => type.ToString(),
    };
}
