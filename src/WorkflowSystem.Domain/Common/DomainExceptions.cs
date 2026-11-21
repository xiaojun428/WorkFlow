namespace WorkflowSystem.Domain.Common;

/// <summary>
/// 领域异常基类：用于表达业务规则违反，区别于系统异常。
/// 应用层捕获后映射为 400/409 等语义化响应。
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }
}

/// <summary>资源不存在（映射 404）。</summary>
public class NotFoundException : DomainException
{
    public NotFoundException(string entityName, Guid id)
        : base($"{entityName}（Id={id}）不存在。") { }
}

/// <summary>资源已存在 / 唯一约束冲突（映射 409）。</summary>
public class DuplicateException : DomainException
{
    public DuplicateException(string message) : base(message) { }
}

/// <summary>业务规则违反（映射 400）。</summary>
public class BusinessRuleViolationException : DomainException
{
    public BusinessRuleViolationException(string message) : base(message) { }
}

/// <summary>并发冲突：RowVersion 不匹配（映射 409）。</summary>
public class ConcurrencyConflictException : DomainException
{
    public ConcurrencyConflictException(string message) : base(message) { }
}
