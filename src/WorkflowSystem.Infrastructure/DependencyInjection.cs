using Microsoft.Extensions.DependencyInjection;

namespace WorkflowSystem.Infrastructure;

/// <summary>
/// Infrastructure 层的依赖注入注册入口。
/// 阶段 2 仅注册基础服务；后续阶段在此注册 DbContext、邮件、文件存储、Entra 等实现。
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services)
    {
        // 阶段 3+ 注册 DbContext（SQL Server）、IFileStorageService、IEmailService 等实现。

        return services;
    }
}
