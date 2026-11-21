using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WorkflowSystem.Web.Controllers;

/// <summary>
/// 工作流设计器控制器：提供可视化工作流设计入口（嵌入 Elsa Studio）。
/// 仅拥有 ManageWorkflow 权限的用户可访问。
/// </summary>
[Authorize(Policy = "permission:ManageWorkflow")]
public class WorkflowDesignerController : Controller
{
    // GET: /WorkflowDesigner —— 工作流定义列表 + 设计器入口。
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    // GET: /WorkflowDesigner/Editor?definitionId=xxx —— 编辑指定工作流定义（嵌入设计器 iframe）。
    [HttpGet]
    public IActionResult Editor(string? definitionId)
    {
        ViewBag.DefinitionId = definitionId;
        return View();
    }
}
