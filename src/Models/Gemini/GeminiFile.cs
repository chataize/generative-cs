using System;
using System.Text.Json.Serialization;

namespace ChatAIze.GenerativeCS.Models.Gemini
{
    public class GeminiFile
    {
        [JsonPropertyName("name")]
        required public string Name { get; set; }

        [JsonPropertyName("uri")]
        required public string Uri { get; set; }

        [JsonPropertyName("mime_type")]
        required public string MimeType { get; set; }

        [JsonPropertyName("size_bytes")]
        public long? SizeBytes { get; set; }

        [JsonPropertyName("create_time")]
        public DateTime? CreateTime { get; set; }

        [JsonPropertyName("update_time")]
        public DateTime? UpdateTime { get; set; }

        [JsonPropertyName("expiration_time")]
        public DateTime? ExpirationTime { get; set; }

        [JsonPropertyName("sha256_hash")]
        public string? Sha256Hash { get; set; }

        [JsonPropertyName("display_name")]
        public string? DisplayName { get; set; }

        [JsonPropertyName("state")]
        public string? State { get; set; } // e.g., "ACTIVE", "PROCESSING"
    }
} 