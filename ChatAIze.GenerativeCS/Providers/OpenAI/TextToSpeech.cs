using System.Text.Json.Nodes;
using ChatAIze.GenerativeCS.Enums;
using ChatAIze.GenerativeCS.Options.OpenAI;
using ChatAIze.GenerativeCS.Utilities;

namespace ChatAIze.GenerativeCS.Providers.OpenAI;

/// <summary>
/// Handles OpenAI text-to-speech synthesis requests.
/// </summary>
internal static class TextToSpeech
{
    /// <summary>
    /// Synthesizes speech audio bytes from text.
    /// </summary>
    /// <param name="text">Text to synthesize.</param>
    /// <param name="apiKey">API key used for the request.</param>
    /// <param name="options">Optional text-to-speech options.</param>
    /// <param name="httpClient">HTTP client to use.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Raw audio bytes.</returns>
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

    /// <summary>
    /// Builds the JSON payload for a speech synthesis request.
    /// </summary>
    /// <param name="text">Text to synthesize.</param>
    /// <param name="options">Text-to-speech options.</param>
    /// <returns>JSON request payload.</returns>
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
            // Skip emitting response_format when using the provider default to keep payload minimal.
            requestObject["response_format"] = options.ResponseFormat.ToString().ToLowerInvariant();
        }

        return requestObject;
    }
}
