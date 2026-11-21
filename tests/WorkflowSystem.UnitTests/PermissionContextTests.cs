using WorkflowSystem.Application.Permissions;
using WorkflowSystem.Domain.Enums;
using Xunit;

namespace WorkflowSystem.UnitTests;

/// <summary>
/// 权限上下文纯逻辑测试：验证权限快照的判定行为。
/// </summary>
public class PermissionContextTests
{
    [Fact]
    public void HasPermission_WhenContainsCode_ReturnsTrue()
    {
        var ctx = new PermissionContext
        {
            PermissionCodes = new HashSet<string> { "Approve", "Reject" },
        };
        Assert.True(ctx.HasPermission("Approve"));
        Assert.False(ctx.HasPermission("Delete"));
    }

    [Fact]
    public void HasRole_WhenContainsRole_ReturnsTrue()
    {
        var ctx = new PermissionContext
        {
            RoleNames = new HashSet<string> { "Manager", "Finance" },
        };
        Assert.True(ctx.HasRole("Manager"));
        Assert.False(ctx.HasRole("QA"));
    }

    [Fact]
    public void EffectiveScope_DefaultsToSelf()
    {
        var ctx = new PermissionContext();
        Assert.Equal(DataScopeType.Self, ctx.EffectiveScope);
    }

    [Fact]
    public void EffectiveScope_AllIsBroadest()
    {
        var ctx = new PermissionContext { EffectiveScope = DataScopeType.All };
        Assert.Equal(DataScopeType.All, ctx.EffectiveScope);
    }
}
