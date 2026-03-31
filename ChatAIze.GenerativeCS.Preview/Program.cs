using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ChatAIze.GenerativeCS.Clients;
using ChatAIze.GenerativeCS.Constants;
using ChatAIze.GenerativeCS.Enums;
using ChatAIze.GenerativeCS.Extensions;
using ChatAIze.GenerativeCS.Models;
using ChatAIze.GenerativeCS.Options.Claude;
using ChatAIze.GenerativeCS.Utilities;
using Microsoft.Extensions.DependencyInjection;

var argsList = args.Select(arg => arg.Trim()).ToArray();
var shouldRunClaudeSmoke = argsList.Length == 0 || argsList.Any(arg => arg.Equals("claude-smoke", StringComparison.OrdinalIgnoreCase));
var shouldRunGrokSmoke = argsList.Any(arg => arg.Equals("grok-smoke", StringComparison.OrdinalIgnoreCase));
var shouldRunOpenAISmoke = argsList.Any(arg => arg.Equals("openai-smoke", StringComparison.OrdinalIgnoreCase));
var shouldRunInteractiveOpenAI = argsList.Any(arg => arg.Equals("openai-chat", StringComparison.OrdinalIgnoreCase));

if (shouldRunClaudeSmoke)
{
    await RunClaudeSmokeTestsAsync();
    return;
}

if (shouldRunGrokSmoke)
{
    await RunGrokSmokeTestsAsync();
    return;
}

if (shouldRunInteractiveOpenAI)
{
    await RunInteractiveOpenAIChatAsync();
    return;
}

if (shouldRunOpenAISmoke)
{
    await RunOpenAISmokeTestsAsync();
    return;
}

Console.WriteLine("Usage:");
Console.WriteLine("  dotnet run --project ChatAIze.GenerativeCS.Preview");
Console.WriteLine("  dotnet run --project ChatAIze.GenerativeCS.Preview -- claude-smoke");
Console.WriteLine("  dotnet run --project ChatAIze.GenerativeCS.Preview -- grok-smoke");
Console.WriteLine("  dotnet run --project ChatAIze.GenerativeCS.Preview -- openai-smoke");
Console.WriteLine("  dotnet run --project ChatAIze.GenerativeCS.Preview -- openai-chat");

static async Task RunClaudeSmokeTestsAsync()
{
    Console.WriteLine("Claude smoke tests starting...");

    var client = new ClaudeClient();
    if (string.IsNullOrWhiteSpace(client.ApiKey))
    {
        throw new InvalidOperationException("Claude API key was not found. Export CLAUDE_API_KEY or ANTHROPIC_API_KEY before running the preview.");
    }

    var usageTracker = new TokenUsageTracker();

    await RunTestAsync("simple completion", async () =>
    {
        var options = CreateClaudeOptions();
        var response = await client.CompleteAsync("Reply with exactly SIMPLE_OK.", options, usageTracker);
        ExpectContains(response, "SIMPLE_OK", "Simple completion did not echo the expected marker.");
    });

    await RunTestAsync("chat continuation", async () =>
    {
        var options = CreateClaudeOptions();
        var chat = new Chat { UserTrackingId = "preview-chat-user" };

        chat.FromUser("Remember this fact exactly: my favorite mineral is quartz.");
        _ = await client.CompleteAsync(chat, options, usageTracker);

        chat.FromUser("What is my favorite mineral? Reply with one short sentence.");
        var response = await client.CompleteAsync(chat, options, usageTracker);
        ExpectContains(response, "quartz", "Claude did not preserve the chat context across turns.");
    });

    await RunTestAsync("function calling", async () =>
    {
        var options = CreateClaudeOptions();
        var functionCalled = false;

        options.AddFunction("GetCityWeather", "Returns the weather for a city.", (string city) =>
        {
            functionCalled = true;
            if (!city.Contains("Warsaw", StringComparison.OrdinalIgnoreCase))
            {
                return $"Error: Unsupported city '{city}'.";
            }

            return "The temperature in Warsaw is 21 C.";
        });

        var response = await client.CompleteAsync("Use the GetCityWeather function for Warsaw and tell me the result.", options, usageTracker);

        if (!functionCalled)
        {
            throw new InvalidOperationException("Claude did not invoke the expected function.");
        }

        ExpectContains(response, "21", "Claude did not incorporate the function result into the final answer.");
    });

    await RunTestAsync("streaming completion", async () =>
    {
        var options = CreateClaudeOptions();
        var streamedBuilder = new StringBuilder();

        await foreach (var chunk in client.StreamCompletionAsync("Reply with exactly STREAM_OK.", options, usageTracker))
        {
            _ = streamedBuilder.Append(chunk);
        }

        var response = streamedBuilder.ToString();
        ExpectContains(response, "STREAM_OK", "Streaming completion did not produce the expected marker.");
    });

    await RunTestAsync("streaming function calling", async () =>
    {
        var options = CreateClaudeOptions();
        var functionCalled = false;
        var streamedBuilder = new StringBuilder();

        options.AddFunction("LookupRiverLength", "Returns the length of a river in kilometers.", (string river) =>
        {
            functionCalled = true;
            if (!river.Contains("Vistula", StringComparison.OrdinalIgnoreCase))
            {
                return $"Error: Unsupported river '{river}'.";
            }

            return "The Vistula river is 1047 kilometers long.";
        });

        await foreach (var chunk in client.StreamCompletionAsync("Use LookupRiverLength for the Vistula river, then answer with one short sentence ending in CLAUDE_STREAM_TOOL_OK.", options, usageTracker))
        {
            _ = streamedBuilder.Append(chunk);
        }

        if (!functionCalled)
        {
            throw new InvalidOperationException("Claude did not invoke the expected function during streaming.");
        }

        var response = streamedBuilder.ToString();
        ExpectContainsNormalizedNumber(response, "1047", "Claude streaming function calling did not incorporate the tool result.");
        ExpectContains(response, "CLAUDE_STREAM_TOOL_OK", "Claude streaming function calling did not finish with the expected marker.");
    });

    await RunTestAsync("structured outputs", async () =>
    {
        var options = CreateClaudeOptions();
        options.ResponseType = typeof(StructuredCityResponse);

        var response = await client.CompleteAsync("Return structured data for the city Warsaw in Poland on the continent Europe.", options, usageTracker);
        var result = JsonSerializer.Deserialize<StructuredCityResponse>(response, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (result is null)
        {
            throw new InvalidOperationException("Claude did not return valid structured JSON.");
        }

        ExpectContains(result.City, "Warsaw", "Structured output returned the wrong city.");
        ExpectContains(result.Country, "Poland", "Structured output returned the wrong country.");
        ExpectContains(result.Continent, "Europe", "Structured output returned the wrong continent.");
    });

    await RunTestAsync("nested structured outputs", async () =>
    {
        var options = CreateClaudeOptions();
        options.ResponseType = typeof(StructuredTravelGuideResponse);

        var response = await client.CompleteAsync(
            "Return a structured travel guide for Warsaw with exactly two highlights named Old Town and Vistula Boulevards. Include a metadata object with season spring and family_friendly true.",
            options,
            usageTracker);

        var result = JsonSerializer.Deserialize<StructuredTravelGuideResponse>(response, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (result is null)
        {
            throw new InvalidOperationException("Claude did not return valid nested structured JSON.");
        }

        ExpectContains(result.City, "Warsaw", "Claude nested structured output returned the wrong city.");
        if (result.Highlights.Count != 2)
        {
            throw new InvalidOperationException($"Claude nested structured output returned {result.Highlights.Count} highlights instead of 2.");
        }

        ExpectContains(result.Highlights[0].Name + " " + result.Highlights[1].Name, "Old Town", "Claude nested structured output omitted Old Town.");
        ExpectContains(result.Highlights[0].Name + " " + result.Highlights[1].Name, "Vistula", "Claude nested structured output omitted Vistula Boulevards.");
        ExpectContains(result.Metadata.Season, "spring", "Claude nested structured output returned the wrong season.");

        if (!result.Metadata.FamilyFriendly)
        {
            throw new InvalidOperationException("Claude nested structured output returned the wrong family_friendly flag.");
        }
    });

    await RunTestAsync("function calling with structured output", async () =>
    {
        var options = CreateClaudeOptions();
        options.ResponseType = typeof(StructuredWeatherSummaryResponse);
        options.IsStrictFunctionCallingOn = false;
        options.IsParallelFunctionCallingOn = true;

        var requestedCities = new List<string>();
        options.AddFunction("GetCityTemperature", "Returns the temperature for a city in Celsius.", (string city) =>
        {
            requestedCities.Add(city);
            return city.Contains("Warsaw", StringComparison.OrdinalIgnoreCase)
                ? "{\"city\":\"Warsaw\",\"temperature_c\":21}"
                : city.Contains("Krakow", StringComparison.OrdinalIgnoreCase)
                    ? "{\"city\":\"Krakow\",\"temperature_c\":19}"
                    : $"Error: Unsupported city '{city}'.";
        });

        var response = await client.CompleteAsync(
            "Call GetCityTemperature for Warsaw and Krakow, then return structured JSON listing both readings, the warmest city, and the temperature difference.",
            options,
            usageTracker);

        var result = JsonSerializer.Deserialize<StructuredWeatherSummaryResponse>(response, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (result is null)
        {
            throw new InvalidOperationException("Claude did not return valid structured JSON after tool execution.");
        }

        if (requestedCities.Count < 2)
        {
            throw new InvalidOperationException($"Claude only invoked GetCityTemperature {requestedCities.Count} times.");
        }

        ExpectContains(string.Join(' ', requestedCities), "Warsaw", "Claude never requested Warsaw weather.");
        ExpectContains(string.Join(' ', requestedCities), "Krakow", "Claude never requested Krakow weather.");
        if (result.Readings.Count != 2)
        {
            throw new InvalidOperationException($"Claude structured weather summary returned {result.Readings.Count} readings instead of 2.");
        }

        ExpectContains(result.WarmestCity, "Warsaw", "Claude structured weather summary returned the wrong warmest city.");
        if (result.DifferenceC != 2)
        {
            throw new InvalidOperationException($"Claude structured weather summary returned the wrong temperature difference: {result.DifferenceC}.");
        }
    });

    await RunTestAsync("json mode", async () =>
    {
        var options = CreateClaudeOptions();
        options.IsJsonMode = true;

        var response = await client.CompleteAsync("Return a JSON object with {\"status\":\"JSON_OK\"}.", options, usageTracker);
        using var document = JsonDocument.Parse(response);
        var status = document.RootElement.GetProperty("status").GetString();

        if (!string.Equals(status, "JSON_OK", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("JSON mode did not return the expected status field.");
        }
    });

    await RunTestAsync("moderation", async () =>
    {
        var safeResult = await client.ModerateAsync("I enjoyed walking my dog in the park today.");
        if (safeResult.IsFlagged)
        {
            throw new InvalidOperationException("Claude moderation incorrectly flagged clearly safe content.");
        }

        var unsafeResult = await client.ModerateAsync("I am going to kill you tonight and burn your house down.");
        if (!unsafeResult.IsFlagged)
        {
            throw new InvalidOperationException("Claude moderation failed to flag explicit violent threats.");
        }

        if (!unsafeResult.IsViolence && !unsafeResult.IsHarassmentThreatening)
        {
            throw new InvalidOperationException("Claude moderation flagged the threat, but not in a violence- or threat-related category.");
        }
    });

    await RunTestAsync("vision input", async () =>
    {
        var options = CreateClaudeOptions();
        var chat = new Chat();
        chat.FromUser("What single color dominates this image? Reply with one word.", imageUrls: ["https://dummyimage.com/256x256/ff0000/ffffff.png"]);

        var response = await client.CompleteAsync(chat, options, usageTracker);
        ExpectContains(response, "red", "Vision completion did not identify the dominant color.");
    });

    await RunTestAsync("dependency injection", async () =>
    {
        var services = new ServiceCollection();
        services.AddClaudeClient(configure =>
        {
            configure.ApiKey = client.ApiKey;
            configure.DefaultCompletionOptions = CreateClaudeOptions();
        });

        using var serviceProvider = services.BuildServiceProvider();
        var resolvedClient = serviceProvider.GetRequiredService<ClaudeClient>();
        var response = await resolvedClient.CompleteAsync("Reply with exactly DI_OK.");

        ExpectContains(response, "DI_OK", "The DI-registered Claude client did not complete successfully.");
    });

    Console.WriteLine();
    Console.WriteLine($"Claude smoke tests passed. Prompt tokens: {usageTracker.PromptTokens}, cached tokens: {usageTracker.CachedTokens}, completion tokens: {usageTracker.CompletionTokens}");
}

static ChatCompletionOptions CreateClaudeOptions()
{
    return new ChatCompletionOptions
    {
        Model = ChatCompletionModels.Claude.Sonnet46,
        MaxAttempts = 3,
        MaxOutputTokens = 512,
        Temperature = 0,
        TopP = 1,
        ReasoningEffort = ReasoningEffort.Low,
        Verbosity = Verbosity.Low,
        IsStrictFunctionCallingOn = true,
        UserTrackingId = "preview-claude-smoke"
    };
}

static async Task RunGrokSmokeTestsAsync()
{
    Console.WriteLine("Grok smoke tests starting...");

    var client = new GrokClient();
    if (string.IsNullOrWhiteSpace(client.ApiKey))
    {
        throw new InvalidOperationException("Grok API key was not found. Export GROK_API_KEY or XAI_API_KEY before running the preview.");
    }

    var usageTracker = new TokenUsageTracker();

    await RunTestAsync("simple completion", async () =>
    {
        var options = CreateGrokOptions();
        var response = await client.CompleteAsync("Reply with exactly GROK_OK.", options, usageTracker);
        ExpectContains(response, "GROK_OK", "Simple completion did not echo the expected marker.");
    });

    await RunTestAsync("chat continuation", async () =>
    {
        var options = CreateGrokOptions();
        var chat = new Chat { UserTrackingId = "preview-grok-chat-user" };

        chat.FromUser("Remember this fact exactly: my favorite gemstone is sapphire.");
        _ = await client.CompleteAsync(chat, options, usageTracker);

        chat.FromUser("What is my favorite gemstone? Reply with one short sentence.");
        var response = await client.CompleteAsync(chat, options, usageTracker);
        ExpectContains(response, "sapphire", "Grok did not preserve the chat context across turns.");
    });

    await RunTestAsync("function calling", async () =>
    {
        var options = CreateGrokOptions();
        var functionCalled = false;

        options.AddFunction("GetCityWeather", "Returns the weather for a city.", (string city) =>
        {
            functionCalled = true;
            if (!city.Contains("Warsaw", StringComparison.OrdinalIgnoreCase))
            {
                return $"Error: Unsupported city '{city}'.";
            }

            return "The temperature in Warsaw is 21 C.";
        });

        var response = await client.CompleteAsync("Use the GetCityWeather function for Warsaw and tell me the result.", options, usageTracker);

        if (!functionCalled)
        {
            throw new InvalidOperationException("Grok did not invoke the expected function.");
        }

        ExpectContains(response, "21", "Grok did not incorporate the function result into the final answer.");
    });

    await RunTestAsync("streaming completion", async () =>
    {
        var options = CreateGrokOptions();
        var streamedBuilder = new StringBuilder();

        await foreach (var chunk in client.StreamCompletionAsync("Reply with exactly SUNRISE_OK.", options, usageTracker))
        {
            _ = streamedBuilder.Append(chunk);
        }

        ExpectContains(streamedBuilder.ToString(), "SUNRISE_OK", "Streaming completion did not produce the expected marker.");
    });

    await RunTestAsync("streaming function calling", async () =>
    {
        var options = CreateGrokOptions();
        var functionCalled = false;
        var streamedBuilder = new StringBuilder();

        options.AddFunction("LookupRiverLength", "Returns the length of a river in kilometers.", (string river) =>
        {
            functionCalled = true;
            if (!river.Contains("Vistula", StringComparison.OrdinalIgnoreCase))
            {
                return $"Error: Unsupported river '{river}'.";
            }

            return "The Vistula river is 1047 kilometers long.";
        });

        await foreach (var chunk in client.StreamCompletionAsync("Use LookupRiverLength for the Vistula river, then answer with one short sentence ending in STREAM_TOOL_OK.", options, usageTracker))
        {
            _ = streamedBuilder.Append(chunk);
        }

        if (!functionCalled)
        {
            throw new InvalidOperationException("Grok did not invoke the expected function during streaming.");
        }

        var response = streamedBuilder.ToString();
        ExpectContainsNormalizedNumber(response, "1047", "Streaming function calling did not incorporate the tool result.");
        ExpectContains(response, "STREAM_TOOL_OK", "Streaming function calling did not finish with the expected marker.");
    });

    await RunTestAsync("structured outputs", async () =>
    {
        var options = CreateGrokOptions();
        options.ResponseType = typeof(StructuredCityResponse);

        var response = await client.CompleteAsync("Return structured data for the city Warsaw in Poland on the continent Europe.", options, usageTracker);
        var result = JsonSerializer.Deserialize<StructuredCityResponse>(response, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (result is null)
        {
            throw new InvalidOperationException("Grok did not return valid structured JSON.");
        }

        ExpectContains(result.City, "Warsaw", "Structured output returned the wrong city.");
        ExpectContains(result.Country, "Poland", "Structured output returned the wrong country.");
        ExpectContains(result.Continent, "Europe", "Structured output returned the wrong continent.");
    });

    await RunTestAsync("nested structured outputs", async () =>
    {
        var options = CreateGrokOptions();
        options.ResponseType = typeof(StructuredTravelGuideResponse);

        var response = await client.CompleteAsync(
            "Return a structured travel guide for Warsaw with exactly two highlights named Old Town and Vistula Boulevards. Include a metadata object with season spring and family_friendly true.",
            options,
            usageTracker);

        var result = JsonSerializer.Deserialize<StructuredTravelGuideResponse>(response, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (result is null)
        {
            throw new InvalidOperationException("Grok did not return valid nested structured JSON.");
        }

        ExpectContains(result.City, "Warsaw", "Nested structured output returned the wrong city.");
        if (result.Highlights.Count != 2)
        {
            throw new InvalidOperationException($"Nested structured output returned {result.Highlights.Count} highlights instead of 2.");
        }

        ExpectContains(result.Highlights[0].Name + " " + result.Highlights[1].Name, "Old Town", "Nested structured output omitted Old Town.");
        ExpectContains(result.Highlights[0].Name + " " + result.Highlights[1].Name, "Vistula", "Nested structured output omitted Vistula Boulevards.");
        ExpectContains(result.Metadata.Season, "spring", "Nested structured output returned the wrong season.");

        if (!result.Metadata.FamilyFriendly)
        {
            throw new InvalidOperationException("Nested structured output returned the wrong family_friendly flag.");
        }
    });

    await RunTestAsync("function calling with structured output", async () =>
    {
        var options = CreateGrokOptions();
        options.ResponseType = typeof(StructuredWeatherSummaryResponse);
        options.IsStrictFunctionCallingOn = false;
        options.IsParallelFunctionCallingOn = true;

        var requestedCities = new List<string>();
        options.AddFunction("GetCityTemperature", "Returns the temperature for a city in Celsius.", (string city) =>
        {
            requestedCities.Add(city);
            return city.Contains("Warsaw", StringComparison.OrdinalIgnoreCase)
                ? "{\"city\":\"Warsaw\",\"temperature_c\":21}"
                : city.Contains("Krakow", StringComparison.OrdinalIgnoreCase)
                    ? "{\"city\":\"Krakow\",\"temperature_c\":19}"
                    : $"Error: Unsupported city '{city}'.";
        });

        var response = await client.CompleteAsync(
            "Call GetCityTemperature for Warsaw and Krakow, then return structured JSON listing both readings, the warmest city, and the temperature difference.",
            options,
            usageTracker);

        var result = JsonSerializer.Deserialize<StructuredWeatherSummaryResponse>(response, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (result is null)
        {
            throw new InvalidOperationException("Grok did not return valid structured JSON after tool execution.");
        }

        if (requestedCities.Count < 2)
        {
            throw new InvalidOperationException($"Grok only invoked GetCityTemperature {requestedCities.Count} times.");
        }

        ExpectContains(string.Join(' ', requestedCities), "Warsaw", "Grok never requested Warsaw weather.");
        ExpectContains(string.Join(' ', requestedCities), "Krakow", "Grok never requested Krakow weather.");
        if (result.Readings.Count != 2)
        {
            throw new InvalidOperationException($"Structured weather summary returned {result.Readings.Count} readings instead of 2.");
        }

        ExpectContains(result.WarmestCity, "Warsaw", "Structured weather summary returned the wrong warmest city.");
        if (result.DifferenceC != 2)
        {
            throw new InvalidOperationException($"Structured weather summary returned the wrong temperature difference: {result.DifferenceC}.");
        }
    });

    await RunTestAsync("json mode", async () =>
    {
        var options = CreateGrokOptions();
        options.IsJsonMode = true;

        var response = await client.CompleteAsync("Return a JSON object with {\"status\":\"GROK_JSON_OK\"}.", options, usageTracker);
        using var document = JsonDocument.Parse(response);
        var status = document.RootElement.GetProperty("status").GetString();

        if (!string.Equals(status, "GROK_JSON_OK", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("JSON mode did not return the expected status field.");
        }
    });

    await RunTestAsync("vision input", async () =>
    {
        var options = CreateGrokOptions();
        var chat = new Chat();
        chat.FromUser("What single color dominates this image? Reply with one word.", imageUrls: ["https://dummyimage.com/256x256/ff0000/ffffff.png"]);

        var response = await client.CompleteAsync(chat, options, usageTracker);
        ExpectContains(response, "red", "Vision completion did not identify the dominant color.");
    });

    await RunTestAsync("text to speech", async () =>
    {
        var audio = await client.SynthesizeSpeechAsync("Hello from the Grok smoke test.", new ChatAIze.GenerativeCS.Options.Grok.TextToSpeechOptions
        {
            VoiceId = GrokTextToSpeechVoices.Eve,
            ResponseFormat = VoiceResponseFormat.MP3
        });

        if (audio.Length == 0)
        {
            throw new InvalidOperationException("Grok text-to-speech returned an empty audio payload.");
        }

        if (audio.Length < 4 || (audio[0] != 0x49 && audio[0] != 0xFF))
        {
            throw new InvalidOperationException("Grok text-to-speech returned bytes that do not look like MP3 audio.");
        }
    });

    await RunTestAsync("text to speech compatibility", async () =>
    {
        var tempAudioPath = Path.Combine(Path.GetTempPath(), $"grok-smoke-{Guid.NewGuid():N}.wav");

        try
        {
            await client.SynthesizeSpeechAsync("Compatibility path test for Grok text to speech.", tempAudioPath, new ChatAIze.GenerativeCS.Options.Grok.TextToSpeechOptions
            {
                Voice = TextToSpeechVoice.Nova,
                Codec = "wav",
                SampleRate = 16000
            });

            var audio = await File.ReadAllBytesAsync(tempAudioPath);
            if (audio.Length < 4)
            {
                throw new InvalidOperationException("Grok text-to-speech compatibility path wrote an unexpectedly short file.");
            }

            var riffHeader = Encoding.ASCII.GetString(audio, 0, 4);
            if (!string.Equals(riffHeader, "RIFF", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Grok WAV synthesis did not produce a RIFF header.");
            }
        }
        finally
        {
            if (File.Exists(tempAudioPath))
            {
                File.Delete(tempAudioPath);
            }
        }
    });

    await RunTestAsync("dependency injection", async () =>
    {
        var services = new ServiceCollection();
        services.AddGrokClient(configure =>
        {
            configure.ApiKey = client.ApiKey;
            configure.DefaultCompletionOptions = CreateGrokOptions();
        });

        using var serviceProvider = services.BuildServiceProvider();
        var resolvedClient = serviceProvider.GetRequiredService<GrokClient>();
        var response = await resolvedClient.CompleteAsync("Reply with exactly GROK_DI_OK.");

        ExpectContains(response, "GROK_DI_OK", "The DI-registered Grok client did not complete successfully.");
    });

    Console.WriteLine();
    Console.WriteLine($"Grok smoke tests passed. Prompt tokens: {usageTracker.PromptTokens}, cached tokens: {usageTracker.CachedTokens}, completion tokens: {usageTracker.CompletionTokens}");
}

static ChatAIze.GenerativeCS.Options.Grok.ChatCompletionOptions CreateGrokOptions()
{
    return new ChatAIze.GenerativeCS.Options.Grok.ChatCompletionOptions
    {
        Model = ChatCompletionModels.Grok.Grok41FastNonReasoning,
        MaxAttempts = 3,
        MaxOutputTokens = 512,
        Temperature = 0,
        TopP = 1,
        IsStrictFunctionCallingOn = true,
        UserTrackingId = "preview-grok-smoke"
    };
}

static async Task RunInteractiveOpenAIChatAsync()
{
    var client = new OpenAIClient();
    var chat = new Chat();

    var options = new ChatAIze.GenerativeCS.Options.OpenAI.ChatCompletionOptions
    {
        Model = ChatCompletionModels.OpenAI.GPT54,
        IsStoringOutputs = true,
        IsDebugMode = true,
        ReasoningEffort = ReasoningEffort.None,
        Verbosity = Verbosity.High
    };

    options.AddFunction("Check City Temperature", (string city) => Random.Shared.Next(0, 100));
    options.AddFunction("Check Country Temperature", async (string country) =>
    {
        await Task.Delay(5000);
        return new { Temp = Random.Shared.Next(0, 100) };
    });

    while (true)
    {
        var input = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(input))
        {
            break;
        }

        chat.FromUser(input);

        var response = await client.CompleteAsync(chat, options);
        Console.WriteLine(response);
    }
}

static async Task RunOpenAISmokeTestsAsync()
{
    Console.WriteLine("OpenAI smoke tests starting...");

    var client = new OpenAIClient();
    if (string.IsNullOrWhiteSpace(client.ApiKey))
    {
        throw new InvalidOperationException("OpenAI API key was not found. Export OPENAI_API_KEY before running the preview.");
    }

    await RunTestAsync("simple completion", async () =>
    {
        var response = await client.CompleteAsync("Reply with exactly OPENAI_OK.", new ChatAIze.GenerativeCS.Options.OpenAI.ChatCompletionOptions
        {
            Model = ChatCompletionModels.OpenAI.GPT54,
            MaxOutputTokens = 64,
            Temperature = 0
        });

        ExpectContains(response, "OPENAI_OK", "OpenAI simple completion did not return the expected marker.");
    });

    await RunTestAsync("streaming completion", async () =>
    {
        var streamedBuilder = new StringBuilder();

        await foreach (var chunk in client.StreamCompletionAsync("Reply with exactly OPENAI_STREAM_OK.", new ChatAIze.GenerativeCS.Options.OpenAI.ChatCompletionOptions
        {
            Model = ChatCompletionModels.OpenAI.GPT54,
            MaxOutputTokens = 64,
            Temperature = 0
        }))
        {
            _ = streamedBuilder.Append(chunk);
        }

        ExpectContains(streamedBuilder.ToString(), "OPENAI_STREAM_OK", "OpenAI streaming completion did not return the expected marker.");
    });

    await RunTestAsync("chat continuation", async () =>
    {
        var options = new ChatAIze.GenerativeCS.Options.OpenAI.ChatCompletionOptions
        {
            Model = ChatCompletionModels.OpenAI.GPT54,
            MaxOutputTokens = 128,
            Temperature = 0
        };

        var chat = new Chat { UserTrackingId = "preview-openai-chat-user" };
        chat.FromUser("Remember this fact exactly: my favorite tree is oak.");
        _ = await client.CompleteAsync(chat, options);

        chat.FromUser("What is my favorite tree? Reply with one short sentence.");
        var response = await client.CompleteAsync(chat, options);
        ExpectContains(response, "oak", "OpenAI did not preserve the chat context across turns.");
    });

    await RunTestAsync("function calling", async () =>
    {
        var options = new ChatAIze.GenerativeCS.Options.OpenAI.ChatCompletionOptions
        {
            Model = ChatCompletionModels.OpenAI.GPT54,
            MaxOutputTokens = 128,
            Temperature = 0
        };

        var functionCalled = false;
        options.AddFunction("GetPreviewNumber", "Returns a deterministic preview number.", () =>
        {
            functionCalled = true;
            return 42;
        });

        var response = await client.CompleteAsync("Use GetPreviewNumber and tell me the number.", options);

        if (!functionCalled)
        {
            throw new InvalidOperationException("OpenAI did not invoke the expected function.");
        }

        ExpectContains(response, "42", "OpenAI did not incorporate the function result into the final answer.");
    });

    await RunTestAsync("streaming function calling", async () =>
    {
        var options = new ChatAIze.GenerativeCS.Options.OpenAI.ChatCompletionOptions
        {
            Model = ChatCompletionModels.OpenAI.GPT54,
            MaxOutputTokens = 128,
            Temperature = 0
        };

        var functionCalled = false;
        var streamedBuilder = new StringBuilder();

        options.AddFunction("LookupRiverLength", "Returns the length of a river in kilometers.", (string river) =>
        {
            functionCalled = true;
            if (!river.Contains("Vistula", StringComparison.OrdinalIgnoreCase))
            {
                return $"Error: Unsupported river '{river}'.";
            }

            return "The Vistula river is 1047 kilometers long.";
        });

        await foreach (var chunk in client.StreamCompletionAsync("Use LookupRiverLength for the Vistula river, then answer with one short sentence ending in OPENAI_STREAM_TOOL_OK.", options))
        {
            _ = streamedBuilder.Append(chunk);
        }

        if (!functionCalled)
        {
            throw new InvalidOperationException("OpenAI did not invoke the expected function during streaming.");
        }

        var response = streamedBuilder.ToString();
        ExpectContainsNormalizedNumber(response, "1047", "OpenAI streaming function calling did not incorporate the tool result.");
        ExpectContains(response, "OPENAI_STREAM_TOOL_OK", "OpenAI streaming function calling did not finish with the expected marker.");
    });

    await RunTestAsync("structured outputs", async () =>
    {
        var response = await client.CompleteAsync("Return structured data for city Warsaw in country Poland.", new ChatAIze.GenerativeCS.Options.OpenAI.ChatCompletionOptions
        {
            Model = ChatCompletionModels.OpenAI.GPT54,
            MaxOutputTokens = 128,
            Temperature = 0,
            ResponseType = typeof(StructuredCityResponse)
        });

        var result = JsonSerializer.Deserialize<StructuredCityResponse>(response, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (result is null)
        {
            throw new InvalidOperationException("OpenAI did not return valid structured JSON.");
        }

        ExpectContains(result.City, "Warsaw", "OpenAI structured output returned the wrong city.");
        ExpectContains(result.Country, "Poland", "OpenAI structured output returned the wrong country.");
    });

    await RunTestAsync("nested structured outputs", async () =>
    {
        var response = await client.CompleteAsync(
            "Return a structured travel guide for Warsaw with exactly two highlights named Old Town and Vistula Boulevards. Include a metadata object with season spring and family_friendly true.",
            new ChatAIze.GenerativeCS.Options.OpenAI.ChatCompletionOptions
            {
                Model = ChatCompletionModels.OpenAI.GPT54,
                MaxOutputTokens = 256,
                Temperature = 0,
                ResponseType = typeof(StructuredTravelGuideResponse)
            });

        var result = JsonSerializer.Deserialize<StructuredTravelGuideResponse>(response, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (result is null)
        {
            throw new InvalidOperationException("OpenAI did not return valid nested structured JSON.");
        }

        ExpectContains(result.City, "Warsaw", "OpenAI nested structured output returned the wrong city.");
        if (result.Highlights.Count != 2)
        {
            throw new InvalidOperationException($"OpenAI nested structured output returned {result.Highlights.Count} highlights instead of 2.");
        }

        ExpectContains(result.Highlights[0].Name + " " + result.Highlights[1].Name, "Old Town", "OpenAI nested structured output omitted Old Town.");
        ExpectContains(result.Highlights[0].Name + " " + result.Highlights[1].Name, "Vistula", "OpenAI nested structured output omitted Vistula Boulevards.");
        ExpectContains(result.Metadata.Season, "spring", "OpenAI nested structured output returned the wrong season.");

        if (!result.Metadata.FamilyFriendly)
        {
            throw new InvalidOperationException("OpenAI nested structured output returned the wrong family_friendly flag.");
        }
    });

    await RunTestAsync("function calling with structured output", async () =>
    {
        var options = new ChatAIze.GenerativeCS.Options.OpenAI.ChatCompletionOptions
        {
            Model = ChatCompletionModels.OpenAI.GPT54,
            MaxOutputTokens = 256,
            Temperature = 0,
            ResponseType = typeof(StructuredWeatherSummaryResponse),
            IsStrictFunctionCallingOn = false,
            IsParallelFunctionCallingOn = true
        };

        var requestedCities = new List<string>();
        options.AddFunction("GetCityTemperature", "Returns the temperature for a city in Celsius.", (string city) =>
        {
            requestedCities.Add(city);
            return city.Contains("Warsaw", StringComparison.OrdinalIgnoreCase)
                ? "{\"city\":\"Warsaw\",\"temperature_c\":21}"
                : city.Contains("Krakow", StringComparison.OrdinalIgnoreCase)
                    ? "{\"city\":\"Krakow\",\"temperature_c\":19}"
                    : $"Error: Unsupported city '{city}'.";
        });

        var response = await client.CompleteAsync(
            "Call GetCityTemperature for Warsaw and Krakow, then return structured JSON listing both readings, the warmest city, and the temperature difference.",
            options);

        var result = JsonSerializer.Deserialize<StructuredWeatherSummaryResponse>(response, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (result is null)
        {
            throw new InvalidOperationException("OpenAI did not return valid structured JSON after tool execution.");
        }

        if (requestedCities.Count < 2)
        {
            throw new InvalidOperationException($"OpenAI only invoked GetCityTemperature {requestedCities.Count} times.");
        }

        ExpectContains(string.Join(' ', requestedCities), "Warsaw", "OpenAI never requested Warsaw weather.");
        ExpectContains(string.Join(' ', requestedCities), "Krakow", "OpenAI never requested Krakow weather.");
        if (result.Readings.Count != 2)
        {
            throw new InvalidOperationException($"OpenAI structured weather summary returned {result.Readings.Count} readings instead of 2.");
        }

        ExpectContains(result.WarmestCity, "Warsaw", "OpenAI structured weather summary returned the wrong warmest city.");
        if (result.DifferenceC != 2)
        {
            throw new InvalidOperationException($"OpenAI structured weather summary returned the wrong temperature difference: {result.DifferenceC}.");
        }
    });

    await RunTestAsync("json mode", async () =>
    {
        var response = await client.CompleteAsync("Return a JSON object with {\"status\":\"OPENAI_JSON_OK\"}.", new ChatAIze.GenerativeCS.Options.OpenAI.ChatCompletionOptions
        {
            Model = ChatCompletionModels.OpenAI.GPT54,
            MaxOutputTokens = 64,
            Temperature = 0,
            IsJsonMode = true
        });

        using var document = JsonDocument.Parse(response);
        var status = document.RootElement.GetProperty("status").GetString();
        if (!string.Equals(status, "OPENAI_JSON_OK", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("OpenAI JSON mode did not return the expected status field.");
        }
    });

    await RunTestAsync("vision input", async () =>
    {
        var chat = new Chat();
        chat.FromUser("What single color dominates this image? Reply with one word.", imageUrls: ["https://dummyimage.com/256x256/ff0000/ffffff.png"]);

        var response = await client.CompleteAsync(chat, new ChatAIze.GenerativeCS.Options.OpenAI.ChatCompletionOptions
        {
            Model = ChatCompletionModels.OpenAI.GPT54,
            MaxOutputTokens = 32,
            Temperature = 0
        });

        ExpectContains(response, "red", "OpenAI vision completion did not identify the dominant color.");
    });

    await RunTestAsync("embeddings", async () =>
    {
        var embedding = await client.GetEmbeddingAsync("OpenAI embedding smoke test.");
        if (embedding.Length == 0)
        {
            throw new InvalidOperationException("OpenAI embedding request returned an empty vector.");
        }

        var base64Embedding = await client.GetBase64EmbeddingAsync("OpenAI base64 embedding smoke test.");
        if (string.IsNullOrWhiteSpace(base64Embedding))
        {
            throw new InvalidOperationException("OpenAI base64 embedding request returned an empty payload.");
        }

        var decodedEmbedding = Convert.FromBase64String(base64Embedding);
        if (decodedEmbedding.Length == 0)
        {
            throw new InvalidOperationException("OpenAI base64 embedding decoded to an empty byte array.");
        }
    });

    await RunTestAsync("moderation", async () =>
    {
        var safeResult = await client.ModerateAsync("I enjoyed reading a book on a quiet afternoon.");
        if (safeResult.IsFlagged)
        {
            throw new InvalidOperationException("OpenAI moderation incorrectly flagged clearly safe content.");
        }

        var unsafeResult = await client.ModerateAsync("I am going to kill you tonight and burn your house down.");
        if (!unsafeResult.IsFlagged)
        {
            throw new InvalidOperationException("OpenAI moderation failed to flag explicit violent threats.");
        }

        if (!unsafeResult.IsViolence && !unsafeResult.IsHarassmentThreatening)
        {
            throw new InvalidOperationException("OpenAI moderation flagged the threat, but not in a violence- or threat-related category.");
        }
    });

    await RunTestAsync("speech round-trip", async () =>
    {
        const string spanishText = "Hola mundo. El gato negro duerme en la silla.";
        var tempAudioPath = Path.Combine(Path.GetTempPath(), $"openai-smoke-{Guid.NewGuid():N}.mp3");

        try
        {
            var audio = await client.SynthesizeSpeechAsync(spanishText, new ChatAIze.GenerativeCS.Options.OpenAI.TextToSpeechOptions
            {
                ResponseFormat = VoiceResponseFormat.MP3
            });

            if (audio.Length == 0)
            {
                throw new InvalidOperationException("OpenAI text-to-speech returned an empty audio payload.");
            }

            await File.WriteAllBytesAsync(tempAudioPath, audio);

            var transcript = await client.TranscriptAsync(tempAudioPath, new ChatAIze.GenerativeCS.Options.OpenAI.TranscriptionOptions(language: "es")
            {
                ResponseFormat = TranscriptionResponseFormat.Text
            });

            ExpectContains(transcript, "gato", "OpenAI transcription did not preserve the expected Spanish content.");

            var translation = await client.TranslateAsync(tempAudioPath, new ChatAIze.GenerativeCS.Options.OpenAI.TranslationOptions
            {
                ResponseFormat = TranscriptionResponseFormat.Text
            });

            // Synthetic TTS clips are sufficient to verify the translation endpoint wiring,
            // but they are not reliable for asserting provider-level translation quality.
            if (string.IsNullOrWhiteSpace(translation))
            {
                throw new InvalidOperationException("OpenAI translation returned an empty response.");
            }
        }
        finally
        {
            if (File.Exists(tempAudioPath))
            {
                File.Delete(tempAudioPath);
            }
        }
    });

    await RunTestAsync("dependency injection", async () =>
    {
        var services = new ServiceCollection();
        services.AddOpenAIClient(configure =>
        {
            configure.ApiKey = client.ApiKey;
            configure.DefaultCompletionOptions = new ChatAIze.GenerativeCS.Options.OpenAI.ChatCompletionOptions
            {
                Model = ChatCompletionModels.OpenAI.GPT54,
                MaxOutputTokens = 64,
                Temperature = 0
            };
        });

        using var serviceProvider = services.BuildServiceProvider();
        var resolvedClient = serviceProvider.GetRequiredService<OpenAIClient>();
        var response = await resolvedClient.CompleteAsync("Reply with exactly OPENAI_DI_OK.");

        ExpectContains(response, "OPENAI_DI_OK", "The DI-registered OpenAI client did not complete successfully.");
    });

    Console.WriteLine();
    Console.WriteLine("OpenAI smoke tests passed.");
}

static async Task RunTestAsync(string name, Func<Task> test)
{
    Console.Write($"{name}... ");
    await test();
    Console.WriteLine("OK");
}

static void ExpectContains(string value, string expectedSubstring, string failureMessage)
{
    if (!value.Contains(expectedSubstring, StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException($"{failureMessage} Actual response: {value}");
    }
}

static void ExpectContainsNormalizedNumber(string value, string expectedDigits, string failureMessage)
{
    var normalizedValue = new string(value.Where(char.IsDigit).ToArray());
    if (!normalizedValue.Contains(expectedDigits, StringComparison.Ordinal))
    {
        throw new InvalidOperationException($"{failureMessage} Actual response: {value}");
    }
}

file sealed class StructuredCityResponse
{
    [JsonPropertyName("city")]
    public string City { get; set; } = string.Empty;

    [JsonPropertyName("country")]
    public string Country { get; set; } = string.Empty;

    [JsonPropertyName("continent")]
    public string Continent { get; set; } = string.Empty;
}

file sealed class StructuredTravelGuideResponse
{
    [JsonPropertyName("city")]
    public string City { get; set; } = string.Empty;

    [JsonPropertyName("highlights")]
    public List<StructuredTravelHighlightResponse> Highlights { get; set; } = [];

    [JsonPropertyName("metadata")]
    public StructuredTravelMetadataResponse Metadata { get; set; } = new();
}

file sealed class StructuredTravelHighlightResponse
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;
}

file sealed class StructuredTravelMetadataResponse
{
    [JsonPropertyName("season")]
    public string Season { get; set; } = string.Empty;

    [JsonPropertyName("family_friendly")]
    public bool FamilyFriendly { get; set; }
}

file sealed class StructuredWeatherSummaryResponse
{
    [JsonPropertyName("readings")]
    public List<StructuredWeatherReadingResponse> Readings { get; set; } = [];

    [JsonPropertyName("warmest_city")]
    public string WarmestCity { get; set; } = string.Empty;

    [JsonPropertyName("difference_c")]
    public int DifferenceC { get; set; }
}

file sealed class StructuredWeatherReadingResponse
{
    [JsonPropertyName("city")]
    public string City { get; set; } = string.Empty;

    [JsonPropertyName("temperature_c")]
    public int TemperatureC { get; set; }
}
