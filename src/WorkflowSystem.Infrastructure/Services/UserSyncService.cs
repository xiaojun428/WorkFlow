using Microsoft.EntityFrameworkCore;
using WorkflowSystem.Application.Identity;
using WorkflowSystem.Domain.Entities.Identity;
using WorkflowSystem.Infrastructure.Persistence;

namespace WorkflowSystem.Infrastructure.Services;

/// <summary>
/// 外部身份用户同步服务实现：根据 Entra ObjectId 查找或创建本系统 User。
/// 首次登录自动创建（映射 Entra 身份），后续登录复用已有 User。
/// 内部角色/部门/权限由管理员在本系统另行维护（需求 8）。
/// </summary>
public class UserSyncService : IUserSyncService
{
    private readonly WorkflowDbContext _db;

    public UserSyncService(WorkflowDbContext db)
    {
        _db = db;
    }

    public async Task<Guid> EnsureUserAsync(string entraObjectId, string email, string displayName, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(x => x.EntraObjectId == entraObjectId, cancellationToken);

        if (user is null)
        {
            user = new User(entraObjectId, email, displayName);
            _db.Users.Add(user);
            await _db.SaveChangesAsync(cancellationToken);
        }
        else if (user.Email != email || user.DisplayName != displayName)
        {
            // 更新可变属性（邮箱/显示名），EntraObjectId 为稳定标识不变。
            user.UpdateProfile(email, displayName);
            await _db.SaveChangesAsync(cancellationToken);
        }

        return user.Id;
    }
}
