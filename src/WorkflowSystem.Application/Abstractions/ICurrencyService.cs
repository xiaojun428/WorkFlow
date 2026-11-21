using WorkflowSystem.Domain.Enums;

namespace WorkflowSystem.Application.Abstractions;

/// <summary>
/// 币种汇率服务抽象：负责多币种金额的汇率折算。
/// 金额条件分支（如预算金额阈值）统一折算到基准币（CNY）后再比较，避免不同币种直接比较。
/// 汇率来源可替换（固定表 / 外部汇率源），阶段 8 提供固定汇率实现。
/// </summary>
public interface ICurrencyService
{
    /// <summary>
    /// 将指定币种的金额折算为基准币（CNY）金额。
    /// 未知币种按 CNY 处理（视作基准币，不折算）。
    /// </summary>
    /// <param name="amount">原始金额。</param>
    /// <param name="currency">原始币种。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>折算到基准币后的金额。</returns>
    Task<decimal> ConvertToBaseAsync(decimal amount, CurrencyCode currency, CancellationToken cancellationToken = default);
}
