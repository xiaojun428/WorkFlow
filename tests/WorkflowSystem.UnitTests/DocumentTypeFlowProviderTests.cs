using WorkflowSystem.Application.Services;
using WorkflowSystem.Domain.Constants;
using WorkflowSystem.Domain.Enums;
using Xunit;

namespace WorkflowSystem.UnitTests;

/// <summary>
/// 阶段 10：EOL / 采购 / 供应商审批流程提供者的单元测试。
/// 验证各文档类型的审批步骤序列解析（含采购金额条件分支）。
/// </summary>
public class DocumentTypeFlowProviderTests
{
    private static IReadOnlyDictionary<string, WorkflowSystem.Application.Workflows.DocumentFieldValueSnapshot> Empty()
        => new Dictionary<string, WorkflowSystem.Application.Workflows.DocumentFieldValueSnapshot>();

    private static IReadOnlyDictionary<string, WorkflowSystem.Application.Workflows.DocumentFieldValueSnapshot> Fields(decimal? amount, string? currency)
        => new Dictionary<string, WorkflowSystem.Application.Workflows.DocumentFieldValueSnapshot>
        {
            ["Amount"] = new("Amount", amount, null),
            ["Currency"] = new("Currency", null, currency),
        };

    [Fact]
    public void EolProvider_SupportsOnlyEol()
    {
        var provider = new EolApprovalFlowProvider();
        Assert.True(provider.Supports(DocumentTypeCodes.EOL));
        Assert.False(provider.Supports(DocumentTypeCodes.Budget));
    }

    [Fact]
    public async Task EolProvider_ReturnsManagerThenQa()
    {
        var provider = new EolApprovalFlowProvider();
        var steps = await provider.ResolveStepsAsync(DocumentTypeCodes.EOL, Empty());

        Assert.Equal(2, steps.Count);
        Assert.Equal("manager-approval", steps[0].StepKey);
        Assert.Equal("qa-approval", steps[1].StepKey);
        Assert.Equal(RoleNames.QA, steps[1].RoleName);
    }

    [Fact]
    public async Task PurchaseProvider_LowAmount_OnlyManager()
    {
        var provider = new PurchaseApprovalFlowProvider(new FixedCurrencyService());
        var steps = await provider.ResolveStepsAsync(DocumentTypeCodes.Purchase, Fields(10_000m, "CNY"));

        Assert.Single(steps);
        Assert.Equal("manager-approval", steps[0].StepKey);
    }

    [Fact]
    public async Task PurchaseProvider_HighAmount_ManagerThenFinance()
    {
        var provider = new PurchaseApprovalFlowProvider(new FixedCurrencyService());
        var steps = await provider.ResolveStepsAsync(DocumentTypeCodes.Purchase, Fields(50_000m, "CNY"));

        Assert.Equal(2, steps.Count);
        Assert.Equal("manager-approval", steps[0].StepKey);
        Assert.Equal("finance-approval", steps[1].StepKey);
    }

    [Fact]
    public async Task PurchaseProvider_ForeignCurrency_ConvertsToCny()
    {
        // 50,000 CNY 阈值 = 约 6,944.44 USD（汇率 7.20）。
        var provider = new PurchaseApprovalFlowProvider(new FixedCurrencyService());

        // 低于阈值：6,000 USD = 43,200 CNY。
        var below = await provider.ResolveStepsAsync(DocumentTypeCodes.Purchase, Fields(6_000m, "USD"));
        Assert.Single(below);

        // 高于阈值：7,000 USD = 50,400 CNY。
        var above = await provider.ResolveStepsAsync(DocumentTypeCodes.Purchase, Fields(7_000m, "USD"));
        Assert.Equal(2, above.Count);
    }

    [Fact]
    public void VendorProvider_SupportsOnlyVendor()
    {
        var provider = new VendorApprovalFlowProvider();
        Assert.True(provider.Supports(DocumentTypeCodes.Vendor));
        Assert.False(provider.Supports(DocumentTypeCodes.Purchase));
    }

    [Fact]
    public async Task VendorProvider_ReturnsPurchasingManagerFinance()
    {
        var provider = new VendorApprovalFlowProvider();
        var steps = await provider.ResolveStepsAsync(DocumentTypeCodes.Vendor, Empty());

        Assert.Equal(3, steps.Count);
        Assert.Equal("purchasing-approval", steps[0].StepKey);
        Assert.Equal("manager-approval", steps[1].StepKey);
        Assert.Equal("finance-approval", steps[2].StepKey);
        Assert.Equal(RoleNames.Purchasing, steps[0].RoleName);
    }
}
