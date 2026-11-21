using WorkflowSystem.Domain.Entities.Document;
using WorkflowSystem.Domain.Enums;
using Xunit;

namespace WorkflowSystem.UnitTests;

/// <summary>
/// 文档状态机领域规则测试：验证提交/撤回/状态流转的幂等与安全约束。
/// </summary>
public class DocumentStateMachineTests
{
    [Fact]
    public void Submit_WhenDraft_TransitionsToInApproval()
    {
        var doc = CreateDocument();
        doc.Submit();
        Assert.Equal(DocumentStatus.InApproval, doc.DocumentStatus);
        Assert.Equal(WorkflowStatus.Running, doc.WorkflowStatus);
        Assert.NotNull(doc.SubmittedAtUtc);
    }

    [Fact]
    public void Submit_WhenAlreadyInApproval_Throws()
    {
        // 防止重复提交。
        var doc = CreateDocument();
        doc.Submit();
        Assert.Throws<WorkflowSystem.Domain.Common.BusinessRuleViolationException>(() => doc.Submit());
    }

    [Fact]
    public void MarkRejected_TransitionsToRejectedAndCompleted()
    {
        var doc = CreateDocument();
        doc.Submit();
        doc.MarkRejected();
        Assert.Equal(DocumentStatus.Rejected, doc.DocumentStatus);
        Assert.Equal(WorkflowStatus.Completed, doc.WorkflowStatus);
    }

    [Fact]
    public void MarkReturned_TransitionsToReturned()
    {
        var doc = CreateDocument();
        doc.Submit();
        doc.MarkReturned();
        Assert.Equal(DocumentStatus.Returned, doc.DocumentStatus);
    }

    [Fact]
    public void MarkWithdrawn_TransitionsToWithdrawn()
    {
        var doc = CreateDocument();
        doc.Submit();
        doc.MarkWithdrawn();
        Assert.Equal(DocumentStatus.Withdrawn, doc.DocumentStatus);
        Assert.Equal(WorkflowStatus.Completed, doc.WorkflowStatus);
    }

    private static Document CreateDocument()
        => new Document(
            "BG-2026-0001",
            Guid.NewGuid(),
            "测试预算申请",
            Guid.NewGuid(),
            Guid.NewGuid());
}
