using Microsoft.EntityFrameworkCore;
using WorkflowSystem.Application.Abstractions;
using WorkflowSystem.Application.Delegations;
using WorkflowSystem.Application.Delegations.Dtos;
using WorkflowSystem.Domain.Common;
using WorkflowSystem.Domain.Entities.Workflow;
using WorkflowSystem.Infrastructure.Persistence;

namespace WorkflowSystem.Infrastructure.Services;

/// <summary>
/// 代理委托服务实现：管理委托审批规则。
/// 委托人在有效期内可让被委托人并行审批（需求 5）。
/// </summary>
public class DelegationService : IDelegationService
{
    private readonly WorkflowDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IClock _clock;

    public DelegationService(WorkflowDbContext db, ICurrentUserService currentUser, IClock clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<IReadOnlyList<DelegationListItemDto>> GetMyDelegationsAsync(CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId
            ?? throw new BusinessRuleViolationException("当前用户未认证。");

        return await _db.DelegationRules
            .AsNoTracking()
            .Where(d => d.DelegatorUserId == userId || d.DelegateUserId == userId)
            .OrderByDescending(d => d.ValidFromUtc)
            .Select(d => new DelegationListItemDto(
                d.Id,
                d.DelegatorUserId,
                d.DelegatorUser != null ? d.DelegatorUser.DisplayName : string.Empty,
                d.DelegateUserId,
                d.DelegateUser != null ? d.DelegateUser.DisplayName : string.Empty,
                d.DocumentTypeId,
                d.DocumentType != null ? d.DocumentType.Name : null,
                d.ValidFromUtc,
                d.ValidToUtc,
                d.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<Guid> CreateAsync(DelegationRequestDto dto, CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId
            ?? throw new BusinessRuleViolationException("当前用户未认证。");

        if (dto.DelegateUserId == userId)
            throw new BusinessRuleViolationException("不能委托给自己。");

        if (dto.ValidFrom >= dto.ValidTo)
            throw new BusinessRuleViolationException("委托结束时间必须晚于开始时间。");

        // 被委托人必须存在且有效。
        var delegateExists = await _db.Users
            .AnyAsync(u => u.Id == dto.DelegateUserId && u.IsActive, cancellationToken);
        if (!delegateExists)
            throw new NotFoundException("User", dto.DelegateUserId);

        // 若指定文档类型，必须存在。
        if (dto.DocumentTypeId.HasValue)
        {
            var typeExists = await _db.DocumentTypes
                .AnyAsync(t => t.Id == dto.DocumentTypeId.Value, cancellationToken);
            if (!typeExists)
                throw new NotFoundException("DocumentType", dto.DocumentTypeId.Value);
        }

        var rule = new DelegationRule(
            userId,
            dto.DelegateUserId,
            DateTime.SpecifyKind(dto.ValidFrom, DateTimeKind.Utc),
            DateTime.SpecifyKind(dto.ValidTo, DateTimeKind.Utc),
            dto.DocumentTypeId);

        _db.DelegationRules.Add(rule);
        await _db.SaveChangesAsync(cancellationToken);
        return rule.Id;
    }

    public async Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId
            ?? throw new BusinessRuleViolationException("当前用户未认证。");

        var rule = await _db.DelegationRules
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken)
            ?? throw new NotFoundException("DelegationRule", id);

        if (rule.DelegatorUserId != userId)
            throw new BusinessRuleViolationException("只能停用自己作为委托人的委托规则。");

        rule.Deactivate();
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UserOptionDto>> GetAvailableDelegatesAsync(CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId;

        return await _db.Users
            .AsNoTracking()
            .Where(u => u.IsActive && (userId == null || u.Id != userId.Value))
            .OrderBy(u => u.DisplayName)
            .Select(u => new UserOptionDto(u.Id, u.DisplayName, u.Email))
            .ToListAsync(cancellationToken);
    }
}
