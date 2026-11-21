using WorkflowSystem.Application.Approvals.Dtos;
using WorkflowSystem.Application.Documents.Dtos;

namespace WorkflowSystem.Application.Approvals;

/// <summary>
/// 审批应用服务：负责审批任务、候选人、审批历史的用例编排。
/// 这是审批闭环的核心，Controller 只调用此接口。
/// 注意：阶段 6 为「不依赖 Elsa 的最小闭环」，流程推进由最小工作流服务模拟；阶段 7 替换为 Elsa。
/// </summary>
public interface IApprovalService
{
    /// <summary>提交申请：创建审批任务 + 候选人 + 历史，并将文档置为审批中。</summary>
    Task<ApprovalTaskListItemDto> SubmitAsync(Guid documentId, SubmitDocumentDto dto, CancellationToken cancellationToken = default);

    /// <summary>审批通过：推进到下一节点或完成流程。</summary>
    Task ApproveAsync(ApprovalActionDto dto, CancellationToken cancellationToken = default);

    /// <summary>驳回：终止流程，文档置为已驳回。</summary>
    Task RejectAsync(ApprovalActionDto dto, CancellationToken cancellationToken = default);

    /// <summary>退回申请人：文档置为退回，申请人可编辑后重新提交。</summary>
    Task ReturnAsync(ApprovalActionDto dto, CancellationToken cancellationToken = default);

    /// <summary>申请人撤回：文档置为已撤回，审批任务取消。</summary>
    Task WithdrawAsync(Guid documentId, CancellationToken cancellationToken = default);

    /// <summary>转交审批：当前任务标记转交，创建新任务给新审批人。</summary>
    Task TransferAsync(TransferRequestDto dto, CancellationToken cancellationToken = default);

    /// <summary>查询待我审批的任务列表。</summary>
    Task<PagedResult<ApprovalTaskListItemDto>> GetPendingTasksAsync(DocumentQuery query, CancellationToken cancellationToken = default);

    /// <summary>查询我已审批的任务列表。</summary>
    Task<PagedResult<ApprovalTaskListItemDto>> GetProcessedTasksAsync(DocumentQuery query, CancellationToken cancellationToken = default);

    /// <summary>获取审批任务详情（含文档与历史时间线）。</summary>
    Task<ApprovalTaskDetailDto?> GetTaskDetailAsync(Guid taskId, CancellationToken cancellationToken = default);

    /// <summary>获取某文档的审批历史时间线。</summary>
    Task<IReadOnlyList<ApprovalHistoryItemDto>> GetHistoryAsync(Guid documentId, CancellationToken cancellationToken = default);
}
