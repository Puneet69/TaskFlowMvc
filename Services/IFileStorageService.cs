using Microsoft.AspNetCore.Http;

namespace TaskFlowMvc.Services;

public interface IFileStorageService
{
    Task<(string StoredPath, string FileName, string ContentType, long SizeBytes)> SaveAsync(IFormFile file, string folder, CancellationToken cancellationToken = default);
    Task<(Stream Stream, string ContentType, string FileName)> OpenReadAsync(string storedPath, CancellationToken cancellationToken = default);
}
