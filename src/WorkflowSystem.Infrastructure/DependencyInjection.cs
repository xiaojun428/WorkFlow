using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WorkflowSystem.Application.Abstractions;
using WorkflowSystem.Application.Approvals;
using WorkflowSystem.Application.Audit;
using WorkflowSystem.Application.Delegations;
using WorkflowSystem.Application.Documents;
using WorkflowSystem.Application.Identity;
using WorkflowSystem.Application.Notifications;
using WorkflowSystem.Application.Permissions;
using WorkflowSystem.Application.Workflows;
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

        // 权限服务（阶段 5）。
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IPermissionAdminService, PermissionAdminService>();

        // 外部身份用户同步（阶段 9：Entra 登录后映射到本系统用户）。
        services.AddScoped<IUserSyncService, UserSyncService>();

        // 审批与工作流服务（阶段 7：工作流已切换为 Elsa Workflows 3 实现）。
        services.AddScoped<IWorkflowService, ElsaWorkflowService>();
        services.AddScoped<IApprovalService, ApprovalService>();

        // 代理委托服务（阶段 10：委托审批规则管理）。
        services.AddScoped<IDelegationService, DelegationService>();

        // 审计服务（阶段 6）。
        services.AddScoped<IAuditService, AuditService>();

        // 通知邮件服务（阶段 9：Transactional Outbox + 后台发送）。
        services.AddScoped<IEmailService, EmailService>();
        services.AddSingleton<IMailSender, LoggingMailSender>();

        // 审批催办配置（需求 7：超时 48h，每 24h 催办一次）。
        services.Configure<ApprovalReminderOptions>(configuration.GetSection("ApprovalReminder"));

        // 后台服务（阶段 9：邮件发件箱消费 + 审批催办）。
        services.AddHostedService<MailOutboxProcessor>();
        services.AddHostedService<ApprovalReminderService>();

        return services;
    }
}
