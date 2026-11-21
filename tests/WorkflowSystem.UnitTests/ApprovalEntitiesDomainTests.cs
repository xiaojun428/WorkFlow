using WorkflowSystem.Domain.Entities.Workflow;
using WorkflowSystem.Domain.Enums;
using Xunit;

namespace WorkflowSystem.UnitTests;

/// <summary>
/// 审批候选人、审批历史、工作流实例的领域规则测试。
/// </summary>
public class ApprovalEntitiesDomainTests
{
    [Fact]
    public void ApprovalCandidate_HoldsTaskAndUser()
    {
        var taskId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var candidate = new ApprovalCandidate(taskId, userId);

        Assert.Equal(taskId, candidate.ApprovalTaskId);
        Assert.Equal(userId, candidate.CandidateUserId);
    }

    [Fact]
    public void ApprovalHistory_RecordsFact()
    {
        var docId = Guid.NewGuid();
        var operatorId = Guid.NewGuid();
        var history = new ApprovalHistory(
            docId,
            ApprovalAction.Approved,
            operatorId,
            fromStatus: "Pending",
            toStatus: "Approved",
            comment: "同意");

        Assert.Equal(docId, history.DocumentId);
        Assert.Equal(ApprovalAction.Approved, history.Action);
        Assert.Equal(operatorId, history.OperatorUserId);
        Assert.Equal("Pending", history.FromStatus);
        Assert.Equal("Approved", history.ToStatus);
        Assert.Equal("同意", history.Comment);
    }

    [Fact]
    public void WorkflowInstance_UpdateStatus_ChangesState()
    {
        var instance = new WorkflowInstance(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "elsa-instance-id",
            "correlation-id");

        Assert.Equal(WorkflowStatus.Running, instance.Status);

        instance.UpdateStatus(WorkflowStatus.Completed);
        Assert.Equal(WorkflowStatus.Completed, instance.Status);
    }

    [Fact]
    public void DelegationRule_IsValidAt_BoundaryInclusive()
    {
        var from = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc);
        var rule = new DelegationRule(Guid.NewGuid(), Guid.NewGuid(), from, to);

        // 边界（含起止点）有效。
        Assert.True(rule.IsValidAt(from));
        Assert.True(rule.IsValidAt(to));
    }
}
