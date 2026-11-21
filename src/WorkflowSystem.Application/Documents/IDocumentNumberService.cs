using WorkflowSystem.Application.Documents.Dtos;

namespace WorkflowSystem.Application.Documents;

/// <summary>
/// 文档编号服务：生成并发安全的文档编号（类型码-YYYY-NNNN，每年重置）。
/// </summary>
public interface IDocumentNumberService
{
    /// <summary>为指定文档类型生成下一个编号（并发安全）。</summary>
    Task<string> GenerateAsync(Guid documentTypeId, CancellationToken cancellationToken = default);
}
