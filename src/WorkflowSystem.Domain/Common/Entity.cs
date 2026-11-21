namespace WorkflowSystem.Domain.Common;

/// <summary>
/// 所有业务实体的基类。主键统一采用 Guid，由应用层通过 SequentialGuid 生成，
/// 保证聚集索引的连续性，同时具备分布式友好、不泄露业务量等优点。
/// </summary>
public abstract class Entity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();

    protected Entity() { }

    protected Entity(Guid id)
    {
        Id = id;
    }
}

/// <summary>
/// 带审计时间戳的实体基类（创建/更新时间，统一保存 UTC）。
/// </summary>
public abstract class AuditableEntity : Entity
{
    public DateTime CreatedAtUtc { get; protected set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; protected set; } = DateTime.UtcNow;

    protected AuditableEntity() { }

    protected AuditableEntity(Guid id) : base(id) { }

    /// <summary>更新最后修改时间（UTC）。</summary>
    public void MarkUpdated() => UpdatedAtUtc = DateTime.UtcNow;
}

/// <summary>
/// 软删除实体基类：普通删除仅打标记，不物理删除。
/// 已提交或已完成的文档不得由普通用户物理删除。
/// </summary>
public abstract class SoftDeletableEntity : AuditableEntity
{
    public bool IsDeleted { get; protected set; }
    public DateTime? DeletedAtUtc { get; protected set; }

    protected SoftDeletableEntity() { }

    protected SoftDeletableEntity(Guid id) : base(id) { }

    /// <summary>软删除。</summary>
    public void SoftDelete()
    {
        if (IsDeleted) return;
        IsDeleted = true;
        DeletedAtUtc = DateTime.UtcNow;
        MarkUpdated();
    }
}
