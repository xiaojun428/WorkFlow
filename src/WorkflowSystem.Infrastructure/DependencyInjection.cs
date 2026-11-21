using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WorkflowSystem.Infrastructure.Persistence;

namespace WorkflowSystem.Infrastructure;

/// <summary>
/// Infrastructure 层的依赖注入注册入口。
/// 阶段 2 仅注册基础服务；后续阶段在此注册邮件、文件存储、Entra 等实现。
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 注册业务 DbContext（SQL Server）。
        var connectionString = configuration.GetConnectionString("WorkflowDb");
        services.AddDbContext<WorkflowDbContext>(options =>
            options.UseSqlServer(
                connectionString,
                sql => sql.MigrationsAssembly(typeof(WorkflowDbContext).Assembly.FullName)));

        // 阶段 4+ 注册 IFileStorageService、IEmailService 等实现。

        return services;
    }
}
