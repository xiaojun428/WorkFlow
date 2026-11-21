using Microsoft.AspNetCore.Authorization;

namespace WorkflowSystem.Web.Authorization;

/// <summary>
/// 权限需求：要求当前用户拥有指定的操作权限码（如 "Approve"、"Create"）。
/// 配合 PermissionHandler 实现基于 Policy 的粗粒度授权。
/// </summary>
public class PermissionRequirement : IAuthorizationRequirement
{
    /// <summary>要求的操作权限码。</summary>
    public string PermissionCode { get; }

    public PermissionRequirement(string permissionCode)
    {
        if (string.IsNullOrWhiteSpace(permissionCode))
            throw new ArgumentException("权限码不能为空。", nameof(permissionCode));
        PermissionCode = permissionCode;
    }
}

/// <summary>
/// 权限策略提供者：根据操作权限码动态构建授权策略。
/// 使用方式：在 Controller 上用 [Authorize(Policy = "permission:Approve")]。
/// </summary>
public static class PermissionPolicies
{
    /// <summary>策略名前缀。</summary>
    public const string Prefix = "permission";

    /// <summary>由权限码生成策略名。</summary>
    public static string For(string permissionCode) => $"{Prefix}:{permissionCode}";

    /// <summary>向 AuthorizationOptions 注册所有操作权限的策略。</summary>
    public static void AddPermissionPolicies(this AuthorizationOptions options)
    {
        foreach (var code in AllCodes)
        {
            options.AddPolicy(For(code), policy =>
                policy.Requirements.Add(new PermissionRequirement(code)));
        }
    }

    private static readonly string[] AllCodes =
    {
        Domain.Constants.PermissionCodes.Create,
        Domain.Constants.PermissionCodes.ViewOwn,
        Domain.Constants.PermissionCodes.ViewDepartment,
        Domain.Constants.PermissionCodes.ViewAll,
        Domain.Constants.PermissionCodes.Edit,
        Domain.Constants.PermissionCodes.Delete,
        Domain.Constants.PermissionCodes.Submit,
        Domain.Constants.PermissionCodes.Withdraw,
        Domain.Constants.PermissionCodes.Approve,
        Domain.Constants.PermissionCodes.Reject,
        Domain.Constants.PermissionCodes.Return,
        Domain.Constants.PermissionCodes.Transfer,
        Domain.Constants.PermissionCodes.DownloadAttachment,
        Domain.Constants.PermissionCodes.ManageWorkflow,
        Domain.Constants.PermissionCodes.ManagePermission,
        Domain.Constants.PermissionCodes.ViewAuditLog,
    };
}
