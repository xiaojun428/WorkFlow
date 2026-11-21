using Microsoft.Extensions.DependencyInjection;

namespace WorkflowSystem.Workflows;

/// <summary>
/// Workflows 层的依赖注入注册入口。
/// 阶段 7 在此注册 Elsa Workflows 3 及自定义 Activity。
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddWorkflows(this IServiceCollection services)
    {
        // 阶段 7+ 注册 Elsa 服务、WorkflowDefinitionProvider、自定义 Activity。

        return services;
    }
}
