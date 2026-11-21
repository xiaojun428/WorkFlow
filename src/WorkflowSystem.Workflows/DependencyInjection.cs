using Elsa.Extensions;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Management;
using Elsa.Persistence.EFCore.Modules.Runtime;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WorkflowSystem.Workflows.Definitions;

namespace WorkflowSystem.Workflows;

/// <summary>
/// Workflows 层的依赖注入注册入口。
/// 阶段 7 在此注册 Elsa Workflows 3：核心服务、SQL Server 持久化、编程式工作流定义。
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddWorkflows(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("WorkflowDb")
            ?? throw new InvalidOperationException("缺少连接字符串 WorkflowDb。");

        // 注册 Elsa Workflows 3：Management（定义/实例）与 Runtime（书签/执行日志）持久化到 SQL Server。
        services.AddElsa(elsa => elsa
            .AddActivitiesFrom<WorkflowsMarker>()
            .AddWorkflowsFrom<WorkflowsMarker>()
            .UseWorkflowManagement(management => management
                .UseEntityFrameworkCore(ef =>
                {
                    ef.UseSqlServer(connectionString);
                    ef.RunMigrations = true; // 开发环境自动迁移，生产环境应改为手动迁移。
                }))
            .UseWorkflowRuntime(runtime => runtime
                .UseEntityFrameworkCore(ef =>
                {
                    ef.UseSqlServer(connectionString);
                    ef.RunMigrations = true;
                })));

        return services;
    }
}
