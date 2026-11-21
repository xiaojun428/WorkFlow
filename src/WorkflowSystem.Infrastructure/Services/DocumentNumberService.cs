using Microsoft.EntityFrameworkCore;
using WorkflowSystem.Application.Abstractions;
using WorkflowSystem.Application.Documents;
using WorkflowSystem.Domain.Common;
using WorkflowSystem.Domain.Entities.Document;
using WorkflowSystem.Infrastructure.Persistence;

namespace WorkflowSystem.Infrastructure.Services;

/// <summary>
/// 文档编号生成服务。
/// 编号格式：{类型前缀}-{YYYY}-{NNNN}，每年重置。
/// 并发安全策略：在事务内以 UPDLOCK 锁定序列表行并自增，
/// 配合 (DocumentTypeId, Year, Month) 唯一索引，保证并发下不重复。
/// </summary>
public class DocumentNumberService : IDocumentNumberService
{
    private readonly WorkflowDbContext _db;
    private readonly IClock _clock;

    public DocumentNumberService(WorkflowDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<string> GenerateAsync(Guid documentTypeId, CancellationToken cancellationToken = default)
    {
        var documentType = await _db.DocumentTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == documentTypeId, cancellationToken)
            ?? throw new NotFoundException("DocumentType", documentTypeId);

        var now = _clock.UtcNow;
        var year = now.Year;

        // 在事务内锁定序列表行，防止并发生成重复编号。
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        var sequence = await _db.DocumentNumberSequences
            .FromSqlRaw(
                "SELECT * FROM [DocumentNumberSequences] WITH (UPDLOCK) WHERE [DocumentTypeId] = {0} AND [Year] = {1}",
                documentTypeId, year)
            .FirstOrDefaultAsync(cancellationToken);

        if (sequence is null)
        {
            // 当前年份尚无序列，创建（用当月作为初始月份，后续可扩展为按月）。
            sequence = new DocumentNumberSequence(documentTypeId, year, now.Month);
            _db.DocumentNumberSequences.Add(sequence);
            await _db.SaveChangesAsync(cancellationToken);
            // 重新锁定，确保并发下同一行只被一个事务读取。
            sequence = await _db.DocumentNumberSequences
                .FromSqlRaw(
                    "SELECT * FROM [DocumentNumberSequences] WITH (UPDLOCK) WHERE [DocumentTypeId] = {0} AND [Year] = {1}",
                    documentTypeId, year)
                .FirstAsync(cancellationToken);
        }

        var nextNo = sequence.Next();
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return $"{documentType.NumberPrefix}-{year}-{nextNo:D4}";
    }
}
