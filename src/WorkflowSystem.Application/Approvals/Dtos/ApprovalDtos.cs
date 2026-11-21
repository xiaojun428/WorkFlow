using WorkflowSystem.Application.Documents.Dtos;
using WorkflowSystem.Domain.Enums;

namespace WorkflowSystem.Application.Approvals.Dtos;

/// <summary>审批任务列表项（待我审批 / 我已审批列表用）。</summary>
public sealed record ApprovalTaskListItemDto(
    Guid Id,
    Guid DocumentId,
    string DocumentNo,
    string DocumentTitle,
    string DocumentTypeName,
    string StepKey,
    string StepName,
    Guid ApplicantUserId,
    string ApplicantName,
    AssignmentType AssignmentType,
    Guid? AssignedUserId,
    Guid? AssignedRoleId,
    Guid? AssignedDepartmentId,
    DateTime? DueAtUtc,
    ApprovalTaskStatus Status,
    DateTime? CompletedAtUtc);

/// <summary>提交申请请求。</summary>
public sealed class SubmitDocumentDto
{
    public string Comment { get; set; } = string.Empty;
}

/// <summary>审批操作请求（通过/驳回/退回）。</summary>
public sealed class ApprovalActionDto
{
    public Guid ApprovalTaskId { get; set; }
    public string Comment { get; set; } = string.Empty;
}

/// <summary>转交请求。</summary>
public sealed class TransferRequestDto
{
    public Guid ApprovalTaskId { get; set; }
    public Guid NewAssigneeUserId { get; set; }
    public string Comment { get; set; } = string.Empty;
}

/// <summary>批量审批请求：一次对多个任务执行通过操作。</summary>
public sealed class BatchApproveRequestDto
{
    public List<Guid> ApprovalTaskIds { get; set; } = new();
    public string Comment { get; set; } = string.Empty;
}

/// <summary>批量审批结果项：单个任务的审批结果。</summary>
public sealed record BatchApproveResultItemDto(
    Guid ApprovalTaskId,
    string DocumentNo,
    bool Success,
    string? Error);

/// <summary>批量审批结果：成功/失败统计与明细。</summary>
public sealed record BatchApproveResultDto(
    int TotalCount,
    int SuccessCount,
    int FailCount,
    IReadOnlyList<BatchApproveResultItemDto> Items);

/// <summary>审批历史条目（时间线展示用）。</summary>
public sealed record ApprovalHistoryItemDto(
    Guid Id,
    ApprovalAction Action,
    Guid OperatorUserId,
    string OperatorName,
    string? FromStatus,
    string? ToStatus,
    string? Comment,
    DateTime OccurredAtUtc);

/// <summary>审批任务详情 DTO（审批页面用，含文档信息与历史）。</summary>
public sealed record ApprovalTaskDetailDto(
    Guid Id,
    Guid DocumentId,
    string DocumentNo,
    string DocumentTitle,
    string DocumentTypeName,
    string StepKey,
    string StepName,
    Guid ApplicantUserId,
    string ApplicantName,
    DateTime? DueAtUtc,
    ApprovalTaskStatus Status,
    DocumentDetailDto Document,
    IReadOnlyList<ApprovalHistoryItemDto> History);
