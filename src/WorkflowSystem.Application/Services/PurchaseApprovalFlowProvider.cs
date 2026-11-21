using WorkflowSystem.Application.Abstractions;
using WorkflowSystem.Application.Workflows;
using WorkflowSystem.Domain.Constants;
using WorkflowSystem.Domain.Enums;

namespace WorkflowSystem.Application.Services;

/// <summary>
/// 采购申请的审批流程定义提供者。
/// 按采购金额（多币种折算到 CNY）条件分支：
///
/// - Amount &lt; 50,000：经理审批 → 完成。
/// - Amount ≥ 50,000：经理 → 财务 → 完成。
///
/// 采购金额阈值与预算（Budget）独立配置，体现「流程定义可配置」。
/// </summary>
public class PurchaseApprovalFlowProvider : IApprovalFlowProvider
{
    private const decimal FinanceThreshold = 50_000m;

    private readonly ICurrencyService _currencyService;

    public PurchaseApprovalFlowProvider(ICurrencyService currencyService)
    {
        _currencyService = currencyService;
    }

    public bool Supports(string documentTypeCode)
        => string.Equals(documentTypeCode, DocumentTypeCodes.Purchase, StringComparison.OrdinalIgnoreCase);

    public async Task<IReadOnlyList<ApprovalStepDefinition>> ResolveStepsAsync(
        string documentTypeCode,
        IReadOnlyDictionary<string, DocumentFieldValueSnapshot> fieldValues,
        CancellationToken cancellationToken = default)
    {
        var amount = fieldValues.TryGetValue("Amount", out var amt) ? (amt.Number ?? 0m) : 0m;
        var currency = ParseCurrency(fieldValues);
        var amountInCny = await _currencyService.ConvertToBaseAsync(amount, currency, cancellationToken);

        var steps = new List<ApprovalStepDefinition>
        {
            new("manager-approval", "经理审批", RoleNames.Manager, 100),
        };

        if (amountInCny >= FinanceThreshold)
        {
            steps.Add(new("finance-approval", "财务审批", RoleNames.Finance, 100));
        }

        return steps;
    }

    private static CurrencyCode ParseCurrency(IReadOnlyDictionary<string, DocumentFieldValueSnapshot> fieldValues)
    {
        if (!fieldValues.TryGetValue("Currency", out var cur) || string.IsNullOrWhiteSpace(cur.Text))
            return CurrencyCode.CNY;

        return Enum.TryParse<CurrencyCode>(cur.Text, ignoreCase: true, out var code)
            ? code
            : CurrencyCode.CNY;
    }
}
