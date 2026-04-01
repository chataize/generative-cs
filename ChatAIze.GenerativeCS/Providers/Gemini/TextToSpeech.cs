using System.Buffers.Binary;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using ChatAIze.GenerativeCS.Constants;
using ChatAIze.GenerativeCS.Enums;
using ChatAIze.GenerativeCS.Options.Gemini;

namespace ChatAIze.GenerativeCS.Providers.Gemini;

/// <summary>
/// Handles Gemini text-to-speech requests.
/// </summary>
internal static class TextToSpeech
{
    internal static async Task<byte[]> SynthesizeSpeechAsync(
        string text,
        string? apiKey,
        TextToSpeechOptions? options = null,
        HttpClient? httpClient = null,
        CancellationToken cancellationToken = default)
    {
        httpClient ??= new()
        {
            Timeout = TimeSpan.FromMinutes(15)
        };

        options ??= new();
        if (!string.IsNullOrWhiteSpace(options.ApiKey))
        {
            apiKey = options.ApiKey;
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
                            ["text"] = text
                        }
                    }
                }
            },
            ["generation_config"] = new JsonObject
            {
                ["response_modalities"] = new JsonArray("AUDIO"),
                ["speech_config"] = new JsonObject
                {
                    ["voice_config"] = new JsonObject
                    {
                        ["prebuilt_voice_config"] = new JsonObject
                        {
                            ["voice_name"] = ResolveVoiceName(options)
                        }
                    }
                }
            }
        };

        using var response = await GeminiHttp.SendGenerateContentRequestAsync(httpClient, options.Model, request, apiKey, options.MaxAttempts, isStreaming: false, cancellationToken);
        using var responseContent = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var responseDocument = await JsonDocument.ParseAsync(responseContent, cancellationToken: cancellationToken);

        var inlineData = responseDocument.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("inlineData");

        var mimeType = inlineData.GetProperty("mimeType").GetString() ?? "audio/L16;codec=pcm;rate=24000";
        var pcmBytes = Convert.FromBase64String(inlineData.GetProperty("data").GetString() ?? string.Empty);
        var sampleRate = ParseSampleRate(mimeType, options.SampleRate);

        return options.ResponseFormat switch
        {
            VoiceResponseFormat.Default => WrapPcm16MonoAsWav(pcmBytes, sampleRate),
            VoiceResponseFormat.MP3 => throw new NotSupportedException("Gemini text-to-speech currently returns PCM audio only. Use WAV/default output instead of MP3."),
            VoiceResponseFormat.Opus => throw new NotSupportedException("Gemini text-to-speech currently does not support Opus output."),
            VoiceResponseFormat.AAC => throw new NotSupportedException("Gemini text-to-speech currently does not support AAC output."),
            VoiceResponseFormat.FLAC => throw new NotSupportedException("Gemini text-to-speech currently does not support FLAC output."),
            _ => WrapPcm16MonoAsWav(pcmBytes, sampleRate)
        };
    }

    private static string ResolveVoiceName(TextToSpeechOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.VoiceName))
        {
            return options.VoiceName;
        }

        return options.Voice switch
        {
            TextToSpeechVoice.Alloy => GeminiTextToSpeechVoices.Kore,
            TextToSpeechVoice.Echo => GeminiTextToSpeechVoices.Puck,
            TextToSpeechVoice.Fable => GeminiTextToSpeechVoices.Leda,
            TextToSpeechVoice.Onyx => GeminiTextToSpeechVoices.Charon,
            TextToSpeechVoice.Nova => GeminiTextToSpeechVoices.Zephyr,
            TextToSpeechVoice.Shimmer => GeminiTextToSpeechVoices.Aoede,
            _ => GeminiTextToSpeechVoices.Kore
        };
    }

    private static int ParseSampleRate(string mimeType, int fallbackSampleRate)
    {
        const string marker = "rate=";
        var markerIndex = mimeType.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (markerIndex < 0)
        {
            return fallbackSampleRate;
        }

        var valueStart = markerIndex + marker.Length;
        var valueEnd = mimeType.IndexOf(';', valueStart);
        var value = valueEnd >= 0 ? mimeType[valueStart..valueEnd] : mimeType[valueStart..];
        return int.TryParse(value, out var sampleRate) ? sampleRate : fallbackSampleRate;
    }

    private static byte[] WrapPcm16MonoAsWav(byte[] pcmBytes, int sampleRate)
    {
        const short channels = 1;
        const short bitsPerSample = 16;
        var blockAlign = (short)(channels * (bitsPerSample / 8));
        var byteRate = sampleRate * blockAlign;
        var buffer = new byte[44 + pcmBytes.Length];

        Encoding.ASCII.GetBytes("RIFF").CopyTo(buffer, 0);
        BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(4, 4), 36 + pcmBytes.Length);
        Encoding.ASCII.GetBytes("WAVE").CopyTo(buffer, 8);
        Encoding.ASCII.GetBytes("fmt ").CopyTo(buffer, 12);
        BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(16, 4), 16);
        BinaryPrimitives.WriteInt16LittleEndian(buffer.AsSpan(20, 2), 1);
        BinaryPrimitives.WriteInt16LittleEndian(buffer.AsSpan(22, 2), channels);
        BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(24, 4), sampleRate);
        BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(28, 4), byteRate);
        BinaryPrimitives.WriteInt16LittleEndian(buffer.AsSpan(32, 2), blockAlign);
        BinaryPrimitives.WriteInt16LittleEndian(buffer.AsSpan(34, 2), bitsPerSample);
        Encoding.ASCII.GetBytes("data").CopyTo(buffer, 36);
        BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(40, 4), pcmBytes.Length);
        pcmBytes.CopyTo(buffer, 44);

        return buffer;
    }
}
