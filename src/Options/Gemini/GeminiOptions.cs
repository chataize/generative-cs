namespace ChatAIze.GenerativeCS.Options.Gemini
{
    public class GeminiOptions
    {
        public string? ApiKey { get; set; }
        public string? ModelId { get; set; } // e.g., "gemini-1.5-flash"

        private string _fileApiBaseUrl = "https://generativelanguage.googleapis.com/upload/v1beta";
        public string FileApiBaseUrl 
        {
            get => _fileApiBaseUrl;
            set => _fileApiBaseUrl = value.TrimEnd('/');
        }

        private string _generativeApiBaseUrl = "https://generativelanguage.googleapis.com/v1beta";
        public string GenerativeApiBaseUrl
        {
            get => _generativeApiBaseUrl;
            set => _generativeApiBaseUrl = value.TrimEnd('/');
        }
        
        // Default timeout for HTTP requests, in seconds
        public int DefaultTimeoutSeconds { get; set; } = 100;
    }
} 