using WorkflowSystem.Domain.Enums;

namespace WorkflowSystem.Application.Delegations.Dtos;

/// <summary>委托规则列表项。</summary>
public sealed record DelegationListItemDto(
    Guid Id,
    Guid DelegatorUserId,
    string DelegatorName,
    Guid DelegateUserId,
    string DelegateName,
    Guid? DocumentTypeId,
    string? DocumentTypeName,
    DateTime ValidFromUtc,
    DateTime ValidToUtc,
    bool IsActive);

/// <summary>新建/编辑委托请求。</summary>
public sealed class DelegationRequestDto
{
    public Guid DelegateUserId { get; set; }
    public Guid? DocumentTypeId { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
}

/// <summary>可选择的被委托用户（用于下拉列表）。</summary>
public sealed record UserOptionDto(Guid Id, string DisplayName, string Email);
