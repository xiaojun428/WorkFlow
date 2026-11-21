using Microsoft.Extensions.DependencyInjection;
using WorkflowSystem.Application.Abstractions;
using WorkflowSystem.Application.Services;

namespace WorkflowSystem.Application;

/// <summary>
/// Application 层的依赖注入注册入口。
/// 阶段 2 仅注册基础服务；后续阶段在此补充各用例服务的注册。
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // 系统时钟。
        services.AddSingleton<IClock, SystemClock>();

        // 阶段 5+ 注册权限服务、验证器等。

        return services;
    }
}
