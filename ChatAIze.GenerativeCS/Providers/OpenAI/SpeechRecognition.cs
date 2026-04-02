using System.Globalization;
using System.Net.Http.Headers;
using ChatAIze.GenerativeCS.Enums;
using ChatAIze.GenerativeCS.Options.OpenAI;
using ChatAIze.GenerativeCS.Utilities;

namespace ChatAIze.GenerativeCS.Providers.OpenAI;

/// <summary>
/// Handles OpenAI speech recognition and translation requests.
/// </summary>
internal static class SpeechRecognition
{
    /// <summary>
    /// Transcribes audio bytes to text.
    /// </summary>
    /// <param name="audio">Audio payload.</param>
    /// <param name="apiKey">API key used for the request.</param>
    /// <param name="options">Optional transcription options.</param>
    /// <param name="fileName">Logical file name sent to the provider for content-type inference.</param>
    /// <param name="httpClient">HTTP client to use.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Text transcript of the audio.</returns>
    internal static async Task<string> TranscriptAsync(byte[] audio, string? apiKey, TranscriptionOptions? options = null, string fileName = "audio.mp3", HttpClient? httpClient = null, CancellationToken cancellationToken = default)
    {
        options ??= new();
        httpClient ??= new()
        {
            Timeout = TimeSpan.FromMinutes(15)
        };

        if (!string.IsNullOrWhiteSpace(options.ApiKey))
        {
            apiKey = options.ApiKey;
        }

        using var response = await httpClient.RepeatPostAsync("https://api.openai.com/v1/audio/transcriptions", () => CreateTranscriptionRequest(audio, options, fileName), apiKey, options.MaxAttempts, cancellationToken);

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    /// <summary>
    /// Translates audio bytes to English text.
    /// </summary>
    /// <param name="audio">Audio payload.</param>
    /// <param name="apiKey">API key used for the request.</param>
    /// <param name="options">Optional translation options.</param>
    /// <param name="fileName">Logical file name sent to the provider for content-type inference.</param>
    /// <param name="httpClient">HTTP client to use.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Translated text.</returns>
    internal static async Task<string> TranslateAsync(byte[] audio, string? apiKey, TranslationOptions? options = null, string fileName = "audio.mp3", HttpClient? httpClient = null, CancellationToken cancellationToken = default)
    {
        options ??= new();
        httpClient ??= new()
        {
            Timeout = TimeSpan.FromMinutes(15)
        };

        if (!string.IsNullOrWhiteSpace(options.ApiKey))
        {
            apiKey = options.ApiKey;
        }

        using var response = await httpClient.RepeatPostAsync("https://api.openai.com/v1/audio/translations", () => CreateTranslationRequest(audio, options, fileName), apiKey, options.MaxAttempts, cancellationToken);

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    /// <summary>
    /// Builds multipart form data content for a transcription request.
    /// </summary>
    /// <param name="audio">Audio payload.</param>
    /// <param name="options">Transcription options.</param>
    /// <param name="fileName">Logical file name sent to the provider.</param>
    /// <returns>Multipart content ready to send.</returns>
    private static MultipartFormDataContent CreateTranscriptionRequest(byte[] audio, TranscriptionOptions options, string fileName)
    {
        var fileContent = CreateAudioFileContent(audio, fileName);

        var content = new MultipartFormDataContent
        {
            { fileContent, "file", fileName },
            { new StringContent(options.Model), "model" }
        };

        if (!string.IsNullOrWhiteSpace(options.Language))
        {
            content.Add(new StringContent(options.Language), "language");
        }

        if (!string.IsNullOrWhiteSpace(options.Prompt))
        {
            content.Add(new StringContent(options.Prompt), "prompt");
        }

        if (options.Temperature != 0)
        {
            content.Add(new StringContent(options.Temperature.ToString(CultureInfo.InvariantCulture)), "temperature");
        }

        if (options.ResponseFormat != TranscriptionResponseFormat.Json)
        {
            // Only send response_format when deviating from the default; provider defaults to json.
            content.Add(new StringContent(GetResponseFormatName(options.ResponseFormat)), "response_format");
        }

        return content;
    }

    /// <summary>
    /// Builds multipart form data content for a translation request.
    /// </summary>
    /// <param name="audio">Audio payload.</param>
    /// <param name="options">Translation options.</param>
    /// <param name="fileName">Logical file name sent to the provider.</param>
    /// <returns>Multipart content ready to send.</returns>
    private static MultipartFormDataContent CreateTranslationRequest(byte[] audio, TranslationOptions options, string fileName)
    {
        var fileContent = CreateAudioFileContent(audio, fileName);

        var content = new MultipartFormDataContent
        {
            { fileContent, "file", fileName },
            { new StringContent(options.Model), "model" }
        };

        if (!string.IsNullOrWhiteSpace(options.Prompt))
        {
            content.Add(new StringContent(options.Prompt), "prompt");
        }

        if (options.Temperature != 0)
        {
            content.Add(new StringContent(options.Temperature.ToString(CultureInfo.InvariantCulture)), "temperature");
        }

        if (options.ResponseFormat != TranscriptionResponseFormat.Json)
        {
            content.Add(new StringContent(GetResponseFormatName(options.ResponseFormat)), "response_format");
        }

        return content;
    }

    /// <summary>
    /// Resolves the provider format name for a transcription response format.
    /// </summary>
    /// <param name="format">Format to convert.</param>
    /// <returns>Provider format identifier.</returns>
    private static string GetResponseFormatName(TranscriptionResponseFormat format)
    {
        if (format == TranscriptionResponseFormat.VerboseJson)
        {
            return "verbose_json";
        }

        return format.ToString().ToLowerInvariant();
    }

    /// <summary>
    /// Creates the file part for audio uploads with a media type that matches the supplied file extension.
    /// </summary>
    /// <param name="audio">Audio bytes to upload.</param>
    /// <param name="fileName">Logical file name for the upload.</param>
    /// <returns>Configured byte-array content for the multipart request.</returns>
    private static ByteArrayContent CreateAudioFileContent(byte[] audio, string fileName)
    {
        var normalizedFileName = string.IsNullOrWhiteSpace(fileName) ? "audio.mp3" : Path.GetFileName(fileName);
        var fileContent = new ByteArrayContent(audio);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(GetAudioContentType(normalizedFileName));

        return fileContent;
    }

    /// <summary>
    /// Maps supported audio file extensions to the content types expected by the provider.
    /// </summary>
    /// <param name="fileName">Logical file name for the upload.</param>
    /// <returns>HTTP content type for the audio payload.</returns>
    private static string GetAudioContentType(string fileName)
    {
        return Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".flac" => "audio/flac",
            ".m4a" => "audio/mp4",
            ".mp3" => "audio/mpeg",
            ".mp4" => "audio/mp4",
            ".mpeg" => "audio/mpeg",
            ".mpga" => "audio/mpeg",
            ".oga" => "audio/ogg",
            ".ogg" => "audio/ogg",
            ".wav" => "audio/wav",
            ".webm" => "audio/webm",
            _ => "application/octet-stream"
        };
    }
}
