using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace WorkflowSystem.Web.Controllers;

/// <summary>
/// 语言切换控制器：切换界面语言（中文/英文），并通过 cookie 持久化选择（需求 10）。
/// </summary>
public class CultureController : Controller
{
    // POST: /Culture/Set —— 切换语言后返回来源页。
    [HttpPost]
    public IActionResult Set(string culture, string returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(culture))
        {
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) });
        }

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);

        return RedirectToAction("Index", "Home");
    }
}
