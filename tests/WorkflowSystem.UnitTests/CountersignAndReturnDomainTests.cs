using WorkflowSystem.Domain.Common;
using WorkflowSystem.Domain.Entities.Workflow;
using WorkflowSystem.Domain.Enums;
using Xunit;

namespace WorkflowSystem.UnitTests;

/// <summary>
/// 阶段 8：会签（候选人按比例聚合）、退回上一步、候选人防重复审批的领域规则测试。
/// 不依赖数据库，验证核心业务不变量。
/// </summary>
public class CountersignAndReturnDomainTests
{
    // ===================== 会签聚合 =====================

    [Fact]
    public void SetCandidates_WithThreeCandidates_100Percent_RequiresAll()
    {
        var task = CreateTask(countersignPercentage: 100);
        task.SetCandidates(new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() });

        // 100% 会签：需全部 3 人通过。
        Assert.Equal(3, task.RequiredApprovals);
    }

    [Fact]
    public void SetCandidates_WithThreeCandidates_50Percent_RequiresTwo()
    {
        var task = CreateTask(countersignPercentage: 50);
        task.SetCandidates(new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() });

        // 50% 会签：3 × 50% = 1.5 → 向上取整 = 2。
        Assert.Equal(2, task.RequiredApprovals);
    }

    [Fact]
    public void RecordCandidateApproval_ReachingThreshold_CompletesNode()
    {
        var task = CreateTask(countersignPercentage: 50);
        var c1 = Guid.NewGuid();
        var c2 = Guid.NewGuid();
        var c3 = Guid.NewGuid();
        task.SetCandidates(new[] { c1, c2, c3 });

        // 第 1 人通过：未满足阈值（2），节点仍待处理。
        Assert.False(task.RecordCandidateApproval());
        Assert.Equal(ApprovalTaskStatus.Pending, task.Status);

        // 第 2 人通过：满足阈值，节点通过。
        Assert.True(task.RecordCandidateApproval());
        Assert.Equal(ApprovalTaskStatus.Approved, task.Status);
    }

    [Fact]
    public void Candidate_MarkActed_Twice_Throws()
    {
        var candidate = new ApprovalCandidate(Guid.NewGuid(), Guid.NewGuid());
        candidate.MarkActed(ApprovalTaskStatus.Approved);

        // 同一候选人重复审批应抛异常。
        Assert.Throws<BusinessRuleViolationException>(() => candidate.MarkActed(ApprovalTaskStatus.Approved));
    }

    [Fact]
    public void SetCandidates_NoCandidates_RequiresOne()
    {
        // 无候选展开（单人审批）：所需通过人数恒为 1。
        var task = CreateTask(countersignPercentage: 100);
        task.SetCandidates(Array.Empty<Guid>());

        Assert.Equal(1, task.RequiredApprovals);
    }

    // ===================== 退回上一步 =====================

    [Fact]
    public void ReturnToPrevious_WhenPending_SetsReturned_AndPreservesPreviousStep()
    {
        var task = CreateTask(countersignPercentage: 100, previousStepKey: "manager-approval");
        task.ReturnToPrevious();

        Assert.Equal(ApprovalTaskStatus.Returned, task.Status);
        Assert.Equal("manager-approval", task.PreviousStepKey);
    }

    [Fact]
    public void ReturnToPrevious_Twice_Throws()
    {
        var task = CreateTask(countersignPercentage: 100, previousStepKey: "manager-approval");
        task.ReturnToPrevious();

        Assert.Throws<BusinessRuleViolationException>(() => task.ReturnToPrevious());
    }

    // ===================== 构造函数与元数据 =====================

    [Fact]
    public void Constructor_DefaultCountersign_Is100()
    {
        var task = CreateTask();
        Assert.Equal(100, task.CountersignPercentage);
        Assert.Null(task.PreviousStepKey);
    }

    private static ApprovalTask CreateTask(int countersignPercentage = 100, string? previousStepKey = null)
        => new ApprovalTask(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "director-approval",
            "部长审批",
            AssignmentType.Role,
            assignedRoleId: Guid.NewGuid(),
            dueAtUtc: DateTime.UtcNow.AddHours(48),
            countersignPercentage: countersignPercentage,
            previousStepKey: previousStepKey);
}
