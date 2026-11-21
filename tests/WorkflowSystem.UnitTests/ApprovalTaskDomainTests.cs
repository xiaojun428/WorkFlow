using WorkflowSystem.Domain.Entities.Workflow;
using WorkflowSystem.Domain.Enums;
using Xunit;

namespace WorkflowSystem.UnitTests;

/// <summary>
/// 审批任务与委托的领域规则测试（不依赖数据库，验证核心安全不变量）。
/// </summary>
public class ApprovalTaskDomainTests
{
    [Fact]
    public void Approve_WhenPending_SetsApproved()
    {
        var task = CreatePendingTask();
        task.Approve();
        Assert.Equal(ApprovalTaskStatus.Approved, task.Status);
        Assert.NotNull(task.CompletedAtUtc);
    }

    [Fact]
    public void Approve_Twice_ThrowsBusinessRuleViolation()
    {
        var task = CreatePendingTask();
        task.Approve();
        // 已审批的任务再次审批应抛异常（幂等保护在领域层的最后防线）。
        Assert.Throws<WorkflowSystem.Domain.Common.BusinessRuleViolationException>(() => task.Approve());
    }

    [Fact]
    public void Reject_WhenPending_SetsRejected()
    {
        var task = CreatePendingTask();
        task.Reject();
        Assert.Equal(ApprovalTaskStatus.Rejected, task.Status);
    }

    [Fact]
    public void Transfer_WhenPending_SetsTransferred()
    {
        var task = CreatePendingTask();
        task.Transfer(Guid.NewGuid());
        Assert.Equal(ApprovalTaskStatus.Transferred, task.Status);
    }

    [Fact]
    public void Cancel_SetsCancelled()
    {
        var task = CreatePendingTask();
        task.Cancel();
        Assert.Equal(ApprovalTaskStatus.Cancelled, task.Status);
    }

    private static ApprovalTask CreatePendingTask()
        => new ApprovalTask(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "manager-approval",
            "经理审批",
            AssignmentType.Role,
            assignedRoleId: Guid.NewGuid(),
            dueAtUtc: DateTime.UtcNow.AddHours(48));
}

/// <summary>委托规则有效期测试。</summary>
public class DelegationRuleTests
{
    [Fact]
    public void IsValidAt_WithinRange_ReturnsTrue()
    {
        var now = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc);
        var rule = new DelegationRule(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc));

        Assert.True(rule.IsValidAt(now));
    }

    [Fact]
    public void IsValidAt_Expired_ReturnsFalse()
    {
        // 过期委托不能用于审批。
        var rule = new DelegationRule(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc));

        Assert.False(rule.IsValidAt(new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc)));
    }

    [Fact]
    public void Constructor_WhenEndBeforeStart_Throws()
    {
        Assert.Throws<WorkflowSystem.Domain.Common.BusinessRuleViolationException>(() =>
            new DelegationRule(
                Guid.NewGuid(),
                Guid.NewGuid(),
                new DateTime(2026, 1, 10),
                new DateTime(2026, 1, 1)));
    }
}
