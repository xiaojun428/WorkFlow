using WorkflowSystem.Application.Abstractions;
using WorkflowSystem.Application.Workflows;
using WorkflowSystem.Domain.Constants;
using WorkflowSystem.Domain.Enums;

namespace WorkflowSystem.Application.Services;

/// <summary>
/// 预算申请（Budget）的审批流程定义提供者。
/// 根据金额字段值（多币种折算到基准币 CNY 后比较）解析审批步骤序列：
///
/// - Amount &lt; 10,000：经理审批 → 完成。
/// - 10,000 ≤ Amount &lt; 100,000：经理 → 部长。
/// - Amount ≥ 100,000：经理 → 部长 → 财务 → 事业部负责人。
///
/// 这是「金额条件分支」的可配置实现：金额阈值与角色映射集中于此，禁止硬编码进引擎或应用服务。
/// 会签：各步骤默认单人审批（比例 100%），可按需配置会签比例。
/// </summary>
public class BudgetApprovalFlowProvider : IApprovalFlowProvider
{
    // 金额阈值（基准币 CNY）。可后续迁移到配置/数据表。
    private const decimal HighThreshold = 100_000m;
    private const decimal MediumThreshold = 10_000m;

    private readonly ICurrencyService _currencyService;

    public BudgetApprovalFlowProvider(ICurrencyService currencyService)
    {
        _currencyService = currencyService;
    }

    public bool Supports(string documentTypeCode)
        => string.Equals(documentTypeCode, DocumentTypeCodes.Budget, StringComparison.OrdinalIgnoreCase);

    public async Task<IReadOnlyList<ApprovalStepDefinition>> ResolveStepsAsync(
        string documentTypeCode,
        IReadOnlyDictionary<string, DocumentFieldValueSnapshot> fieldValues,
        CancellationToken cancellationToken = default)
    {
        // 读取金额与币种字段（FieldKey: Amount / Currency）。
        var amount = fieldValues.TryGetValue("Amount", out var amt) ? (amt.Number ?? 0m) : 0m;
        var currency = ParseCurrency(fieldValues);

        // 折算到基准币 CNY 后比较（多币种支持）。
        var amountInCny = await _currencyService.ConvertToBaseAsync(amount, currency, cancellationToken);

        var steps = new List<ApprovalStepDefinition>
        {
            // 第一步恒为经理审批（单人审批，比例 100%）。
            new("manager-approval", "经理审批", RoleNames.Manager, 100),
        };

        if (amountInCny >= HighThreshold)
        {
            // 大额：经理 → 部长 → 财务 → 事业部负责人。
            steps.Add(new("director-approval", "部长审批", RoleNames.Manager, 100));
            steps.Add(new("finance-approval", "财务审批", RoleNames.Finance, 100));
            steps.Add(new("business-owner-approval", "事业部负责人审批", RoleNames.Manager, 100));
        }
        else if (amountInCny >= MediumThreshold)
        {
            // 中额：经理 → 部长。
            steps.Add(new("director-approval", "部长审批", RoleNames.Manager, 100));
        }

        // 小额：仅经理审批，无后续节点。

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
