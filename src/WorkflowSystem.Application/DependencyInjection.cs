using Microsoft.Extensions.DependencyInjection;
using WorkflowSystem.Application.Abstractions;
using WorkflowSystem.Application.Services;
using WorkflowSystem.Application.Workflows;

namespace WorkflowSystem.Application;

/// <summary>
/// Application 层的依赖注入注册入口。
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // 系统时钟。
        services.AddSingleton<IClock, SystemClock>();

        // 币种汇率服务（阶段 8：多币种金额折算）。
        services.AddScoped<ICurrencyService, FixedCurrencyService>();

        // 审批流程定义提供者（阶段 8/10：金额条件分支 / 多级审批 / 会签，按文档类型分发）。
        services.AddScoped<IApprovalFlowProvider, BudgetApprovalFlowProvider>();
        services.AddScoped<IApprovalFlowProvider, EolApprovalFlowProvider>();
        services.AddScoped<IApprovalFlowProvider, PurchaseApprovalFlowProvider>();
        services.AddScoped<IApprovalFlowProvider, VendorApprovalFlowProvider>();

        return services;
    }
}
