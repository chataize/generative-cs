using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ChatAIze.GenerativeCS.Models.Gemini
{
    public class GeminiListFilesResponse
    {
        [JsonPropertyName("files")]
        public List<GeminiFile> Files { get; set; } = new();

        [JsonPropertyName("nextPageToken")]
        public string? NextPageToken { get; set; }
    }
} 