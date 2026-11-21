using WorkflowSystem.Application.Abstractions;

namespace WorkflowSystem.Application.Services;

/// <summary>
/// 系统时钟默认实现：返回 UTC 时间。
/// </summary>
public class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
