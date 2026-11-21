using WorkflowSystem.Application.Delegations.Dtos;

namespace WorkflowSystem.Application.Delegations;

/// <summary>
/// 代理委托服务抽象：管理委托审批规则（带有效期），
/// 支持列表、新建、停用；真正审批时由 ApprovalService 二次校验有效期。
/// </summary>
public interface IDelegationService
{
    /// <summary>查询当前用户的委托规则（作为委托人的委托 + 作为被委托人的委托）。</summary>
    Task<IReadOnlyList<DelegationListItemDto>> GetMyDelegationsAsync(CancellationToken cancellationToken = default);

    /// <summary>新建委托规则。</summary>
    Task<Guid> CreateAsync(DelegationRequestDto dto, CancellationToken cancellationToken = default);

    /// <summary>停用委托规则（仅委托人本人可停用）。</summary>
    Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>获取可选择的被委托用户（除当前用户外的有效用户）。</summary>
    Task<IReadOnlyList<UserOptionDto>> GetAvailableDelegatesAsync(CancellationToken cancellationToken = default);
}
