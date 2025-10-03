using System.Text.Json.Nodes;
using ChatAIze.GenerativeCS.Enums;
using ChatAIze.GenerativeCS.Options.OpenAI;
using ChatAIze.GenerativeCS.Utilities;

namespace ChatAIze.GenerativeCS.Providers.OpenAI;

internal static class TextToSpeech
{
    internal static async Task<byte[]> SynthesizeSpeechAsync(string text, string? apiKey, TextToSpeechOptions? options = null, HttpClient? httpClient = null, CancellationToken cancellationToken = default)
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

        var requestObject = CreateSpeechSynthesisRequest(text, options);
        using var response = await httpClient.RepeatPostAsJsonAsync("https://api.openai.com/v1/audio/speech", requestObject, apiKey, options.MaxAttempts, cancellationToken);

        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }

    private static JsonObject CreateSpeechSynthesisRequest(string text, TextToSpeechOptions options)
    {
        var requestObject = new JsonObject
        {
            ["model"] = options.Model,
            ["voice"] = options.Voice.ToString().ToLowerInvariant(),
            ["input"] = text
        };

        if (options.Speed != 1.0)
        {
            requestObject["speed"] = options.Speed;
        }

        if (options.ResponseFormat != VoiceResponseFormat.Default)
        {
            requestObject["response_format"] = options.ResponseFormat.ToString().ToLowerInvariant();
        }

        return requestObject;
    }
}
