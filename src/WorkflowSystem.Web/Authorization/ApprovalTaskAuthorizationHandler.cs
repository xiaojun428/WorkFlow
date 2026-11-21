using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using WorkflowSystem.Application.Abstractions;
using WorkflowSystem.Application.Permissions;
using WorkflowSystem.Domain.Entities.Workflow;
using WorkflowSystem.Domain.Enums;
using WorkflowSystem.Infrastructure.Persistence;

namespace WorkflowSystem.Web.Authorization;

/// <summary>
/// 审批任务操作需求：对具体 ApprovalTask 做审批操作授权（Approve/Reject/Return/Transfer）。
/// </summary>
public class ApprovalTaskOperationRequirement : IAuthorizationRequirement
{
    public string Operation { get; }

    public ApprovalTaskOperationRequirement(string operation)
    {
        if (string.IsNullOrWhiteSpace(operation))
            throw new ArgumentException("操作名不能为空。", nameof(operation));
        Operation = operation;
    }
}

/// <summary>
/// 审批任务资源级授权处理器。
/// 关键安全规则：真正审批时必须重新判断操作者是否仍符合候选人和权限规则，不能只信任前端传入的 TaskId。
/// </summary>
public class ApprovalTaskAuthorizationHandler : AuthorizationHandler<ApprovalTaskOperationRequirement, ApprovalTask>
{
    private readonly IPermissionService _permissionService;
    private readonly ICurrentUserService _currentUser;
    private readonly IClock _clock;
    private readonly WorkflowDbContext _db;

    public ApprovalTaskAuthorizationHandler(
        IPermissionService permissionService,
        ICurrentUserService currentUser,
        IClock clock,
        WorkflowDbContext db)
    {
        _permissionService = permissionService;
        _currentUser = currentUser;
        _clock = clock;
        _db = db;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ApprovalTaskOperationRequirement requirement,
        ApprovalTask resource)
    {
        if (context.User.Identity?.IsAuthenticated != true)
            return;

        var userId = _currentUser.UserId;
        if (userId is null)
            return;

        var allowed = requirement.Operation switch
        {
            "Approve" => await CanActAsync("Approve", resource, userId.Value),
            "Reject" => await CanActAsync("Reject", resource, userId.Value),
            "Return" => await CanActAsync("Return", resource, userId.Value),
            "Transfer" => await CanActAsync("Transfer", resource, userId.Value),
            _ => false,
        };

        if (allowed)
            context.Succeed(requirement);
    }

    /// <summary>
    /// 判定当前用户能否对某审批任务执行审批动作。
    /// 规则：任务必须 Pending；用户必须拥有对应权限码；用户必须是任务当前指派者或有效候选人；
    /// 若用户是通过代理（委托）身份操作，须校验代理有效期。
    /// </summary>
    private async Task<bool> CanActAsync(string operation, ApprovalTask task, Guid userId)
    {
        // 任务必须处于待处理状态。
        if (task.Status != ApprovalTaskStatus.Pending)
            return false;

        // 必须拥有操作权限码。
        var perm = await _permissionService.GetCurrentUserContextAsync();
        if (!perm.HasPermission(operation))
            return false;

        // 1) 直接指派给该用户。
        if (task.AssignedUserId == userId)
            return true;

        // 2) 候选审批人包含该用户。
        var isCandidate = await _db.ApprovalCandidates
            .AnyAsync(c => c.ApprovalTaskId == task.Id && c.CandidateUserId == userId);
        if (isCandidate)
            return true;

        // 3) 代理审批：存在有效的委托规则，委托人是当前任务指派者或候选人。
        var now = _clock.UtcNow;
        var hasValidDelegation = await _db.DelegationRules
            .AnyAsync(d => d.DelegateUserId == userId
                           && d.IsActive
                           && now >= d.ValidFromUtc
                           && now <= d.ValidToUtc
                           && (d.DocumentTypeId == null || d.DocumentTypeId == task.Document!.DocumentTypeId)
                           && (d.DelegatorUserId == task.AssignedUserId
                               || _db.ApprovalCandidates.Any(c =>
                                   c.ApprovalTaskId == task.Id && c.CandidateUserId == d.DelegatorUserId)));

        return hasValidDelegation;
    }
}
