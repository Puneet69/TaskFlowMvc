using System.Text.RegularExpressions;
using Microsoft.AspNetCore.StaticFiles;

namespace TaskFlowMvc.Services;

public class LocalFileStorageService(IWebHostEnvironment environment) : IFileStorageService
{
    private static readonly Regex UnsafeChars = new("[^a-zA-Z0-9_.-]", RegexOptions.Compiled);

    public async Task<(string StoredPath, string FileName, string ContentType, long SizeBytes)> SaveAsync(IFormFile file, string folder, CancellationToken cancellationToken = default)
    {
        var root = environment.WebRootPath;
        if (string.IsNullOrWhiteSpace(root))
        {
            root = Path.Combine(environment.ContentRootPath, "wwwroot");
        }

        Directory.CreateDirectory(root);
        var targetFolder = Path.Combine(root, "uploads", folder);
        Directory.CreateDirectory(targetFolder);

        var safeName = UnsafeChars.Replace(Path.GetFileName(file.FileName), "_");
        var uniqueName = $"{DateTime.UtcNow:yyyyMMddHHmmssfff}_{Guid.NewGuid():N}_{safeName}";
        var fullPath = Path.Combine(targetFolder, uniqueName);

        await using (var stream = File.Create(fullPath))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        var relative = Path.Combine("uploads", folder, uniqueName).Replace("\\", "/");
        return ("/" + relative, safeName, file.ContentType ?? "application/octet-stream", file.Length);
    }

    public Task<(Stream Stream, string ContentType, string FileName)> OpenReadAsync(string storedPath, CancellationToken cancellationToken = default)
    {
        var root = environment.WebRootPath;
        if (string.IsNullOrWhiteSpace(root))
        {
            root = Path.Combine(environment.ContentRootPath, "wwwroot");
        }

        var normalized = (storedPath ?? string.Empty).Replace('\\', '/').Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new FileNotFoundException("Attachment path is empty.");
        }

        if (normalized.StartsWith('/'))
        {
            normalized = normalized[1..];
        }

        if (!normalized.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("Attachment path is invalid.");
        }

        var uploadsRoot = Path.GetFullPath(Path.Combine(root, "uploads"));
        var fullPath = Path.GetFullPath(Path.Combine(root, normalized.Replace('/', Path.DirectorySeparatorChar)));
        if (!fullPath.StartsWith(uploadsRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("Attachment path traversal denied.");
        }

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("Attachment not found.", fullPath);
        }

        var provider = new FileExtensionContentTypeProvider();
        if (!provider.TryGetContentType(fullPath, out var contentType))
        {
            contentType = "application/octet-stream";
        }

        var fileName = Path.GetFileName(fullPath);
        Stream stream = new FileStream(
            fullPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 1024 * 64,
            options: FileOptions.Asynchronous | FileOptions.SequentialScan);

        return Task.FromResult((stream, contentType, fileName));
    }
}
