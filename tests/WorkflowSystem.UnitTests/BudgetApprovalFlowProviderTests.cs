using WorkflowSystem.Application.Services;
using WorkflowSystem.Domain.Enums;
using Xunit;

namespace WorkflowSystem.UnitTests;

/// <summary>
/// 阶段 8：金额条件分支（Budget 审批流程）与多币种汇率的单元测试。
/// 验证预算金额阈值正确映射到审批步骤序列，以及多币种折算到基准币 CNY。
/// </summary>
public class BudgetApprovalFlowProviderTests
{
    private readonly BudgetApprovalFlowProvider _provider;

    public BudgetApprovalFlowProviderTests()
    {
        _provider = new BudgetApprovalFlowProvider(new FixedCurrencyService());
    }

    private static IReadOnlyDictionary<string, WorkflowSystem.Application.Workflows.DocumentFieldValueSnapshot> Amount(decimal amount, string currency = "CNY")
        => new Dictionary<string, WorkflowSystem.Application.Workflows.DocumentFieldValueSnapshot>
        {
            ["Amount"] = new("Amount", amount, null),
            ["Currency"] = new("Currency", null, currency),
        };

    [Fact]
    public async Task SmallAmount_OnlyManagerApproval()
    {
        // < 1 万：仅经理审批。
        var steps = await _provider.ResolveStepsAsync("Budget", Amount(5_000m));
        Assert.Single(steps);
        Assert.Equal("manager-approval", steps[0].StepKey);
    }

    [Fact]
    public async Task MediumAmount_ManagerThenDirector()
    {
        // 1 万 ≤ amount < 10 万：经理 → 部长。
        var steps = await _provider.ResolveStepsAsync("Budget", Amount(50_000m));
        Assert.Equal(2, steps.Count);
        Assert.Equal("manager-approval", steps[0].StepKey);
        Assert.Equal("director-approval", steps[1].StepKey);
    }

    [Fact]
    public async Task HighAmount_FullChain()
    {
        // ≥ 10 万：经理 → 部长 → 财务 → 事业部负责人。
        var steps = await _provider.ResolveStepsAsync("Budget", Amount(150_000m));
        Assert.Equal(4, steps.Count);
        Assert.Equal("finance-approval", steps[2].StepKey);
        Assert.Equal("business-owner-approval", steps[3].StepKey);
    }

    [Fact]
    public async Task MultiCurrency_ConvertedBeforeCompare()
    {
        // 1 万 USD ≈ 7.2 万 CNY：应命中「中额」分支（经理 → 部长）。
        var steps = await _provider.ResolveStepsAsync("Budget", Amount(10_000m, "USD"));
        Assert.Equal(2, steps.Count);
        Assert.Equal("director-approval", steps[1].StepKey);
    }

    [Fact]
    public async Task ThresholdBoundary_Exactly10000_MediumBranch()
    {
        // 边界：正好 1 万 → 中额分支（经理 → 部长）。
        var steps = await _provider.ResolveStepsAsync("Budget", Amount(10_000m));
        Assert.Equal(2, steps.Count);
    }
}

/// <summary>汇率折算测试。</summary>
public class FixedCurrencyServiceTests
{
    private readonly FixedCurrencyService _service = new();

    [Fact]
    public async Task Convert_Usd_ToCny()
    {
        var result = await _service.ConvertToBaseAsync(100m, CurrencyCode.USD);
        Assert.Equal(720.00m, result); // 100 × 7.20
    }

    [Fact]
    public async Task Convert_Cny_Unchanged()
    {
        var result = await _service.ConvertToBaseAsync(100m, CurrencyCode.CNY);
        Assert.Equal(100m, result);
    }

    [Fact]
    public async Task Convert_UnknownCurrency_TreatedAsBase()
    {
        var result = await _service.ConvertToBaseAsync(100m, (CurrencyCode)999);
        Assert.Equal(100m, result);
    }
}
