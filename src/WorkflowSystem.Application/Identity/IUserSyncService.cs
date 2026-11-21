namespace WorkflowSystem.Application.Identity;

/// <summary>
/// 外部身份用户同步服务：把 Entra（外部身份）映射/同步到本系统 User 实体。
/// 需求 8：Entra 只做登录认证（提供身份），内部角色/部门/权限由本系统维护。
/// 因此登录后需要把 Entra ObjectId 映射到本系统 User，并将其 Id 作为业务侧用户标识。
/// </summary>
public interface IUserSyncService
{
    /// <summary>
    /// 根据 Entra ObjectId 查找或创建本系统 User，返回业务侧 User Id。
    /// </summary>
    /// <param name="entraObjectId">Entra 的 oid（对象 Id）。</param>
    /// <param name="email">用户邮箱（首选邮箱）。</param>
    /// <param name="displayName">显示名称。</param>
    Task<Guid> EnsureUserAsync(string entraObjectId, string email, string displayName, CancellationToken cancellationToken = default);
}
