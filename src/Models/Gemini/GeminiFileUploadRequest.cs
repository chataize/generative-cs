using System.Text.Json.Serialization;

namespace ChatAIze.GenerativeCS.Models.Gemini
{
    public class FileMetadata
    {
        [JsonPropertyName("display_name")]
        required public string DisplayName { get; set; }
    }

    public class GeminiFileUploadRequest
    {
        [JsonPropertyName("file")]
        required public FileMetadata File { get; set; }
    }
} 