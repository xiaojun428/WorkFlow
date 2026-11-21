using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WorkflowSystem.Application.Abstractions;
using WorkflowSystem.Application.Documents;
using WorkflowSystem.Infrastructure.Persistence;
using WorkflowSystem.Infrastructure.Services;

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

        // 文件存储（本地受保护目录，预留切换到 Blob/SharePoint）。
        services.AddOptions<LocalFileStorageOptions>()
            .Bind(configuration.GetSection("FileStorage"));
        services.AddScoped<IFileStorageService, LocalFileStorageService>();

        // 文档相关服务。
        services.AddScoped<IDocumentNumberService, DocumentNumberService>();
        services.AddScoped<IDocumentService, DocumentService>();

        // 阶段 5+ 注册 IEmailService、权限服务等实现。

        return services;
    }
}
