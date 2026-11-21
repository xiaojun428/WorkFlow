using WorkflowSystem.Domain.Common;
using WorkflowSystem.Domain.Entities.Identity;
using WorkflowSystem.Domain.Enums;
using DocumentEntity = WorkflowSystem.Domain.Entities.Document.Document;
using DocumentType = WorkflowSystem.Domain.Entities.Document.DocumentType;

namespace WorkflowSystem.Domain.Entities.Workflow;

/// <summary>
/// 工作流定义（业务侧元数据，与 Elsa 定义解耦）。
/// </summary>
public class WorkflowDefinition : Entity
{
    public string Name { get; private set; } = string.Empty;
    public bool IsPublished { get; private set; }

    private readonly List<WorkflowDefinitionVersion> _versions = new();
    public IReadOnlyCollection<WorkflowDefinitionVersion> Versions => _versions;

    private WorkflowDefinition() { }

    public WorkflowDefinition(string name)
    {
        Name = name;
    }

    public void Publish() => IsPublished = true;
}

/// <summary>
/// 工作流定义版本：每次发布产生新版本，已启动实例继续使用启动时版本。
/// </summary>
public class WorkflowDefinitionVersion : Entity
{
    public Guid WorkflowDefinitionId { get; private set; }
    public int VersionNo { get; private set; }
    public string ElsaDefinitionId { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;

    public WorkflowDefinition? WorkflowDefinition { get; private set; }

    private WorkflowDefinitionVersion() { }

    public WorkflowDefinitionVersion(Guid workflowDefinitionId, int versionNo, string elsaDefinitionId, bool isActive = true)
    {
        WorkflowDefinitionId = workflowDefinitionId;
        VersionNo = versionNo;
        ElsaDefinitionId = elsaDefinitionId;
        IsActive = isActive;
    }
}

/// <summary>
/// 文档类型与工作流定义的映射（一种文档类型绑定一个工作流定义）。
/// </summary>
public class WorkflowDocumentMapping : Entity
{
    public Guid DocumentTypeId { get; private set; }
    public Guid WorkflowDefinitionId { get; private set; }
    public bool IsActive { get; private set; }

    public DocumentType? DocumentType { get; private set; }
    public WorkflowDefinition? WorkflowDefinition { get; private set; }

    private WorkflowDocumentMapping() { }

    public WorkflowDocumentMapping(Guid documentTypeId, Guid workflowDefinitionId, bool isActive = true)
    {
        DocumentTypeId = documentTypeId;
        WorkflowDefinitionId = workflowDefinitionId;
        IsActive = isActive;
    }
}

/// <summary>
/// 工作流实例（业务侧映射，不替代 Elsa 内部实例表）。
/// 保存 DocumentId ↔ ElsaInstanceId ↔ DefinitionVersionId 的映射关系。
/// </summary>
public class WorkflowInstance : Entity
{
    public Guid DocumentId { get; private set; }
    public Guid WorkflowDefinitionVersionId { get; private set; }
    public string ElsaInstanceId { get; private set; } = string.Empty;
    public WorkflowStatus Status { get; private set; } = WorkflowStatus.Running;
    public string CorrelationId { get; private set; } = string.Empty;

    public DocumentEntity? Document { get; private set; }
    public WorkflowDefinitionVersion? WorkflowDefinitionVersion { get; private set; }

    private WorkflowInstance() { }

    public WorkflowInstance(Guid documentId, Guid workflowDefinitionVersionId, string elsaInstanceId, string correlationId)
    {
        DocumentId = documentId;
        WorkflowDefinitionVersionId = workflowDefinitionVersionId;
        ElsaInstanceId = elsaInstanceId;
        CorrelationId = correlationId;
    }

    public void UpdateStatus(WorkflowStatus status) => Status = status;
}

/// <summary>
/// 审批任务（可变的工作项，有生命周期状态流转）。
/// 与 ApprovalHistory（不可变事实）严格分离。
/// </summary>
public class ApprovalTask : Entity
{
    public Guid DocumentId { get; private set; }
    public Guid? WorkflowInstanceId { get; private set; }
    public Guid WorkflowDefinitionVersionId { get; private set; }

    public string StepKey { get; private set; } = string.Empty;
    public string StepName { get; private set; } = string.Empty;

    public AssignmentType AssignmentType { get; private set; }
    public Guid? AssignedUserId { get; private set; }
    public Guid? AssignedRoleId { get; private set; }
    public Guid? AssignedDepartmentId { get; private set; }

    public DateTime? DueAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public ApprovalTaskStatus Status { get; private set; } = ApprovalTaskStatus.Pending;

    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    public DocumentEntity? Document { get; private set; }
    public User? AssignedUser { get; private set; }
    public Role? AssignedRole { get; private set; }
    public Department? AssignedDepartment { get; private set; }

    private readonly List<ApprovalCandidate> _candidates = new();
    public IReadOnlyCollection<ApprovalCandidate> Candidates => _candidates;

    private ApprovalTask() { }

    public ApprovalTask(
        Guid documentId,
        Guid workflowDefinitionVersionId,
        string stepKey,
        string stepName,
        AssignmentType assignmentType,
        Guid? assignedUserId = null,
        Guid? assignedRoleId = null,
        Guid? assignedDepartmentId = null,
        DateTime? dueAtUtc = null)
    {
        DocumentId = documentId;
        WorkflowDefinitionVersionId = workflowDefinitionVersionId;
        StepKey = stepKey;
        StepName = stepName;
        AssignmentType = assignmentType;
        AssignedUserId = assignedUserId;
        AssignedRoleId = assignedRoleId;
        AssignedDepartmentId = assignedDepartmentId;
        DueAtUtc = dueAtUtc;
    }

    /// <summary>审批通过（状态条件更新由 DbContext 层面以 RowVersion + Status=Pending 保证幂等）。</summary>
    public void Approve()
    {
        EnsurePending();
        Status = ApprovalTaskStatus.Approved;
        CompletedAtUtc = DateTime.UtcNow;
    }

    /// <summary>驳回。</summary>
    public void Reject()
    {
        EnsurePending();
        Status = ApprovalTaskStatus.Rejected;
        CompletedAtUtc = DateTime.UtcNow;
    }

    /// <summary>退回申请人。</summary>
    public void Return()
    {
        EnsurePending();
        Status = ApprovalTaskStatus.Returned;
        CompletedAtUtc = DateTime.UtcNow;
    }

    /// <summary>撤回。</summary>
    public void Withdraw()
    {
        EnsurePending();
        Status = ApprovalTaskStatus.Withdrawn;
        CompletedAtUtc = DateTime.UtcNow;
    }

    /// <summary>转交。</summary>
    public void Transfer(Guid newAssignedUserId)
    {
        EnsurePending();
        Status = ApprovalTaskStatus.Transferred;
        CompletedAtUtc = DateTime.UtcNow;
        // 转交后新任务由应用层另行创建，本任务标记为已转交。
        _ = newAssignedUserId;
    }

    /// <summary>取消。</summary>
    public void Cancel()
    {
        Status = ApprovalTaskStatus.Cancelled;
        CompletedAtUtc = DateTime.UtcNow;
    }

    /// <summary>超时。</summary>
    public void Expire()
    {
        EnsurePending();
        Status = ApprovalTaskStatus.Expired;
        CompletedAtUtc = DateTime.UtcNow;
    }

    public void AddCandidate(Guid candidateUserId) => _candidates.Add(new ApprovalCandidate(Id, candidateUserId));

    private void EnsurePending()
    {
        if (Status != ApprovalTaskStatus.Pending)
            throw new BusinessRuleViolationException($"审批任务已处于 {Status} 状态，不能重复操作。");
    }
}

/// <summary>
/// 审批候选人：保存候选审批人。真正审批时必须重新校验操作者是否仍符合候选人与权限规则。
/// </summary>
public class ApprovalCandidate : Entity
{
    public Guid ApprovalTaskId { get; private set; }
    public Guid CandidateUserId { get; private set; }

    public ApprovalTask? ApprovalTask { get; private set; }
    public User? CandidateUser { get; private set; }

    private ApprovalCandidate() { }

    public ApprovalCandidate(Guid approvalTaskId, Guid candidateUserId)
    {
        ApprovalTaskId = approvalTaskId;
        CandidateUserId = candidateUserId;
    }
}

/// <summary>
/// 审批历史：不可变的审批行为事实，只追加写入，普通角色不允许修改或删除。
/// </summary>
public class ApprovalHistory : Entity
{
    public Guid DocumentId { get; private set; }
    public Guid? ApprovalTaskId { get; private set; }
    public ApprovalAction Action { get; private set; }
    public Guid OperatorUserId { get; private set; }
    public string? FromStatus { get; private set; }
    public string? ToStatus { get; private set; }
    public string? Comment { get; private set; }
    public DateTime OccurredAtUtc { get; private set; } = DateTime.UtcNow;

    public DocumentEntity? Document { get; private set; }
    public User? OperatorUser { get; private set; }

    private ApprovalHistory() { }

    public ApprovalHistory(Guid documentId, ApprovalAction action, Guid operatorUserId, string? fromStatus = null, string? toStatus = null, string? comment = null, Guid? approvalTaskId = null)
    {
        DocumentId = documentId;
        Action = action;
        OperatorUserId = operatorUserId;
        FromStatus = fromStatus;
        ToStatus = toStatus;
        Comment = comment;
        ApprovalTaskId = approvalTaskId;
    }
}

/// <summary>
/// 委托审批规则：带有效期，代理期内委托人与被委托人可并行审批。
/// </summary>
public class DelegationRule : Entity
{
    public Guid DelegatorUserId { get; private set; }
    public Guid DelegateUserId { get; private set; }
    public Guid? DocumentTypeId { get; private set; }
    public DateTime ValidFromUtc { get; private set; }
    public DateTime ValidToUtc { get; private set; }
    public bool IsActive { get; private set; } = true;

    public User? DelegatorUser { get; private set; }
    public User? DelegateUser { get; private set; }
    public DocumentType? DocumentType { get; private set; }

    private DelegationRule() { }

    public DelegationRule(Guid delegatorUserId, Guid delegateUserId, DateTime validFromUtc, DateTime validToUtc, Guid? documentTypeId = null)
    {
        if (validToUtc <= validFromUtc) throw new BusinessRuleViolationException("委托结束时间必须晚于开始时间。");
        DelegatorUserId = delegatorUserId;
        DelegateUserId = delegateUserId;
        ValidFromUtc = validFromUtc;
        ValidToUtc = validToUtc;
        DocumentTypeId = documentTypeId;
    }

    /// <summary>判断委托在指定时间是否有效（过期委托不能用于审批）。</summary>
    public bool IsValidAt(DateTime nowUtc)
        => IsActive && nowUtc >= ValidFromUtc && nowUtc <= ValidToUtc;
}
