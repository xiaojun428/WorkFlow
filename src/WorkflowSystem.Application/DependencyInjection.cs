using Microsoft.Extensions.DependencyInjection;

namespace WorkflowSystem.Application;

/// <summary>
/// Application 层的依赖注入注册入口。
/// 阶段 2 仅注册基础服务；后续阶段在此补充各用例服务的注册。
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // 阶段 3+ 会注册 FluentValidation、MediatR（如需）、各应用服务实现。
        // 当前阶段保持最小可用，确保解决方案可编译、可运行。

        return services;
    }
}
