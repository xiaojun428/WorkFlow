using Microsoft.AspNetCore.Authorization;
using WorkflowSystem.Application.Permissions;

namespace WorkflowSystem.Web.Authorization;

/// <summary>
/// 权限策略处理器：处理 PermissionRequirement，调用权限服务判断当前用户是否拥有操作权限码。
/// </summary>
public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IPermissionService _permissionService;

    public PermissionHandler(IPermissionService permissionService)
    {
        _permissionService = permissionService;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        // 未认证直接失败（由认证中间件负责 Challenge，这里仅不通过）。
        if (context.User.Identity?.IsAuthenticated != true)
            return;

        var hasPermission = await _permissionService.HasPermissionAsync(requirement.PermissionCode);
        if (hasPermission)
            context.Succeed(requirement);
    }
}
