using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using WorkflowSystem.Application.Abstractions;
using WorkflowSystem.Domain.Common;

namespace WorkflowSystem.Infrastructure.Services;

/// <summary>
/// 本地文件存储配置。
/// </summary>
public sealed class LocalFileStorageOptions
{
    /// <summary>存储根目录（受保护目录，不对外公开）。</summary>
    public string RootPath { get; set; } = "uploads";

    /// <summary>允许的文件扩展名白名单。</summary>
    public string[] AllowedExtensions { get; set; } = { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".png", ".jpg", ".jpeg", ".txt", ".zip" };

    /// <summary>允许的 MIME 类型白名单。</summary>
    public string[] AllowedMimeTypes { get; set; } = {
        "application/pdf",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-excel",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "application/vnd.ms-powerpoint",
        "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        "image/png", "image/jpeg", "text/plain", "application/zip"
    };

    /// <summary>最大文件大小（字节），默认 20MB。</summary>
    public long MaxFileSizeBytes { get; set; } = 20 * 1024 * 1024;
}

/// <summary>
/// 本地文件存储实现。
/// 存储键为相对路径（GUID + 安全扩展名），不依赖具体物理路径；
/// 预留切换到 SharePoint / Azure Blob 的能力（只需替换此实现）。
/// 安全要点：校验扩展名/MIME/大小，防路径穿越，目录不对外公开。
/// </summary>
public class LocalFileStorageService : IFileStorageService
{
    private readonly LocalFileStorageOptions _options;
    private readonly string _rootPath;

    public LocalFileStorageService(IOptions<LocalFileStorageOptions> options)
    {
        _options = options.Value;
        _rootPath = Path.GetFullPath(_options.RootPath);
        // 确保受保护目录存在。
        Directory.CreateDirectory(_rootPath);
    }

    public async Task<StoredFile> SaveAsync(Stream content, string fileName, string mimeType, CancellationToken cancellationToken = default)
    {
        // 1. 文件名安全校验：去除路径、仅保留文件名。
        var safeName = SanitizeFileName(fileName);
        var extension = Path.GetExtension(safeName).ToLowerInvariant();

        // 2. 扩展名白名单。
        if (!_options.AllowedExtensions.Contains(extension))
            throw new BusinessRuleViolationException($"不支持的文件类型：{extension}");

        // 3. MIME 白名单。
        if (!_options.AllowedMimeTypes.Contains(mimeType, StringComparer.OrdinalIgnoreCase))
            throw new BusinessRuleViolationException($"不支持的 MIME 类型：{mimeType}");

        // 4. 大小校验（先检查 ContentLength，再实际读取时限制）。
        if (content.CanSeek && content.Length > _options.MaxFileSizeBytes)
            throw new BusinessRuleViolationException($"文件大小超过限制（{_options.MaxFileSizeBytes / 1024 / 1024}MB）。");

        // 5. 生成存储键（GUID + 安全扩展名），不暴露原始文件名。
        var storageKey = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(_rootPath, storageKey);

        // 6. 防止路径穿越：确保最终路径仍在根目录内。
        var fullRoot = Path.GetFullPath(_rootPath);
        var fullDestination = Path.GetFullPath(fullPath);
        if (!fullDestination.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
            throw new BusinessRuleViolationException("非法的文件路径。");

        // 7. 写入文件并计算 SHA256 哈希与大小。
        long sizeBytes = 0;
        using var sha256 = SHA256.Create();
        await using (var fileStream = new FileStream(fullDestination, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true))
        await using (var cryptoStream = new CryptoStream(fileStream, sha256, CryptoStreamMode.Write))
        {
            var buffer = new byte[81920];
            int read;
            while ((read = await content.ReadAsync(buffer, cancellationToken)) > 0)
            {
                sizeBytes += read;
                if (sizeBytes > _options.MaxFileSizeBytes)
                {
                    // 超出大小限制，删除已写文件并抛错。
                    await fileStream.FlushAsync(cancellationToken);
                    File.Delete(fullDestination);
                    throw new BusinessRuleViolationException($"文件大小超过限制（{_options.MaxFileSizeBytes / 1024 / 1024}MB）。");
                }
                await cryptoStream.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            }
            await cryptoStream.FlushFinalBlockAsync(cancellationToken);
        }

        var hash = Convert.ToHexString(sha256.Hash ?? Array.Empty<byte>());

        return new StoredFile(storageKey, sizeBytes, mimeType, hash);
    }

    public Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        // 防路径穿越：存储键必须是安全文件名，不含路径分隔符。
        if (string.IsNullOrWhiteSpace(storageKey) || storageKey.Contains('/') || storageKey.Contains('\\') || storageKey.Contains(".."))
            throw new BusinessRuleViolationException("非法的存储键。");

        var fullPath = Path.Combine(_rootPath, storageKey);
        if (!File.Exists(fullPath))
            throw new NotFoundException("DocumentFile", Guid.Empty);

        return Task.FromResult<Stream>(new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true));
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storageKey) || storageKey.Contains('/') || storageKey.Contains('\\') || storageKey.Contains(".."))
            throw new BusinessRuleViolationException("非法的存储键。");

        var fullPath = Path.Combine(_rootPath, storageKey);
        if (File.Exists(fullPath))
            File.Delete(fullPath);

        return Task.CompletedTask;
    }

    /// <summary>净化文件名：仅保留文件名部分，移除路径与危险字符。</summary>
    private static string SanitizeFileName(string fileName)
    {
        var name = Path.GetFileName(fileName ?? string.Empty);
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return string.IsNullOrWhiteSpace(name) ? "unnamed" : name;
    }
}
