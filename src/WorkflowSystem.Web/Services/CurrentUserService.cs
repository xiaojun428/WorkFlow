using System.Security.Claims;
using WorkflowSystem.Application.Abstractions;

namespace WorkflowSystem.Web.Services;

/// <summary>
/// 当前用户上下文实现（从 HttpContext 读取）。
/// 阶段 9 接入 Entra 前，若未认证则回退到开发测试用户（仅 Development 环境）。
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? UserId
    {
        get
        {
            var idClaim = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (Guid.TryParse(idClaim, out var id)) return id;
            // 开发阶段回退：返回种子测试用户 Alice（阶段 9 移除）。
            return Guid.Parse("00000000-0000-0000-0000-000000000031");
        }
    }

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

    public string? Email => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Email);
}
