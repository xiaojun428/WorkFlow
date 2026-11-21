namespace WorkflowSystem.Application.Abstractions;

/// <summary>
/// 文件存储抽象：数据库只保存元数据和存储键，物理文件通过此接口存取。
/// 第一版使用本地受保护目录；预留切换到 SharePoint / Azure Blob 的能力。
/// </summary>
public interface IFileStorageService
{
    /// <summary>保存文件流，返回存储结果（含存储键、大小、哈希）。</summary>
    Task<StoredFile> SaveAsync(Stream content, string fileName, string mimeType, CancellationToken cancellationToken = default);

    /// <summary>按存储键读取文件流。</summary>
    Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default);

    /// <summary>删除文件（软删除场景下可延迟物理删除）。</summary>
    Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default);
}

/// <summary>
/// 文件保存结果：包含存储键、大小、哈希等元数据，用于写入附件表。
/// </summary>
public sealed record StoredFile(
    string StorageKey,
    long SizeBytes,
    string MimeType,
    string Sha256Hash);
