using System.Text.Json.Serialization;

namespace ChatAIze.GenerativeCS.Models
{
    /// <summary>
    /// Represents a part of a chat message content, which can be text, file data, etc.
    /// </summary>
    public interface IChatContentPart { }

    public class TextPart : IChatContentPart
    {
        [JsonPropertyName("text")]
        public string Text { get; set; }

        public TextPart(string text)
        {
            Text = text;
        }
    }

    public class FileDataPart : IChatContentPart
    {
        [JsonPropertyName("file_data")]
        public FileDataSource FileData { get; set; }

        public FileDataPart(FileDataSource fileData)
        {
            FileData = fileData;
        }
    }

    public class FileDataSource
    {
        [JsonPropertyName("mime_type")]
        public string MimeType { get; set; }

        [JsonPropertyName("file_uri")]
        public string FileUri { get; set; }

        public FileDataSource(string mimeType, string fileUri)
        {
            MimeType = mimeType;
            FileUri = fileUri;
        }
    }
} 