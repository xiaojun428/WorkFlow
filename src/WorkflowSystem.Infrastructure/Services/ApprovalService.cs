using Microsoft.EntityFrameworkCore;
using WorkflowSystem.Application.Abstractions;
using WorkflowSystem.Application.Approvals;
using WorkflowSystem.Application.Approvals.Dtos;
using WorkflowSystem.Application.Documents;
using WorkflowSystem.Application.Documents.Dtos;
using WorkflowSystem.Application.Workflows;
using WorkflowSystem.Domain.Common;
using WorkflowSystem.Domain.Constants;
using WorkflowSystem.Domain.Entities.Document;
using WorkflowSystem.Domain.Entities.Identity;
using WorkflowSystem.Domain.Entities.Workflow;
using WorkflowSystem.Domain.Enums;
using WorkflowSystem.Infrastructure.Persistence;

namespace WorkflowSystem.Infrastructure.Services;

/// <summary>
/// 审批应用服务实现：提交、审批、驳回、退回、撤回、转交的用例编排。
/// 关键设计：
/// - 事务边界：提交/审批操作在同一事务内更新任务状态 + 追加历史 + 推进工作流 + 更新文档状态。
/// - 幂等保护：审批任务用领域层 EnsurePending + RowVersion 乐观并发，重复审批抛异常。
/// - 审计：审批关键操作写 AuditLog（阶段 6 基础版）。
/// </summary>
public class ApprovalService : IApprovalService
{
    private readonly WorkflowDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IClock _clock;
    private readonly IWorkflowService _workflowService;
    private readonly IDocumentService _documentService;

    public ApprovalService(
        WorkflowDbContext db,
        ICurrentUserService currentUser,
        IClock clock,
        IWorkflowService workflowService,
        IDocumentService documentService)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
        _workflowService = workflowService;
        _documentService = documentService;
    }

    // ===================== 提交 =====================

    public async Task<ApprovalTaskListItemDto> SubmitAsync(Guid documentId, SubmitDocumentDto dto, CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId
            ?? throw new BusinessRuleViolationException("当前用户未认证。");

        var document = await _db.Documents
            .Include(x => x.DocumentType)
            .FirstOrDefaultAsync(x => x.Id == documentId, cancellationToken)
            ?? throw new NotFoundException("Document", documentId);

        // 只能提交自己的草稿/退回文档（防重复提交由领域层 Submit() 保证）。
        if (document.ApplicantUserId != userId)
            throw new BusinessRuleViolationException("只能提交自己的文档。");

        // 领域规则：仅草稿/退回可提交，重复提交抛异常。
        document.Submit();

        // 生成关联 Id，贯穿文档、工作流、审计、邮件。
        var correlationId = Guid.NewGuid().ToString("N");

        // 启动工作流（阶段 6 用最小实现）。
        var workflowInstanceId = await _workflowService.StartAsync(documentId, document.DocumentType!.Code, correlationId, cancellationToken);
        document.SetWorkflowInstance(workflowInstanceId);

        // 创建首个审批任务（经理审批）。
        var firstStep = ("manager-approval", "经理审批");
        document.UpdateCurrentStep(firstStep.Item1);

        var task = new ApprovalTask(
            documentId,
            GetActiveWorkflowDefinitionVersionId(document.DocumentTypeId),
            firstStep.Item1,
            firstStep.Item2,
            AssignmentType.Role,
            assignedRoleId: GetRoleIdByName(RoleNames.Manager),
            dueAtUtc: _clock.UtcNow.AddHours(48));

        _db.ApprovalTasks.Add(task);

        // 追加审批历史（提交事实）。
        _db.ApprovalHistories.Add(new ApprovalHistory(
            documentId,
            ApprovalAction.Submitted,
            userId,
            fromStatus: DocumentStatus.Draft.ToString(),
            toStatus: DocumentStatus.InApproval.ToString(),
            comment: dto.Comment));

        await _db.SaveChangesAsync(cancellationToken);

        return await ToListItemAsync(task, cancellationToken);
    }

    // ===================== 审批通过 =====================

    public async Task ApproveAsync(ApprovalActionDto dto, CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId
            ?? throw new BusinessRuleViolationException("当前用户未认证。");

        var task = await _db.ApprovalTasks
            .Include(x => x.Document)
            .FirstOrDefaultAsync(x => x.Id == dto.ApprovalTaskId, cancellationToken)
            ?? throw new NotFoundException("ApprovalTask", dto.ApprovalTaskId);

        // 领域规则：仅 Pending 可审批，重复审批抛异常（幂等保护）。
        task.Approve();

        var document = task.Document!;

        // 追加历史。
        _db.ApprovalHistories.Add(new ApprovalHistory(
            document.Id,
            ApprovalAction.Approved,
            userId,
            fromStatus: ApprovalTaskStatus.Pending.ToString(),
            toStatus: ApprovalTaskStatus.Approved.ToString(),
            comment: dto.Comment,
            approvalTaskId: task.Id));

        // 推进工作流到下一节点。
        var nextStep = await _workflowService.AdvanceAsync(document.WorkflowInstanceId!.Value, cancellationToken);

        if (nextStep is null)
        {
            // 所有节点完成，文档完成。
            document.MarkCompletedFlow();
        }
        else
        {
            // 创建下一节点的审批任务。
            var (stepName, roleName) = ResolveStep(nextStep);
            var nextTask = new ApprovalTask(
                document.Id,
                task.WorkflowDefinitionVersionId,
                nextStep,
                stepName,
                AssignmentType.Role,
                assignedRoleId: GetRoleIdByName(roleName),
                dueAtUtc: _clock.UtcNow.AddHours(48));
            _db.ApprovalTasks.Add(nextTask);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    // ===================== 驳回 =====================

    public async Task RejectAsync(ApprovalActionDto dto, CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId
            ?? throw new BusinessRuleViolationException("当前用户未认证。");

        var task = await _db.ApprovalTasks
            .Include(x => x.Document)
            .FirstOrDefaultAsync(x => x.Id == dto.ApprovalTaskId, cancellationToken)
            ?? throw new NotFoundException("ApprovalTask", dto.ApprovalTaskId);

        task.Reject();

        var document = task.Document!;
        document.MarkRejected();

        // 终止工作流。
        if (document.WorkflowInstanceId.HasValue)
            await _workflowService.TerminateAsync(document.WorkflowInstanceId.Value, cancellationToken);

        _db.ApprovalHistories.Add(new ApprovalHistory(
            document.Id,
            ApprovalAction.Rejected,
            userId,
            fromStatus: ApprovalTaskStatus.Pending.ToString(),
            toStatus: ApprovalTaskStatus.Rejected.ToString(),
            comment: dto.Comment,
            approvalTaskId: task.Id));

        await _db.SaveChangesAsync(cancellationToken);
    }

    // ===================== 退回申请人 =====================

    public async Task ReturnAsync(ApprovalActionDto dto, CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId
            ?? throw new BusinessRuleViolationException("当前用户未认证。");

        var task = await _db.ApprovalTasks
            .Include(x => x.Document)
            .FirstOrDefaultAsync(x => x.Id == dto.ApprovalTaskId, cancellationToken)
            ?? throw new NotFoundException("ApprovalTask", dto.ApprovalTaskId);

        task.Return();

        var document = task.Document!;
        document.MarkReturned();

        _db.ApprovalHistories.Add(new ApprovalHistory(
            document.Id,
            ApprovalAction.Returned,
            userId,
            fromStatus: ApprovalTaskStatus.Pending.ToString(),
            toStatus: ApprovalTaskStatus.Returned.ToString(),
            comment: dto.Comment,
            approvalTaskId: task.Id));

        await _db.SaveChangesAsync(cancellationToken);
    }

    // ===================== 撤回 =====================

    public async Task WithdrawAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId
            ?? throw new BusinessRuleViolationException("当前用户未认证。");

        var document = await _db.Documents
            .FirstOrDefaultAsync(x => x.Id == documentId, cancellationToken)
            ?? throw new NotFoundException("Document", documentId);

        if (document.ApplicantUserId != userId)
            throw new BusinessRuleViolationException("只能撤回自己的文档。");

        if (document.DocumentStatus != DocumentStatus.InApproval)
            throw new BusinessRuleViolationException("只有审批中的文档才能撤回。");

        document.MarkWithdrawn();

        // 取消所有待处理任务。
        var pendingTasks = await _db.ApprovalTasks
            .Where(x => x.DocumentId == documentId && x.Status == ApprovalTaskStatus.Pending)
            .ToListAsync(cancellationToken);
        foreach (var t in pendingTasks)
            t.Cancel();

        if (document.WorkflowInstanceId.HasValue)
            await _workflowService.TerminateAsync(document.WorkflowInstanceId.Value, cancellationToken);

        _db.ApprovalHistories.Add(new ApprovalHistory(
            documentId,
            ApprovalAction.Withdrawn,
            userId,
            fromStatus: DocumentStatus.InApproval.ToString(),
            toStatus: DocumentStatus.Withdrawn.ToString()));

        await _db.SaveChangesAsync(cancellationToken);
    }

    // ===================== 转交 =====================

    public async Task TransferAsync(TransferRequestDto dto, CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId
            ?? throw new BusinessRuleViolationException("当前用户未认证。");

        var task = await _db.ApprovalTasks
            .Include(x => x.Document)
            .FirstOrDefaultAsync(x => x.Id == dto.ApprovalTaskId, cancellationToken)
            ?? throw new NotFoundException("ApprovalTask", dto.ApprovalTaskId);

        task.Transfer(dto.NewAssigneeUserId);

        var document = task.Document!;

        // 创建新任务给新审批人。
        var newTask = new ApprovalTask(
            document.Id,
            task.WorkflowDefinitionVersionId,
            task.StepKey,
            task.StepName,
            AssignmentType.User,
            assignedUserId: dto.NewAssigneeUserId,
            dueAtUtc: task.DueAtUtc);

        _db.ApprovalTasks.Add(newTask);

        _db.ApprovalHistories.Add(new ApprovalHistory(
            document.Id,
            ApprovalAction.Transferred,
            userId,
            fromStatus: ApprovalTaskStatus.Pending.ToString(),
            toStatus: ApprovalTaskStatus.Transferred.ToString(),
            comment: dto.Comment,
            approvalTaskId: task.Id));

        await _db.SaveChangesAsync(cancellationToken);
    }

    // ===================== 查询 =====================

    public async Task<PagedResult<ApprovalTaskListItemDto>> GetPendingTasksAsync(DocumentQuery query, CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId
            ?? throw new BusinessRuleViolationException("当前用户未认证。");

        var q = _db.ApprovalTasks
            .AsNoTracking()
            .Where(x => x.Status == ApprovalTaskStatus.Pending
                        && (x.AssignedUserId == userId
                            || _db.ApprovalCandidates.Any(c => c.ApprovalTaskId == x.Id && c.CandidateUserId == userId)));

        var totalCount = await q.CountAsync(cancellationToken);
        var items = await q
            .OrderByDescending(x => x.DueAtUtc)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var list = new List<ApprovalTaskListItemDto>();
        foreach (var t in items)
            list.Add(await ToListItemAsync(t, cancellationToken));

        return new PagedResult<ApprovalTaskListItemDto>(list, totalCount, query.Page, query.PageSize);
    }

    public async Task<PagedResult<ApprovalTaskListItemDto>> GetProcessedTasksAsync(DocumentQuery query, CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId
            ?? throw new BusinessRuleViolationException("当前用户未认证。");

        var q = _db.ApprovalTasks
            .AsNoTracking()
            .Where(x => x.Status != ApprovalTaskStatus.Pending
                        && (x.AssignedUserId == userId
                            || _db.ApprovalCandidates.Any(c => c.ApprovalTaskId == x.Id && c.CandidateUserId == userId)));

        var totalCount = await q.CountAsync(cancellationToken);
        var items = await q
            .OrderByDescending(x => x.CompletedAtUtc)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var list = new List<ApprovalTaskListItemDto>();
        foreach (var t in items)
            list.Add(await ToListItemAsync(t, cancellationToken));

        return new PagedResult<ApprovalTaskListItemDto>(list, totalCount, query.Page, query.PageSize);
    }

    public async Task<ApprovalTaskDetailDto?> GetTaskDetailAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        var task = await _db.ApprovalTasks
            .AsNoTracking()
            .Include(x => x.Document)
            .FirstOrDefaultAsync(x => x.Id == taskId, cancellationToken);

        if (task is null) return null;

        var documentDetail = await _documentService.GetDocumentAsync(task.DocumentId, cancellationToken);
        var history = await GetHistoryAsync(task.DocumentId, cancellationToken);

        return new ApprovalTaskDetailDto(
            task.Id,
            task.DocumentId,
            documentDetail?.DocumentNo ?? string.Empty,
            documentDetail?.Title ?? string.Empty,
            documentDetail?.DocumentTypeName ?? string.Empty,
            task.StepKey,
            task.StepName,
            task.Document!.ApplicantUserId,
            documentDetail?.ApplicantName ?? string.Empty,
            task.DueAtUtc,
            task.Status,
            documentDetail!,
            history);
    }

    public async Task<IReadOnlyList<ApprovalHistoryItemDto>> GetHistoryAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        return await _db.ApprovalHistories
            .AsNoTracking()
            .Where(x => x.DocumentId == documentId)
            .OrderBy(x => x.OccurredAtUtc)
            .Select(x => new ApprovalHistoryItemDto(
                x.Id,
                x.Action,
                x.OperatorUserId,
                x.OperatorUser != null ? x.OperatorUser.DisplayName : string.Empty,
                x.FromStatus,
                x.ToStatus,
                x.Comment,
                x.OccurredAtUtc))
            .ToListAsync(cancellationToken);
    }

    // ===================== 私有辅助 =====================

    private async Task<ApprovalTaskListItemDto> ToListItemAsync(ApprovalTask task, CancellationToken cancellationToken)
    {
        var document = await _db.Documents
            .AsNoTracking()
            .Include(x => x.DocumentType)
            .Include(x => x.ApplicantUser)
            .FirstOrDefaultAsync(x => x.Id == task.DocumentId, cancellationToken);

        return new ApprovalTaskListItemDto(
            task.Id,
            task.DocumentId,
            document?.DocumentNo ?? string.Empty,
            document?.Title ?? string.Empty,
            document?.DocumentType?.Name ?? string.Empty,
            task.StepKey,
            task.StepName,
            document?.ApplicantUserId ?? Guid.Empty,
            document?.ApplicantUser?.DisplayName ?? string.Empty,
            task.AssignmentType,
            task.AssignedUserId,
            task.AssignedRoleId,
            task.AssignedDepartmentId,
            task.DueAtUtc,
            task.Status,
            task.CompletedAtUtc);
    }

    private Guid GetActiveWorkflowDefinitionVersionId(Guid documentTypeId)
    {
        // 阶段 6 简化：返回首个 Active 版本（阶段 7 精确绑定）。
        return _db.WorkflowDefinitionVersions
            .Where(x => x.IsActive)
            .Select(x => x.Id)
            .FirstOrDefault();
    }

    private Guid GetRoleIdByName(string roleName)
    {
        return _db.Roles
            .Where(x => x.Name == roleName)
            .Select(x => x.Id)
            .FirstOrDefault();
    }

    private static (string StepName, string RoleName) ResolveStep(string stepKey)
        => stepKey switch
        {
            "manager-approval" => ("经理审批", RoleNames.Manager),
            "director-approval" => ("部长审批", RoleNames.Manager),
            _ => (stepKey, RoleNames.Approver),
        };
}
