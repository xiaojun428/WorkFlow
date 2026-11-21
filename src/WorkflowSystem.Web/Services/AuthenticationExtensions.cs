using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using WorkflowSystem.Application.Identity;

namespace WorkflowSystem.Web.Services;

/// <summary>
/// 认证配置扩展：接入 Microsoft Entra ID（Azure AD）OpenID Connect 登录认证。
/// 需求 8：Entra 只做登录认证（提供身份），内部角色/部门/权限由本系统维护。
///
/// 行为：
/// - 配置了 AzureAd:ClientId → 启用 Entra OIDC；登录成功后通过 IUserSyncService 把 Entra 身份
///   映射/同步到本系统 User，并把业务侧 User.Id 写入 ClaimTypes.NameIdentifier。
/// - 未配置 → 启用开发回退（Cookie 认证），由 Account/DevLogin 以测试用户登录。
/// </summary>
public static class AuthenticationExtensions
{
    public static IServiceCollection AddWorkflowAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var azureAd = configuration.GetSection("AzureAd");
        var clientId = azureAd["ClientId"];

        if (!string.IsNullOrWhiteSpace(clientId))
        {
            AddEntraAuthentication(services, configuration);
        }
        else
        {
            AddDevelopmentAuthentication(services);
        }

        return services;
    }

    private static void AddEntraAuthentication(IServiceCollection services, IConfiguration configuration)
    {
        services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
            .AddMicrosoftIdentityWebApp(options =>
            {
                configuration.Bind("AzureAd", options);
                options.Events = new OpenIdConnectEvents
                {
                    // 登录成功后：把 Entra 身份映射到本系统 User，写回业务侧 User.Id 作为 NameIdentifier。
                    OnTokenValidated = async context =>
                    {
                        var syncService = context.HttpContext.RequestServices.GetRequiredService<IUserSyncService>();
                        var oid = context.Principal?.FindFirstValue("http://schemas.microsoft.com/identity/claims/objectidentifier")
                                  ?? context.Principal?.FindFirstValue("oid");
                        var email = context.Principal?.FindFirstValue(ClaimTypes.Email)
                                    ?? context.Principal?.FindFirstValue("preferred_username")
                                    ?? string.Empty;
                        var displayName = context.Principal?.FindFirstValue("name") ?? email;

                        if (!string.IsNullOrWhiteSpace(oid))
                        {
                            var userId = await syncService.EnsureUserAsync(oid, email, displayName, context.HttpContext.RequestAborted);

                            // 用本系统 User.Id 替换 NameIdentifier，使 CurrentUserService 能读取业务用户 Id。
                            var identity = context.Principal?.Identity as ClaimsIdentity;
                            identity?.RemoveClaim(identity.FindFirst(ClaimTypes.NameIdentifier));
                            identity?.AddClaim(new Claim(ClaimTypes.NameIdentifier, userId.ToString()));
                        }
                    },
                };
            });
    }

    private static void AddDevelopmentAuthentication(IServiceCollection services)
    {
        // 开发环境回退：Cookie 认证，由 Account/DevLogin 以种子测试用户登录（阶段 9 接入 Entra 前使用）。
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.LoginPath = "/Account/DevLogin";
                options.LogoutPath = "/Account/Logout";
                options.AccessDeniedPath = "/Account/AccessDenied";
            });
    }
}
