namespace WorkflowSystem.Application.Abstractions;

/// <summary>
/// 时钟抽象：统一时间来源，便于测试。所有业务时间取 UTC。
/// </summary>
public interface IClock
{
    DateTime UtcNow { get; }
}

/// <summary>
/// 当前用户上下文：提供当前登录用户身份，便于权限判定与审计。
/// 阶段 9 接入 Entra 前，由开发身份服务提供。
/// </summary>
public interface ICurrentUserService
{
    /// <summary>当前用户 Id（未认证时为 null）。</summary>
    Guid? UserId { get; }

    /// <summary>当前用户是否已认证。</summary>
    bool IsAuthenticated { get; }

    /// <summary>当前用户邮箱。</summary>
    string? Email { get; }
}
