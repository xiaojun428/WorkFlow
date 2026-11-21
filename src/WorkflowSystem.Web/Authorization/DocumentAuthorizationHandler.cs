using Microsoft.AspNetCore.Authorization;
using WorkflowSystem.Application.Abstractions;
using WorkflowSystem.Application.Permissions;
using WorkflowSystem.Domain.Entities.Document;
using WorkflowSystem.Domain.Enums;

namespace WorkflowSystem.Web.Authorization;

/// <summary>
/// 文档操作需求：对具体 Document 资源做操作级授权。
/// Operation 为操作名（View/Edit/Delete/Submit/Withdraw/DownloadAttachment 等）。
/// </summary>
public class DocumentOperationRequirement : IAuthorizationRequirement
{
    public string Operation { get; }

    public DocumentOperationRequirement(string operation)
    {
        if (string.IsNullOrWhiteSpace(operation))
            throw new ArgumentException("操作名不能为空。", nameof(operation));
        Operation = operation;
    }
}

/// <summary>
/// 文档资源级授权处理器：综合判定当前用户能否对指定 Document 执行某操作。
/// 判定因素：申请人/所有者、数据范围、部门、文档状态、操作权限码、字段权限由应用层另行裁剪。
/// </summary>
public class DocumentAuthorizationHandler : AuthorizationHandler<DocumentOperationRequirement, Document>
{
    private readonly IPermissionService _permissionService;
    private readonly ICurrentUserService _currentUser;

    public DocumentAuthorizationHandler(IPermissionService permissionService, ICurrentUserService currentUser)
    {
        _permissionService = permissionService;
        _currentUser = currentUser;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        DocumentOperationRequirement requirement,
        Document resource)
    {
        if (context.User.Identity?.IsAuthenticated != true)
            return;

        var userId = _currentUser.UserId;
        if (userId is null)
            return;

        var perm = await _permissionService.GetCurrentUserContextAsync();

        var allowed = requirement.Operation switch
        {
            // 查看：申请人可看自己的；其余需按数据范围判定。
            "View" => CanView(perm, resource, userId.Value),
            // 编辑：申请人/所有者且状态为草稿或退回。
            "Edit" => CanEdit(perm, resource, userId.Value),
            // 删除：仅申请人/所有者且为草稿，且拥有 Delete 权限。
            "Delete" => CanDelete(perm, resource, userId.Value),
            // 提交：申请人/所有者，状态为草稿或退回。
            "Submit" => CanSubmit(perm, resource, userId.Value),
            // 撤回：申请人/所有者，状态为审批中。
            "Withdraw" => CanWithdraw(perm, resource, userId.Value),
            // 下载附件：可查看该文档即可（附件的具体授权在下载端点再校验）。
            "DownloadAttachment" => CanView(perm, resource, userId.Value),
            _ => false,
        };

        if (allowed)
            context.Succeed(requirement);
    }

    // 数据范围判定：All 通过；Department 要求文档所属部门在当前用户部门集合内；Self 要求是申请人本人。
    private bool WithinScope(PermissionContext perm, Document doc, Guid userId)
        => perm.EffectiveScope switch
        {
            DataScopeType.All => true,
            DataScopeType.Department => perm.DepartmentIds.Contains(doc.OwnerDepartmentId),
            DataScopeType.Self => doc.ApplicantUserId == userId,
            _ => false,
        };

    private bool CanView(PermissionContext perm, Document doc, Guid userId)
        // 申请人永远可查看自己的文档。
        => doc.ApplicantUserId == userId
           || (perm.HasPermission("ViewAll") && perm.EffectiveScope == DataScopeType.All)
           || (perm.HasPermission("ViewDepartment") && perm.DepartmentIds.Contains(doc.OwnerDepartmentId))
           || (perm.HasPermission("ViewOwn") && doc.ApplicantUserId == userId)
           || (perm.HasPermission("ViewAll") && WithinScope(perm, doc, userId));

    private bool CanEdit(PermissionContext perm, Document doc, Guid userId)
    {
        if (!perm.HasPermission("Edit")) return false;
        if (doc.ApplicantUserId != userId) return false;
        return doc.DocumentStatus is DocumentStatus.Draft or DocumentStatus.Returned;
    }

    private bool CanDelete(PermissionContext perm, Document doc, Guid userId)
    {
        if (!perm.HasPermission("Delete")) return false;
        if (doc.ApplicantUserId != userId) return false;
        // 已提交或已完成文档不得删除。
        return doc.DocumentStatus is DocumentStatus.Draft or DocumentStatus.Returned;
    }

    private bool CanSubmit(PermissionContext perm, Document doc, Guid userId)
    {
        if (!perm.HasPermission("Submit")) return false;
        if (doc.ApplicantUserId != userId) return false;
        return doc.DocumentStatus is DocumentStatus.Draft or DocumentStatus.Returned;
    }

    private bool CanWithdraw(PermissionContext perm, Document doc, Guid userId)
    {
        if (!perm.HasPermission("Withdraw")) return false;
        if (doc.ApplicantUserId != userId) return false;
        return doc.DocumentStatus == DocumentStatus.InApproval;
    }
}
