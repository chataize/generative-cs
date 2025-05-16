using ChatAIze.GenerativeCS.Models.Gemini;
using ChatAIze.GenerativeCS.Options.Gemini;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json; 
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace ChatAIze.GenerativeCS.Providers.Gemini
{
    public class FileService : IFileService // Renamed from GeminiFileServiceProvider, implements IFileService
    {
        private readonly HttpClient _httpClient;
        private readonly GeminiOptions _options;
        // private const string GeminiApiKeyHeader = "X-Goog-Api-Key"; // Not used, API key is in query

        public FileService(HttpClient httpClient, IOptions<GeminiOptions> options) // Constructor name matches new class name
        {
            _httpClient = httpClient;
            _options = options.Value;
        }

        public async Task<GeminiFile?> UploadFileAsync(string filePath, string mimeType, string? displayName = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_options.ApiKey))
                throw new InvalidOperationException("API key is not configured for Gemini.");

            displayName ??= Path.GetFileName(filePath);
            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            return await UploadFileAsync(fileStream, Path.GetFileName(filePath), mimeType, displayName, cancellationToken);
        }

        public async Task<GeminiFile?> UploadFileAsync(Stream stream, string fileName, string mimeType, string? displayName = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_options.ApiKey))
                throw new InvalidOperationException("API key is not configured for Gemini.");

            displayName ??= fileName;
            long fileSize = stream.Length;

            var initialRequestUrl = $"{_options.FileApiBaseUrl}/files?key={_options.ApiKey}";

            var initialRequest = new HttpRequestMessage(HttpMethod.Post, initialRequestUrl);
            initialRequest.Headers.Add("X-Goog-Upload-Protocol", "resumable");
            initialRequest.Headers.Add("X-Goog-Upload-Command", "start");
            initialRequest.Headers.Add("X-Goog-Upload-Header-Content-Length", fileSize.ToString());
            initialRequest.Headers.Add("X-Goog-Upload-Header-Content-Type", mimeType);
            
            var uploadRequestData = new GeminiFileUploadRequest { File = new Models.Gemini.FileMetadata { DisplayName = displayName } };
            initialRequest.Content = JsonContent.Create(uploadRequestData, options: new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            initialRequest.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            HttpResponseMessage initialResponse = await _httpClient.SendAsync(initialRequest, cancellationToken);
            if (!initialResponse.IsSuccessStatusCode)
            {
                var errorContent = await initialResponse.Content.ReadAsStringAsync(cancellationToken);
                throw new HttpRequestException($"Failed to initiate file upload. Status: {initialResponse.StatusCode}. Response: {errorContent}");
            }

            if (!initialResponse.Headers.TryGetValues("X-Goog-Upload-URL", out var uploadUrlValues) || !Uri.TryCreate(uploadUrlValues.FirstOrDefault(), UriKind.Absolute, out var uploadUrl))
            {
                throw new HttpRequestException("Failed to get upload URL from response headers.");
            }

            var uploadContent = new StreamContent(stream);
            uploadContent.Headers.ContentType = new MediaTypeHeaderValue(mimeType);

            var uploadRequest = new HttpRequestMessage(HttpMethod.Post, uploadUrl);
            uploadRequest.Headers.Add("X-Goog-Upload-Command", "upload, finalize");
            uploadRequest.Headers.Add("X-Goog-Upload-Offset", "0");
            uploadRequest.Content = uploadContent;

            HttpResponseMessage uploadResponse = await _httpClient.SendAsync(uploadRequest, cancellationToken);

            if (!uploadResponse.IsSuccessStatusCode)
            {
                var errorContent = await uploadResponse.Content.ReadAsStringAsync(cancellationToken);
                throw new HttpRequestException($"Failed to upload file content. Status: {uploadResponse.StatusCode}. Response: {errorContent}");
            }

            var fileUploadWrapper = await uploadResponse.Content.ReadFromJsonAsync<GeminiFileUploadResponseWrapper>(cancellationToken: cancellationToken);
            return fileUploadWrapper?.File;
        }

        public async Task<GeminiFile?> GetFileAsync(string name, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_options.ApiKey))
                throw new InvalidOperationException("API key is not configured for Gemini.");

            var requestUrl = $"{_options.FileApiBaseUrl}/{name.TrimStart('/')}?key={_options.ApiKey}";
            var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);

            HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new HttpRequestException($"Failed to get file metadata. Status: {response.StatusCode}. Response: {errorContent}");
            }
            var fileWrapper = await response.Content.ReadFromJsonAsync<GeminiFileUploadResponseWrapper>(cancellationToken: cancellationToken);
            return fileWrapper?.File;
        }

        public async Task<GeminiListFilesResponse?> ListFilesAsync(int pageSize = 1000, string? pageToken = null, CancellationToken cancellationToken = default)
        {
             if (string.IsNullOrWhiteSpace(_options.ApiKey))
                throw new InvalidOperationException("API key is not configured for Gemini.");

            var requestUrl = $"{_options.FileApiBaseUrl}/files?key={_options.ApiKey}&pageSize={pageSize}";
            if (!string.IsNullOrEmpty(pageToken))
            {
                requestUrl += $"&pageToken={pageToken}";
            }
            var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);

            HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new HttpRequestException($"Failed to list files. Status: {response.StatusCode}. Response: {errorContent}");
            }
            return await response.Content.ReadFromJsonAsync<GeminiListFilesResponse>(cancellationToken: cancellationToken);
        }

        public async Task<bool> DeleteFileAsync(string name, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_options.ApiKey))
                throw new InvalidOperationException("API key is not configured for Gemini.");

            var requestUrl = $"{_options.FileApiBaseUrl}/{name.TrimStart('/')}?key={_options.ApiKey}";
            var request = new HttpRequestMessage(HttpMethod.Delete, requestUrl);

            HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NoContent)
            {
                return true;
            }
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return false;
            
            // var errorContent = await response.Content.ReadAsStringAsync(cancellationToken); // Don't read if already returned true/false
            return false;
        }

        private class GeminiFileUploadResponseWrapper
        {
            [JsonPropertyName("file")]
            required public GeminiFile File { get; set; }
        }
    }
} 