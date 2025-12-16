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
    /// <param name="httpClient">HTTP client to use.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Text transcript of the audio.</returns>
    internal static async Task<string> TranscriptAsync(byte[] audio, string? apiKey, TranscriptionOptions? options = null, HttpClient? httpClient = null, CancellationToken cancellationToken = default)
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

        using var requestContent = CreateTranscriptionRequest(audio, options);
        using var response = await httpClient.RepeatPostAsync("https://api.openai.com/v1/audio/transcriptions", requestContent, apiKey, options.MaxAttempts, cancellationToken);

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    /// <summary>
    /// Translates audio bytes to English text.
    /// </summary>
    /// <param name="audio">Audio payload.</param>
    /// <param name="apiKey">API key used for the request.</param>
    /// <param name="options">Optional translation options.</param>
    /// <param name="httpClient">HTTP client to use.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Translated text.</returns>
    internal static async Task<string> TranslateAsync(byte[] audio, string? apiKey, TranslationOptions? options = null, HttpClient? httpClient = null, CancellationToken cancellationToken = default)
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

        using var requestContent = CreateTranslationRequest(audio, options);
        using var response = await httpClient.RepeatPostAsync("https://api.openai.com/v1/audio/translations", requestContent, apiKey, options.MaxAttempts, cancellationToken);

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    /// <summary>
    /// Builds multipart form data content for a transcription request.
    /// </summary>
    /// <param name="audio">Audio payload.</param>
    /// <param name="options">Transcription options.</param>
    /// <returns>Multipart content ready to send.</returns>
    private static MultipartFormDataContent CreateTranscriptionRequest(byte[] audio, TranscriptionOptions options)
    {
        var fileContent = new ByteArrayContent(audio);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data");

        var content = new MultipartFormDataContent
        {
            { fileContent, "file", Path.GetFileName("audio.mp3") },
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
            content.Add(new StringContent(options.Temperature.ToString()), "temperature");
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
    /// <returns>Multipart content ready to send.</returns>
    private static MultipartFormDataContent CreateTranslationRequest(byte[] audio, TranslationOptions options)
    {
        var fileContent = new ByteArrayContent(audio);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data");

        var content = new MultipartFormDataContent
        {
            { fileContent, "file", Path.GetFileName("audio.mp3") },
            { new StringContent(options.Model), "model" }
        };

        if (!string.IsNullOrWhiteSpace(options.Prompt))
        {
            content.Add(new StringContent(options.Prompt), "prompt");
        }

        if (options.Temperature != 0)
        {
            content.Add(new StringContent(options.Temperature.ToString()), "temperature");
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
}
