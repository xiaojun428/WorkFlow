using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WorkflowSystem.Application.Abstractions;
using WorkflowSystem.Application.Approvals;
using WorkflowSystem.Application.Approvals.Dtos;
using WorkflowSystem.Application.Audit;
using WorkflowSystem.Application.Documents;
using WorkflowSystem.Application.Documents.Dtos;
using WorkflowSystem.Application.Notifications;
using WorkflowSystem.Application.Workflows;
using WorkflowSystem.Domain.Common;
using WorkflowSystem.Domain.Constants;
using WorkflowSystem.Domain.Entities.Document;
using WorkflowSystem.Domain.Entities.Workflow;
using WorkflowSystem.Domain.Enums;
using WorkflowSystem.Infrastructure.Persistence;

namespace WorkflowSystem.Infrastructure.Services;

/// <summary>
/// 审批应用服务实现：提交、审批（含会签）、驳回、退回申请人、退回上一步、撤回、转交的用例编排。
///
/// 关键设计（阶段 8 完整版）：
/// - 流程节点序列由 IApprovalFlowProvider 按文档类型 + 字段值（金额）解析，金额条件分支/多级审批/会签比例集中于此，不硬编码。
/// - 会签：审批任务按候选人集合 + 会签比例计算所需通过人数；候选人逐个审批，满足比例后节点通过并推进。
/// - 事务边界：审批操作在同一事务内更新任务状态 + 追加历史 + 推进工作流 + 更新文档状态。
/// - 幂等保护：ApprovalTask 用 EnsurePending + RowVersion 乐观并发；候选人用 HasActed 防重复审批。
/// - 审计：所有审批关键操作写 AuditLog（追加写入）。
/// - 代理审批：真正审批时二次校验委托有效期（过期委托不能审批）。
/// </summary>
public class ApprovalService : IApprovalService
{
    private readonly WorkflowDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IClock _clock;
    private readonly IWorkflowService _workflowService;
    private readonly IDocumentService _documentService;
    private readonly IEnumerable<IApprovalFlowProvider> _flowProviders;
    private readonly IAuditService _auditService;
    private readonly IEmailService _emailService;

    public ApprovalService(
        WorkflowDbContext db,
        ICurrentUserService currentUser,
        IClock clock,
        IWorkflowService workflowService,
        IDocumentService documentService,
        IEnumerable<IApprovalFlowProvider> flowProviders,
        IAuditService auditService,
        IEmailService emailService)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
        _workflowService = workflowService;
        _documentService = documentService;
        _flowProviders = flowProviders;
        _auditService = auditService;
        _emailService = emailService;
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

        // 启动工作流（Elsa Bookmark 挂起在首个审批点）。
        var workflowInstanceId = await _workflowService.StartAsync(documentId, document.DocumentType!.Code, correlationId, cancellationToken);
        document.SetWorkflowInstance(workflowInstanceId);

        // 解析审批步骤序列（金额条件分支 / 多级审批 / 会签比例）。
        var steps = await ResolveStepsAsync(document, cancellationToken);
        var firstStep = steps[0];

        document.UpdateCurrentStep(firstStep.StepKey);

        // 创建首个审批任务（含候选人展开与会签比例）。
        var task = await CreateTaskForStepAsync(document, firstStep, previousStepKey: null, cancellationToken);

        // 追加审批历史（提交事实）。
        _db.ApprovalHistories.Add(new ApprovalHistory(
            documentId,
            ApprovalAction.Submitted,
            userId,
            fromStatus: DocumentStatus.Draft.ToString(),
            toStatus: DocumentStatus.InApproval.ToString(),
            comment: dto.Comment));

        await _db.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync("Submit", "Document", documentId, detail: Serialize(new { CorrelationId = correlationId }), correlationId: correlationId, cancellationToken: cancellationToken);

        // 通知首个审批步骤的候选人（异步入队，不阻塞提交事务）。
        await NotifyApproversAsync(task, document, correlationId, cancellationToken);

        return await ToListItemAsync(task, cancellationToken);
    }

    // ===================== 审批通过（含会签） =====================

    public async Task ApproveAsync(ApprovalActionDto dto, CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId
            ?? throw new BusinessRuleViolationException("当前用户未认证。");

        var task = await _db.ApprovalTasks
            .Include(x => x.Document).ThenInclude(x => x!.DocumentType)
            .Include(x => x.Candidates)
            .FirstOrDefaultAsync(x => x.Id == dto.ApprovalTaskId, cancellationToken)
            ?? throw new NotFoundException("ApprovalTask", dto.ApprovalTaskId);

        // 代理审批二次校验：若操作者并非任务直接指派者/候选人，必须存在有效委托，否则拒绝。
        await EnsureCanActAsync(task, userId, cancellationToken);

        var document = task.Document!;

        // 判定是否会签任务（存在多个候选人）。会签场景逐候选人聚合，单人场景直接完成。
        var isCountersign = task.Candidates.Count > 1;
        bool nodeApproved;

        if (isCountersign)
        {
            nodeApproved = await ApproveCountersignAsync(task, userId, dto.Comment, cancellationToken);
        }
        else
        {
            task.Approve();
            nodeApproved = true;
            _db.ApprovalHistories.Add(new ApprovalHistory(
                document.Id,
                ApprovalAction.Approved,
                userId,
                fromStatus: ApprovalTaskStatus.Pending.ToString(),
                toStatus: ApprovalTaskStatus.Approved.ToString(),
                comment: dto.Comment,
                approvalTaskId: task.Id));
        }

        if (!nodeApproved)
        {
            // 会签未满足比例：仅保存候选人审批记录，节点仍待处理。
            await _db.SaveChangesAsync(cancellationToken);
            await _auditService.LogAsync("Approve", "ApprovalTask", task.Id, detail: Serialize(new { Countersign = true, Pending = true }), cancellationToken: cancellationToken);
            return;
        }

        // 节点通过：推进到下一节点或完成。
        await AdvanceAfterApprovalAsync(task, document, cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);
        await _auditService.LogAsync("Approve", "ApprovalTask", task.Id, detail: Serialize(new { DocumentId = document.Id }), cancellationToken: cancellationToken);
    }

    // ===================== 批量审批 =====================

    public async Task<BatchApproveResultDto> BatchApproveAsync(BatchApproveRequestDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.ApprovalTaskIds is null || dto.ApprovalTaskIds.Count == 0)
            throw new BusinessRuleViolationException("请至少选择一个审批任务。");

        var items = new List<BatchApproveResultItemDto>();

        foreach (var taskId in dto.ApprovalTaskIds.Distinct())
        {
            try
            {
                // 复用单条审批逻辑（含权限校验、会签、推进、审计）。
                await ApproveAsync(new ApprovalActionDto { ApprovalTaskId = taskId, Comment = dto.Comment }, cancellationToken);
                items.Add(new BatchApproveResultItemDto(taskId, ResolveDocumentNoForBatch(taskId), true, null));
            }
            catch (Exception ex) when (ex is DomainException or DbUpdateConcurrencyException)
            {
                // 单条失败不影响其余任务：记录失败原因并重置变更追踪器，避免异常状态污染后续操作。
                _db.ChangeTracker.Clear();
                items.Add(new BatchApproveResultItemDto(taskId, ResolveDocumentNoForBatch(taskId), false, ex.Message));
            }
        }

        return new BatchApproveResultDto(
            dto.ApprovalTaskIds.Count,
            items.Count(x => x.Success),
            items.Count(x => !x.Success),
            items);
    }

    private string ResolveDocumentNoForBatch(Guid taskId)
    {
        return _db.ApprovalTasks
            .AsNoTracking()
            .Where(t => t.Id == taskId)
            .Select(t => t.Document != null ? t.Document.DocumentNo : string.Empty)
            .FirstOrDefault() ?? string.Empty;
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

        await EnsureCanActAsync(task, userId, cancellationToken);

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
        await _auditService.LogAsync("Reject", "ApprovalTask", task.Id, detail: Serialize(new { DocumentId = document.Id }), cancellationToken: cancellationToken);
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

        await EnsureCanActAsync(task, userId, cancellationToken);

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
        await _auditService.LogAsync("Return", "ApprovalTask", task.Id, detail: Serialize(new { DocumentId = document.Id }), cancellationToken: cancellationToken);
    }

    // ===================== 退回上一步 =====================

    public async Task ReturnToPreviousAsync(ApprovalActionDto dto, CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId
            ?? throw new BusinessRuleViolationException("当前用户未认证。");

        var task = await _db.ApprovalTasks
            .Include(x => x.Document)
            .FirstOrDefaultAsync(x => x.Id == dto.ApprovalTaskId, cancellationToken)
            ?? throw new NotFoundException("ApprovalTask", dto.ApprovalTaskId);

        await EnsureCanActAsync(task, userId, cancellationToken);

        // 无上一步则不允许退回上一步（退回到申请人改用 Return）。
        if (string.IsNullOrWhiteSpace(task.PreviousStepKey))
            throw new BusinessRuleViolationException("当前是首个审批步骤，无法退回上一步。");

        task.ReturnToPrevious();

        var document = task.Document!;

        // 重新创建上一步的审批任务（需中间审批人重新审批）。
        var previousStep = new ApprovalStepDefinition(task.PreviousStepKey!, ResolveStepName(task.PreviousStepKey!), ResolveStepRole(task.PreviousStepKey!), 100);
        var previousTask = await CreateTaskForStepAsync(document, previousStep, previousStepKey: null, cancellationToken);

        document.UpdateCurrentStep(previousStep.StepKey);

        _db.ApprovalHistories.Add(new ApprovalHistory(
            document.Id,
            ApprovalAction.ReturnToPrevious,
            userId,
            fromStatus: ApprovalTaskStatus.Pending.ToString(),
            toStatus: ApprovalTaskStatus.Returned.ToString(),
            comment: dto.Comment,
            approvalTaskId: task.Id));

        await _db.SaveChangesAsync(cancellationToken);
        await _auditService.LogAsync("ReturnToPrevious", "ApprovalTask", task.Id, detail: Serialize(new { DocumentId = document.Id, PreviousStep = task.PreviousStepKey, NewTaskId = previousTask.Id }), cancellationToken: cancellationToken);
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
        await _auditService.LogAsync("Withdraw", "Document", documentId, cancellationToken: cancellationToken);
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

        await EnsureCanActAsync(task, userId, cancellationToken);

        task.Transfer(dto.NewAssigneeUserId);

        var document = task.Document!;

        // 创建新任务给新审批人（保留会签比例与上一步信息）。
        var newTask = new ApprovalTask(
            document.Id,
            task.WorkflowDefinitionVersionId,
            task.StepKey,
            task.StepName,
            AssignmentType.User,
            assignedUserId: dto.NewAssigneeUserId,
            dueAtUtc: task.DueAtUtc,
            countersignPercentage: task.CountersignPercentage,
            previousStepKey: task.PreviousStepKey);
        newTask.SetCandidates(new[] { dto.NewAssigneeUserId });

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
        await _auditService.LogAsync("Transfer", "ApprovalTask", task.Id, detail: Serialize(new { NewAssignee = dto.NewAssigneeUserId }), cancellationToken: cancellationToken);
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

    /// <summary>
    /// 解析审批步骤序列：读取文档动态字段值，交给 IApprovalFlowProvider 计算金额条件分支与多级审批。
    /// </summary>
    private async Task<IReadOnlyList<ApprovalStepDefinition>> ResolveStepsAsync(Document document, CancellationToken cancellationToken)
    {
        var fieldValues = await _db.DocumentFieldValues
            .AsNoTracking()
            .Include(x => x.FieldDefinition)
            .Where(x => x.DocumentId == document.Id)
            .ToListAsync(cancellationToken);

        var snapshot = fieldValues.ToDictionary(
            x => x.FieldDefinition!.FieldKey,
            x => new DocumentFieldValueSnapshot(x.FieldDefinition!.FieldKey, x.FieldValueNumber, x.FieldValueText));

        var documentTypeCode = document.DocumentType!.Code;
        var provider = _flowProviders.FirstOrDefault(p => p.Supports(documentTypeCode))
            ?? throw new BusinessRuleViolationException($"文档类型 {documentTypeCode} 未配置审批流程提供者。");

        var steps = await provider.ResolveStepsAsync(documentTypeCode, snapshot, cancellationToken);
        if (steps.Count == 0)
            throw new BusinessRuleViolationException("文档类型未配置审批流程。");
        return steps;
    }

    /// <summary>
    /// 为某个审批步骤创建审批任务：展开候选审批人（按角色），设置会签比例与所需通过人数。
    /// </summary>
    private async Task<ApprovalTask> CreateTaskForStepAsync(
        Document document,
        ApprovalStepDefinition step,
        string? previousStepKey,
        CancellationToken cancellationToken)
    {
        var roleId = step.RoleName is null ? (Guid?)null : GetRoleIdByName(step.RoleName);

        var task = new ApprovalTask(
            document.Id,
            GetActiveWorkflowDefinitionVersionId(document.DocumentTypeId),
            step.StepKey,
            step.StepName,
            AssignmentType.Role,
            assignedRoleId: roleId,
            dueAtUtc: _clock.UtcNow.AddHours(48),
            countersignPercentage: step.CountersignPercentage,
            previousStepKey: previousStepKey);

        // 展开候选审批人（该角色的所有有效用户）。
        var candidateUserIds = await ResolveCandidateUserIdsAsync(roleId, cancellationToken);
        task.SetCandidates(candidateUserIds);

        _db.ApprovalTasks.Add(task);
        return task;
    }

    /// <summary>
    /// 按角色展开候选审批人 Id（该角色下所有有效用户）。
    /// </summary>
    private async Task<List<Guid>> ResolveCandidateUserIdsAsync(Guid? roleId, CancellationToken cancellationToken)
    {
        if (roleId is null)
            return new List<Guid>();

        return await _db.UserRoles
            .AsNoTracking()
            .Where(ur => ur.RoleId == roleId.Value
                         && ur.User != null && ur.User.IsActive
                         && (ur.ValidToUtc == null || ur.ValidToUtc > _clock.UtcNow))
            .Select(ur => ur.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    /// <summary>会签聚合：记录当前候选人的通过审批，判定是否满足会签比例。</summary>
    private Task<bool> ApproveCountersignAsync(ApprovalTask task, Guid userId, string comment, CancellationToken cancellationToken)
    {
        var candidate = task.Candidates.FirstOrDefault(c => c.CandidateUserId == userId)
            ?? throw new BusinessRuleViolationException("当前用户不是该审批任务的候选人。");

        candidate.MarkActed(ApprovalTaskStatus.Approved);

        // 追加该候选人的通过历史。
        _db.ApprovalHistories.Add(new ApprovalHistory(
            task.DocumentId,
            ApprovalAction.Approved,
            userId,
            fromStatus: ApprovalTaskStatus.Pending.ToString(),
            toStatus: ApprovalTaskStatus.Pending.ToString(),
            comment: comment,
            approvalTaskId: task.Id));

        // 记录一次通过，返回是否满足会签比例（节点通过）。
        return Task.FromResult(task.RecordCandidateApproval());
    }

    /// <summary>审批通过后推进：定位下一节点并创建任务，或完成流程。</summary>
    private async Task AdvanceAfterApprovalAsync(ApprovalTask task, Document document, CancellationToken cancellationToken)
    {
        // 解析完整步骤序列，定位当前步骤。
        var steps = await ResolveStepsAsync(document, cancellationToken);
        var currentIndex = steps.ToList().FindIndex(s => s.StepKey == task.StepKey);

        // 恢复 Elsa 工作流（Bookmark 恢复，引擎骨架完成当前审批点）。
        if (document.WorkflowInstanceId.HasValue)
            await _workflowService.AdvanceAsync(document.WorkflowInstanceId.Value, task.StepKey, cancellationToken);

        var correlationId = document.WorkflowInstanceId.HasValue
            ? (await _db.WorkflowInstances.Where(x => x.Id == document.WorkflowInstanceId.Value).Select(x => x.CorrelationId).FirstOrDefaultAsync(cancellationToken)) ?? document.Id.ToString("N")
            : document.Id.ToString("N");

        if (currentIndex < 0 || currentIndex == steps.Count - 1)
        {
            // 已是最后一步：流程完成。
            document.MarkCompletedFlow();
            document.UpdateCurrentStep(null);

            // 通知申请人：审批完成。
            await NotifyApplicantAsync(document, "审批完成", $"您的文档「{document.Title}」已完成全部审批。", correlationId, cancellationToken);
        }
        else
        {
            // 创建下一节点的审批任务（保留上一步信息，供退回上一步使用）。
            var nextStep = steps[currentIndex + 1];
            var nextTask = await CreateTaskForStepAsync(document, nextStep, previousStepKey: task.StepKey, cancellationToken);
            document.UpdateCurrentStep(nextStep.StepKey);

            // 通知下一节点的候选人。
            await NotifyApproversAsync(nextTask, document, correlationId, cancellationToken);
        }
    }

    /// <summary>
    /// 代理审批二次校验：操作者必须是任务直接指派者/候选人，或存在有效委托（委托人在指派者/候选人中且委托未过期）。
    /// </summary>
    private async Task EnsureCanActAsync(ApprovalTask task, Guid userId, CancellationToken cancellationToken)
    {
        if (task.Status != ApprovalTaskStatus.Pending)
            throw new BusinessRuleViolationException("审批任务已处理，不能重复操作。");

        if (task.AssignedUserId == userId)
            return;

        var isCandidate = await _db.ApprovalCandidates
            .AnyAsync(c => c.ApprovalTaskId == task.Id && c.CandidateUserId == userId, cancellationToken);
        if (isCandidate)
            return;

        // 代理审批：委托规则有效期内，委托人是任务指派者或候选人。
        var now = _clock.UtcNow;
        var hasValidDelegation = await _db.DelegationRules
            .AnyAsync(d => d.DelegateUserId == userId
                           && d.IsActive
                           && now >= d.ValidFromUtc
                           && now <= d.ValidToUtc
                           && (d.DocumentTypeId == null || d.DocumentTypeId == task.Document!.DocumentTypeId)
                           && (d.DelegatorUserId == task.AssignedUserId
                               || _db.ApprovalCandidates.Any(c => c.ApprovalTaskId == task.Id && c.CandidateUserId == d.DelegatorUserId)),
                cancellationToken);

        if (!hasValidDelegation)
            throw new BusinessRuleViolationException("当前用户无权审批该任务（非指派者、候选人，或无有效委托）。");
    }

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
        return _db.WorkflowDefinitionVersions
            .Where(x => x.IsActive)
            .Select(x => x.Id)
            .FirstOrDefault();
    }

    private Guid? GetRoleIdByName(string roleName)
    {
        return _db.Roles
            .Where(x => x.Name == roleName)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefault();
    }

    private static string ResolveStepName(string stepKey)
        => stepKey switch
        {
            "manager-approval" => "经理审批",
            "director-approval" => "部长审批",
            "finance-approval" => "财务审批",
            "business-owner-approval" => "事业部负责人审批",
            "qa-approval" => "品质确认",
            "purchasing-approval" => "采购审批",
            _ => stepKey,
        };

    private static string? ResolveStepRole(string stepKey)
        => stepKey switch
        {
            "manager-approval" => RoleNames.Manager,
            "director-approval" => RoleNames.Manager,
            "finance-approval" => RoleNames.Finance,
            "business-owner-approval" => RoleNames.Manager,
            "qa-approval" => RoleNames.QA,
            "purchasing-approval" => RoleNames.Purchasing,
            _ => RoleNames.Approver,
        };

    private static string Serialize(object obj) => JsonSerializer.Serialize(obj);

    // ===================== 通知辅助 =====================

    /// <summary>通知某审批步骤的候选人（按候选人的邮箱入队通知邮件）。</summary>
    private async Task NotifyApproversAsync(ApprovalTask task, Document document, string correlationId, CancellationToken cancellationToken)
    {
        var candidateUserIds = task.Candidates.Select(c => c.CandidateUserId).ToList();
        if (candidateUserIds.Count == 0 && task.AssignedUserId.HasValue)
            candidateUserIds.Add(task.AssignedUserId.Value);

        if (candidateUserIds.Count == 0)
            return;

        var emails = await _db.Users
            .AsNoTracking()
            .Where(u => candidateUserIds.Contains(u.Id))
            .Select(u => u.Email)
            .ToListAsync(cancellationToken);

        foreach (var email in emails)
        {
            if (string.IsNullOrWhiteSpace(email))
                continue;

            await _emailService.EnqueueAsync(
                email,
                $"【待审批】{document.DocumentNo} - {task.StepName}",
                $"您有一项待审批任务。\n文档编号：{document.DocumentNo}\n标题：{document.Title}\n审批步骤：{task.StepName}",
                correlationId,
                $"notify:{task.Id}:{email}",
                cancellationToken);
        }
    }

    /// <summary>通知申请人（审批完成 / 驳回等）。</summary>
    private async Task NotifyApplicantAsync(Document document, string subject, string body, string correlationId, CancellationToken cancellationToken)
    {
        var applicant = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == document.ApplicantUserId, cancellationToken);

        if (applicant is null || string.IsNullOrWhiteSpace(applicant.Email))
            return;

        await _emailService.EnqueueAsync(
            applicant.Email,
            $"{subject}：{document.DocumentNo}",
            body,
            correlationId,
            $"notify:{document.Id}:{subject}",
            cancellationToken);
    }
}
