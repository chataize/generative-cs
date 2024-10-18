using System.Net.Http.Headers;
using ChatAIze.GenerativeCS.Enums;
using ChatAIze.GenerativeCS.Options.OpenAI;
using ChatAIze.GenerativeCS.Utilities;

namespace ChatAIze.GenerativeCS.Providers.OpenAI;

internal static class SpeechRecognition
{
    internal static async Task<string> TranscriptAsync(byte[] audio, string? apiKey, TranscriptionOptions? options = null, HttpClient? httpClient = null, CancellationToken cancellationToken = default)
    {
        options ??= new();
        httpClient ??= new();

        if (!string.IsNullOrWhiteSpace(options.ApiKey))
        {
            apiKey = options.ApiKey;
        }

        using var requestContent = CreateTranscriptionRequest(audio, options);
        using var response = await httpClient.RepeatPostAsync("https://api.openai.com/v1/audio/transcriptions", requestContent, apiKey, options.MaxAttempts, cancellationToken);

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    internal static async Task<string> TranslateAsync(byte[] audio, string? apiKey, TranslationOptions? options = null, HttpClient? httpClient = null, CancellationToken cancellationToken = default)
    {
        options ??= new();
        httpClient ??= new();

        if (!string.IsNullOrWhiteSpace(options.ApiKey))
        {
            apiKey = options.ApiKey;
        }

        using var requestContent = CreateTranslationRequest(audio, options);
        using var response = await httpClient.RepeatPostAsync("https://api.openai.com/v1/audio/translations", requestContent, apiKey, options.MaxAttempts, cancellationToken);

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

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
            content.Add(new StringContent(GetResponseFormatName(options.ResponseFormat)), "response_format");
        }

        return content;
    }

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

    private static string GetResponseFormatName(TranscriptionResponseFormat format)
    {
        if (format == TranscriptionResponseFormat.VerboseJson)
        {
            return "verbose_json";
        }

        return format.ToString().ToLowerInvariant();
    }
}
