using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkflowSystem.Infrastructure.Persistence.Seed;

namespace WorkflowSystem.Web.Controllers;

/// <summary>
/// 账号控制器：处理登录/登出。
/// 接入 Entra 后，登录由 Entra OIDC 中间件处理；本控制器提供开发环境回退登录（DevLogin）与登出。
/// </summary>
public class AccountController : Controller
{
    // GET: /Account/DevLogin —— 开发环境登录（以种子测试用户 Alice 登录）。
    [HttpGet]
    [AllowAnonymous]
    public IActionResult DevLogin()
    {
        // 仅开发环境可用（生产环境应通过 Entra 登录）。
        return View();
    }

    // POST: /Account/DevLogin —— 选择测试用户登录（开发环境）。
    [HttpPost]
    [ValidateAntiForgeryToken]
    [AllowAnonymous]
    public async Task<IActionResult> DevLogin(string userId)
    {
        // 开发回退：从种子用户中选择一个身份登录（默认 Alice）。
        var selected = userId switch
        {
            "bob" => (Id: SeedData.UserBob, Name: "Bob（Manager）", Email: "bob@example.com"),
            "carol" => (Id: SeedData.UserCarol, Name: "Carol（财务）", Email: "carol@example.com"),
            "dave" => (Id: SeedData.UserDave, Name: "Dave（QA）", Email: "dave@example.com"),
            "eve" => (Id: SeedData.UserEve, Name: "Eve（采购）", Email: "eve@example.com"),
            "frank" => (Id: SeedData.UserFrank, Name: "Frank（管理员）", Email: "frank@example.com"),
            _ => (Id: SeedData.UserAlice, Name: "Alice（申请人）", Email: "alice@example.com"),
        };

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, selected.Id.ToString()),
            new(ClaimTypes.Name, selected.Name),
            new(ClaimTypes.Email, selected.Email),
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));

        return RedirectToAction("Index", "Home");
    }

    // POST: /Account/Logout —— 登出。
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    // GET: /Account/AccessDenied —— 无权访问。
    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied() => View();
}
