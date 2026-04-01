using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using ChatAIze.GenerativeCS.Enums;
using GeminiTranscriptionOptions = ChatAIze.GenerativeCS.Options.Gemini.TranscriptionOptions;
using GeminiTranslationOptions = ChatAIze.GenerativeCS.Options.Gemini.TranslationOptions;

namespace ChatAIze.GenerativeCS.Providers.Gemini;

/// <summary>
/// Implements transcription and audio translation on top of Gemini audio understanding.
/// </summary>
internal static class SpeechRecognition
{
    internal static async Task<string> TranscriptAsync(
        byte[] audio,
        string? apiKey,
        GeminiTranscriptionOptions? options = null,
        string fileName = "",
        HttpClient? httpClient = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new();
        return await AnalyzeAudioAsync(
            audio,
            apiKey,
            options.Model,
            options.ApiKey,
            fileName,
            BuildTranscriptPrompt(options),
            options.ResponseFormat,
            options.MaxAttempts,
            options.Temperature,
            httpClient,
            cancellationToken);
    }

    internal static async Task<string> TranslateAsync(
        byte[] audio,
        string? apiKey,
        GeminiTranslationOptions? options = null,
        string fileName = "",
        HttpClient? httpClient = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new();

        return await AnalyzeAudioAsync(
            audio,
            apiKey,
            options.Model,
            options.ApiKey,
            fileName,
            BuildTranslationPrompt(options),
            options.ResponseFormat,
            options.MaxAttempts,
            options.Temperature,
            httpClient,
            cancellationToken);
    }

    private static async Task<string> AnalyzeAudioAsync(
        byte[] audio,
        string? apiKey,
        string model,
        string? apiKeyOverride,
        string fileName,
        string prompt,
        TranscriptionResponseFormat responseFormat,
        int maxAttempts,
        double temperature,
        HttpClient? httpClient,
        CancellationToken cancellationToken)
    {
        httpClient ??= new()
        {
            Timeout = TimeSpan.FromMinutes(15)
        };

        if (!string.IsNullOrWhiteSpace(apiKeyOverride))
        {
            apiKey = apiKeyOverride;
        }

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("Gemini API key was not provided.");
        }

        var request = new JsonObject
        {
            ["contents"] = new JsonArray
            {
                new JsonObject
                {
                    ["parts"] = new JsonArray
                    {
                        new JsonObject
                        {
                            ["text"] = prompt
                        },
                        new JsonObject
                        {
                            ["inline_data"] = new JsonObject
                            {
                                ["mime_type"] = GetAudioContentType(fileName, audio),
                                ["data"] = Convert.ToBase64String(audio)
                            }
                        }
                    }
                }
            }
        };

        var generationConfig = new JsonObject();
        if (temperature != 0)
        {
            generationConfig["temperature"] = temperature;
        }

        if (responseFormat is TranscriptionResponseFormat.Json or TranscriptionResponseFormat.VerboseJson)
        {
            generationConfig["response_mime_type"] = "application/json";
        }

        if (generationConfig.Count > 0)
        {
            request["generation_config"] = generationConfig;
        }

        using var response = await GeminiHttp.SendGenerateContentRequestAsync(httpClient, model, request, apiKey, maxAttempts, isStreaming: false, cancellationToken);
        using var responseContent = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var responseDocument = await JsonDocument.ParseAsync(responseContent, cancellationToken: cancellationToken);

        var candidate = responseDocument.RootElement.GetProperty("candidates")[0];
        if (!candidate.TryGetProperty("content", out var contentElement)
            || !contentElement.TryGetProperty("parts", out var partsElement)
            || partsElement.GetArrayLength() == 0)
        {
            throw new InvalidOperationException("Gemini returned no transcription content.");
        }

        var firstPart = partsElement[0];
        if (firstPart.TryGetProperty("text", out var textElement))
        {
            return textElement.GetString() ?? string.Empty;
        }

        return firstPart.GetRawText();
    }

    private static string BuildTranscriptPrompt(GeminiTranscriptionOptions options)
    {
        var builder = new StringBuilder("Generate a transcript of the speech.");

        if (!string.IsNullOrWhiteSpace(options.Language))
        {
            builder.Append(' ')
                .Append("The spoken language is likely ")
                .Append(options.Language)
                .Append('.');
        }

        if (!string.IsNullOrWhiteSpace(options.Prompt))
        {
            builder.Append(' ')
                .Append(options.Prompt.Trim());
        }

        AppendFormatInstruction(builder, options.ResponseFormat, isTranslation: false);
        return builder.ToString();
    }

    private static string BuildTranslationPrompt(GeminiTranslationOptions options)
    {
        var builder = new StringBuilder("Listen to the audio and translate the spoken content into ")
            .Append(options.TargetLanguage)
            .Append(". Do not repeat the original-language transcript. If the speech is already in ")
            .Append(options.TargetLanguage)
            .Append(", return it unchanged. Preserve the meaning, numbers, and named entities.");

        if (!string.IsNullOrWhiteSpace(options.Prompt))
        {
            builder.Append(' ')
                .Append(options.Prompt.Trim());
        }

        AppendFormatInstruction(builder, options.ResponseFormat, isTranslation: true);
        return builder.ToString();
    }

    private static void AppendFormatInstruction(StringBuilder builder, TranscriptionResponseFormat responseFormat, bool isTranslation)
    {
        switch (responseFormat)
        {
            case TranscriptionResponseFormat.Text:
                builder.Append(isTranslation
                    ? " Return only the translated text."
                    : " Return only the transcript text.");
                break;
            case TranscriptionResponseFormat.Json:
                builder.Append(isTranslation
                    ? " Return a JSON object with a single property named \"text\" containing the translated text."
                    : " Return a JSON object with a single property named \"text\" containing the transcript.");
                break;
            case TranscriptionResponseFormat.VerboseJson:
                builder.Append(isTranslation
                    ? " Return a JSON object with properties \"text\" and \"notes\". Put the translated text in \"text\"."
                    : " Return a JSON object with properties \"text\" and \"segments\". Each segment should include \"start_time\", \"end_time\", and \"text\".");
                break;
            case TranscriptionResponseFormat.SRT:
                builder.Append(" Return only valid SRT subtitle content.");
                break;
            case TranscriptionResponseFormat.VTT:
                builder.Append(" Return only valid WebVTT subtitle content.");
                break;
            default:
                builder.Append(" Return only the text output.");
                break;
        }
    }

    private static string GetAudioContentType(string fileName, byte[] audio)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".mp3" => "audio/mpeg",
            ".wav" => "audio/wav",
            ".m4a" => "audio/mp4",
            ".mp4" => "audio/mp4",
            ".webm" => "audio/webm",
            ".ogg" => "audio/ogg",
            ".flac" => "audio/flac",
            _ => DetectAudioContentType(audio)
        };
    }

    /// <summary>
    /// Best-effort audio content-type detection for raw byte-array overloads that do not carry a file name.
    /// </summary>
    private static string DetectAudioContentType(byte[] audio)
    {
        if (audio.Length >= 12
            && audio[0] == (byte)'R'
            && audio[1] == (byte)'I'
            && audio[2] == (byte)'F'
            && audio[3] == (byte)'F'
            && audio[8] == (byte)'W'
            && audio[9] == (byte)'A'
            && audio[10] == (byte)'V'
            && audio[11] == (byte)'E')
        {
            return "audio/wav";
        }

        if (audio.Length >= 4
            && audio[0] == (byte)'f'
            && audio[1] == (byte)'L'
            && audio[2] == (byte)'a'
            && audio[3] == (byte)'C')
        {
            return "audio/flac";
        }

        if (audio.Length >= 4
            && audio[0] == (byte)'O'
            && audio[1] == (byte)'g'
            && audio[2] == (byte)'g'
            && audio[3] == (byte)'S')
        {
            return "audio/ogg";
        }

        if (audio.Length >= 3
            && audio[0] == (byte)'I'
            && audio[1] == (byte)'D'
            && audio[2] == (byte)'3')
        {
            return "audio/mpeg";
        }

        if (audio.Length >= 2
            && audio[0] == 0xFF
            && (audio[1] & 0xE0) == 0xE0)
        {
            return "audio/mpeg";
        }

        if (audio.Length >= 12
            && audio[4] == (byte)'f'
            && audio[5] == (byte)'t'
            && audio[6] == (byte)'y'
            && audio[7] == (byte)'p')
        {
            return "audio/mp4";
        }

        if (audio.Length >= 4
            && audio[0] == 0x1A
            && audio[1] == 0x45
            && audio[2] == 0xDF
            && audio[3] == 0xA3)
        {
            return "audio/webm";
        }

        return "application/octet-stream";
    }
}
