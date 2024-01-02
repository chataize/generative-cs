using System.Text.Json.Nodes;
using GenerativeCS.Enums;
using GenerativeCS.Options.OpenAI;
using GenerativeCS.Utilities;

namespace GenerativeCS.Providers.OpenAI;

internal static class TextToSpeech
{
    internal static async Task<byte[]> SynthesizeSpeechAsync(string text, string apiKey, TextToSpeechOptions? options = null, HttpClient? httpClient = null, CancellationToken cancellationToken = default)
    {
        options ??= new();
        httpClient ??= new();

        var requestObject = CreateSpeechSynthesisRequest(text, options);
        using var response = await httpClient.RepeatPostAsJsonAsync("https://api.openai.com/v1/audio/speech", requestObject, apiKey, options.MaxAttempts, cancellationToken);

        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }

    private static JsonObject CreateSpeechSynthesisRequest(string text, TextToSpeechOptions options)
    {
        var requestObject = new JsonObject
        {
            { "model", options.Model },
            { "voice", options.Voice.ToString().ToLowerInvariant() },
            { "input", text }
        };

        if (options.Speed != 1.0)
        {
            requestObject.Add("speed", options.Speed);
        }

        if (options.ResponseFormat != VoiceResponseFormat.Default)
        {
            requestObject.Add("response_format", options.ResponseFormat.ToString().ToLowerInvariant());
        }

        return requestObject;
    }
}
