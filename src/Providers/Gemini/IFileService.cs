using System.Threading;
using System.Threading.Tasks;
using ChatAIze.GenerativeCS.Models.Gemini;

namespace ChatAIze.GenerativeCS.Providers.Gemini
{
    public interface IFileService // Renamed from IGeminiFileServiceProvider
    {
        Task<GeminiFile?> UploadFileAsync(string filePath, string mimeType, string? displayName = null, CancellationToken cancellationToken = default);
        Task<GeminiFile?> UploadFileAsync(System.IO.Stream stream, string fileName, string mimeType, string? displayName = null, CancellationToken cancellationToken = default);
        Task<GeminiFile?> GetFileAsync(string name, CancellationToken cancellationToken = default);
        Task<GeminiListFilesResponse?> ListFilesAsync(int pageSize = 1000, string? pageToken = null, CancellationToken cancellationToken = default);
        Task<bool> DeleteFileAsync(string name, CancellationToken cancellationToken = default);
    }
} 