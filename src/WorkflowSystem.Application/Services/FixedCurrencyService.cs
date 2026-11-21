using WorkflowSystem.Application.Abstractions;
using WorkflowSystem.Domain.Enums;

namespace WorkflowSystem.Application.Services;

/// <summary>
/// 币种汇率服务实现：固定汇率表（阶段 8）。
/// 后续可替换为外部汇率源（如每日汇率 API）或可配置的汇率表（ExchangeRate）。
/// 汇率含义：1 单位外币 = N 元人民币（CNY 为基准币）。
/// </summary>
public class FixedCurrencyService : ICurrencyService
{
    private static readonly IReadOnlyDictionary<CurrencyCode, decimal> RatesToCny = new Dictionary<CurrencyCode, decimal>
    {
        [CurrencyCode.CNY] = 1.0m,
        [CurrencyCode.USD] = 7.20m,
        [CurrencyCode.EUR] = 7.85m,
        [CurrencyCode.JPY] = 0.048m,
        [CurrencyCode.GBP] = 9.15m,
        [CurrencyCode.HKD] = 0.92m,
    };

    public Task<decimal> ConvertToBaseAsync(decimal amount, CurrencyCode currency, CancellationToken cancellationToken = default)
    {
        if (!RatesToCny.TryGetValue(currency, out var rate))
            rate = 1.0m; // 未知币种视为基准币，不折算。
        return Task.FromResult(decimal.Round(amount * rate, 2, MidpointRounding.AwayFromZero));
    }
}
