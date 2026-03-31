using System.Text.Json.Nodes;
using ChatAIze.GenerativeCS.Constants;
using ChatAIze.GenerativeCS.Enums;
using ChatAIze.GenerativeCS.Options.Grok;
using ChatAIze.GenerativeCS.Utilities;

namespace ChatAIze.GenerativeCS.Providers.Grok;

/// <summary>
/// Handles xAI text-to-speech synthesis requests.
/// </summary>
internal static class TextToSpeech
{
    /// <summary>
    /// Synthesizes speech audio bytes from text.
    /// </summary>
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
        using var response = await httpClient.RepeatPostAsJsonAsync("https://api.x.ai/v1/tts", requestObject, apiKey, options.MaxAttempts, cancellationToken);

        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }

    /// <summary>
    /// Builds the JSON payload for an xAI text-to-speech request.
    /// </summary>
    private static JsonObject CreateSpeechSynthesisRequest(string text, TextToSpeechOptions options)
    {
        var requestObject = new JsonObject
        {
            ["text"] = text,
            ["voice_id"] = ResolveVoiceId(options),
            ["language"] = options.Language
        };

        var outputFormat = new JsonObject();
        var codec = ResolveCodec(options);
        if (codec is not null)
        {
            outputFormat["codec"] = codec;
        }

        if (options.SampleRate.HasValue)
        {
            outputFormat["sample_rate"] = options.SampleRate.Value;
        }

        if (options.BitRate.HasValue)
        {
            outputFormat["bit_rate"] = options.BitRate.Value;
        }

        if (outputFormat.Count > 0)
        {
            requestObject["output_format"] = outputFormat;
        }

        return requestObject;
    }

    /// <summary>
    /// Resolves the xAI voice identifier to send for the request.
    /// </summary>
    private static string ResolveVoiceId(TextToSpeechOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.VoiceId))
        {
            return options.VoiceId;
        }

        return options.Voice switch
        {
            TextToSpeechVoice.Alloy => GrokTextToSpeechVoices.Eve,
            TextToSpeechVoice.Echo => GrokTextToSpeechVoices.Rex,
            TextToSpeechVoice.Fable => GrokTextToSpeechVoices.Ara,
            TextToSpeechVoice.Onyx => GrokTextToSpeechVoices.Leo,
            TextToSpeechVoice.Nova => GrokTextToSpeechVoices.Sal,
            TextToSpeechVoice.Shimmer => GrokTextToSpeechVoices.Una,
            _ => GrokTextToSpeechVoices.Eve
        };
    }

    /// <summary>
    /// Resolves the codec to send for the request or returns null to use xAI's default MP3 output.
    /// </summary>
    private static string? ResolveCodec(TextToSpeechOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.Codec))
        {
            return options.Codec.ToLowerInvariant();
        }

        return options.ResponseFormat switch
        {
            VoiceResponseFormat.Default => null,
            VoiceResponseFormat.MP3 => "mp3",
            VoiceResponseFormat.Opus => throw new NotSupportedException("xAI text-to-speech does not support Opus output. Use Codec = \"wav\", \"pcm\", \"mulaw\", or \"alaw\" when you need a Grok-native format."),
            VoiceResponseFormat.AAC => throw new NotSupportedException("xAI text-to-speech does not support AAC output. Use Codec = \"wav\", \"pcm\", \"mulaw\", or \"alaw\" when you need a Grok-native format."),
            VoiceResponseFormat.FLAC => throw new NotSupportedException("xAI text-to-speech does not support FLAC output. Use Codec = \"wav\", \"pcm\", \"mulaw\", or \"alaw\" when you need a Grok-native format."),
            _ => null
        };
    }
}
