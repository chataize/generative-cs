using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using ChatAIze.GenerativeCS.Clients;
using ChatAIze.GenerativeCS.Constants;
using ChatAIze.GenerativeCS.Enums;
using ChatAIze.GenerativeCS.Extensions;
using ChatAIze.GenerativeCS.Models;
using ChatAIze.GenerativeCS.Options.Claude;
using ChatAIze.GenerativeCS.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

var argsList = args.Select(arg => arg.Trim()).ToArray();
var shouldRunClaudeSmoke = argsList.Length == 0 || argsList.Any(arg => arg.Equals("claude-smoke", StringComparison.OrdinalIgnoreCase));
var shouldRunGeminiSmoke = argsList.Any(arg => arg.Equals("gemini-smoke", StringComparison.OrdinalIgnoreCase));
var shouldRunGeminiChatSmoke = argsList.Any(arg => arg.Equals("gemini-chat-smoke", StringComparison.OrdinalIgnoreCase));
var shouldRunGeminiTailSmoke = argsList.Any(arg => arg.Equals("gemini-tail-smoke", StringComparison.OrdinalIgnoreCase));
var shouldRunGeminiLocalSmoke = argsList.Any(arg => arg.Equals("gemini-local-smoke", StringComparison.OrdinalIgnoreCase));
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

if (shouldRunGeminiSmoke)
{
    await RunGeminiSmokeTestsAsync();
    return;
}

if (shouldRunGeminiChatSmoke)
{
    await RunGeminiSmokeTestsAsync(runChatAndStructuredOnly: true);
    return;
}

if (shouldRunGeminiTailSmoke)
{
    await RunGeminiSmokeTestsAsync(runTailOnly: true);
    return;
}

if (shouldRunGeminiLocalSmoke)
{
    await RunGeminiLocalRegressionTestsAsync();
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
Console.WriteLine("  dotnet run --project ChatAIze.GenerativeCS.Preview -- gemini-smoke");
Console.WriteLine("  dotnet run --project ChatAIze.GenerativeCS.Preview -- gemini-chat-smoke");
Console.WriteLine("  dotnet run --project ChatAIze.GenerativeCS.Preview -- gemini-tail-smoke");
Console.WriteLine("  dotnet run --project ChatAIze.GenerativeCS.Preview -- gemini-local-smoke");
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
        options.IsStrictFunctionCallingOn = true;
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

static async Task RunGeminiSmokeTestsAsync(bool runChatAndStructuredOnly = false, bool runTailOnly = false)
{
    var suiteName = runChatAndStructuredOnly
        ? "Gemini chat smoke tests"
        : runTailOnly
            ? "Gemini tail smoke tests"
            : "Gemini smoke tests";

    Console.WriteLine($"{suiteName} starting...");

    var client = new GeminiClient();
    if (string.IsNullOrWhiteSpace(client.ApiKey))
    {
        throw new InvalidOperationException("Gemini API key was not found. Export GEMINI_API_KEY before running the preview.");
    }

    var usageTracker = new TokenUsageTracker();

    if (!runTailOnly)
    {
        await RunTestAsync("simple completion", async () =>
        {
            var options = CreateGeminiOptions();
            var response = await client.CompleteAsync("Reply with exactly GEMINI_OK.", options, usageTracker);
            ExpectContains(response, "GEMINI_OK", "Gemini simple completion did not echo the expected marker.");
        });

        await RunTestAsync("chat continuation", async () =>
        {
            var options = CreateGeminiOptions();
            var chat = new Chat { UserTrackingId = "preview-gemini-chat-user" };

            chat.FromUser("Remember this fact exactly: my favorite planet is Saturn.");
            _ = await client.CompleteAsync(chat, options, usageTracker);

            chat.FromUser("What is my favorite planet? Reply with one short sentence.");
            var response = await client.CompleteAsync(chat, options, usageTracker);
            ExpectContains(response, "Saturn", "Gemini did not preserve the chat context across turns.");
        });

        await RunTestAsync("function calling", async () =>
        {
            var options = CreateGeminiOptions();
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
                throw new InvalidOperationException("Gemini did not invoke the expected function.");
            }

            ExpectContains(response, "21", "Gemini did not incorporate the function result into the final answer.");
        });

        await RunTestAsync("streaming completion", async () =>
        {
            var options = CreateGeminiOptions();
            var streamedBuilder = new StringBuilder();

            await foreach (var chunk in client.StreamCompletionAsync("Reply with exactly GEMINI_STREAM_OK.", options, usageTracker))
            {
                _ = streamedBuilder.Append(chunk);
            }

            ExpectContains(streamedBuilder.ToString(), "GEMINI_STREAM_OK", "Gemini streaming completion did not return the expected marker.");
        });

        await RunTestAsync("streaming function calling", async () =>
        {
            var options = CreateGeminiOptions();
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

            await foreach (var chunk in client.StreamCompletionAsync("Use LookupRiverLength for the Vistula river, then answer with one short sentence ending in GEMINI_STREAM_TOOL_OK.", options, usageTracker))
            {
                _ = streamedBuilder.Append(chunk);
            }

            if (!functionCalled)
            {
                throw new InvalidOperationException("Gemini did not invoke the expected function during streaming.");
            }

            var response = streamedBuilder.ToString();
            ExpectContainsNormalizedNumber(response, "1047", "Gemini streaming function calling did not incorporate the tool result.");
            ExpectContains(response, "GEMINI_STREAM_TOOL_OK", "Gemini streaming function calling did not finish with the expected marker.");
        });

        await RunTestAsync("structured outputs", async () =>
        {
            var options = CreateGeminiOptions(ChatCompletionModels.Gemini.Gemini31FlashLitePreview);
            options.ResponseType = typeof(StructuredCityResponse);

            var response = await client.CompleteAsync("Return structured data for the city Warsaw in Poland on the continent Europe.", options, usageTracker);
            var result = JsonSerializer.Deserialize<StructuredCityResponse>(response, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (result is null)
            {
                throw new InvalidOperationException("Gemini did not return valid structured JSON.");
            }

            ExpectContains(result.City, "Warsaw", "Gemini structured output returned the wrong city.");
            ExpectContains(result.Country, "Poland", "Gemini structured output returned the wrong country.");
            ExpectContains(result.Continent, "Europe", "Gemini structured output returned the wrong continent.");
        });

        await RunTestAsync("nested structured outputs", async () =>
        {
            var options = CreateGeminiOptions(ChatCompletionModels.Gemini.Gemini31FlashLitePreview);
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
                throw new InvalidOperationException("Gemini did not return valid nested structured JSON.");
            }

            ExpectContains(result.City, "Warsaw", "Gemini nested structured output returned the wrong city.");
            if (result.Highlights.Count != 2)
            {
                throw new InvalidOperationException($"Gemini nested structured output returned {result.Highlights.Count} highlights instead of 2.");
            }

            ExpectContains(result.Highlights[0].Name + " " + result.Highlights[1].Name, "Old Town", "Gemini nested structured output omitted Old Town.");
            ExpectContains(result.Highlights[0].Name + " " + result.Highlights[1].Name, "Vistula", "Gemini nested structured output omitted Vistula Boulevards.");
            ExpectContains(result.Metadata.Season, "spring", "Gemini nested structured output returned the wrong season.");

            if (!result.Metadata.FamilyFriendly)
            {
                throw new InvalidOperationException("Gemini nested structured output returned the wrong family_friendly flag.");
            }
        });

        await RunTestAsync("function calling with structured output", async () =>
        {
            var options = CreateGeminiOptions(ChatCompletionModels.Gemini.Gemini31FlashLitePreview);
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
                throw new InvalidOperationException("Gemini did not return valid structured JSON after tool execution.");
            }

            if (requestedCities.Count < 2)
            {
                throw new InvalidOperationException($"Gemini only invoked GetCityTemperature {requestedCities.Count} times.");
            }

            ExpectContains(string.Join(' ', requestedCities), "Warsaw", "Gemini never requested Warsaw weather.");
            ExpectContains(string.Join(' ', requestedCities), "Krakow", "Gemini never requested Krakow weather.");
            if (result.Readings.Count != 2)
            {
                throw new InvalidOperationException($"Gemini structured weather summary returned {result.Readings.Count} readings instead of 2.");
            }

            ExpectContains(result.WarmestCity, "Warsaw", "Gemini structured weather summary returned the wrong warmest city.");
            if (result.DifferenceC != 2)
            {
                throw new InvalidOperationException($"Gemini structured weather summary returned the wrong temperature difference: {result.DifferenceC}.");
            }
        });

        await RunTestAsync("manual function schema with default callback", async () =>
        {
            var options = CreateGeminiOptions(ChatCompletionModels.Gemini.Gemini20Flash);
            string? requestedOrigin = null;
            string? requestedDestination = null;
            string? requestedTravelMode = null;

            options.AddFunction(new ChatFunction
            {
                Name = "LookupRoutePlan",
                Description = "Returns a route plan between two cities.",
                Parameters =
                [
                    new FunctionParameter(typeof(string), "origin", "Origin city."),
                    new FunctionParameter(typeof(string), "destination", "Destination city."),
                    new FunctionParameter(typeof(string), "travel_mode", "Travel mode.", true, ["train", "car"])
                ]
            });

            options.DefaultFunctionCallback = (name, arguments, _) =>
            {
                if (!name.Equals("LookupRoutePlan", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException($"Unexpected function callback: {name}.");
                }

                using var document = JsonDocument.Parse(arguments);
                requestedOrigin = document.RootElement.GetProperty("origin").GetString();
                requestedDestination = document.RootElement.GetProperty("destination").GetString();
                requestedTravelMode = document.RootElement.GetProperty("travel_mode").GetString();

                object result = new
                {
                    route_id = "PL-TR-452",
                    duration_minutes = 163,
                    summary = "Direct train from Warsaw to Gdansk."
                };

                return ValueTask.FromResult<object?>(result);
            };

            var response = await client.CompleteAsync(
                "Use LookupRoutePlan for a train trip from Warsaw to Gdansk, then answer with the route id, the duration in minutes, and end with GEMINI_MANUAL_TOOL_OK.",
                options,
                usageTracker);

            ExpectContains(requestedOrigin ?? string.Empty, "Warsaw", "Gemini manual function schema used the wrong origin.");
            ExpectContains(requestedDestination ?? string.Empty, "Gdansk", "Gemini manual function schema used the wrong destination.");
            ExpectContains(requestedTravelMode ?? string.Empty, "train", "Gemini manual function schema used the wrong travel mode.");
            ExpectContains(response, "PL-TR-452", "Gemini manual function schema response omitted the route id.");
            ExpectContainsNormalizedNumber(response, "163", "Gemini manual function schema response omitted the duration.");
            ExpectContains(response, "GEMINI_MANUAL_TOOL_OK", "Gemini manual function schema did not finish with the expected marker.");
        });

        await RunTestAsync("tool memory after function calls", async () =>
        {
            var firstTurnOptions = CreateGeminiOptions(ChatCompletionModels.Gemini.Gemini20Flash);
            firstTurnOptions.IsStrictFunctionCallingOn = false;
            var museumFunctionCalled = false;
            var tramFunctionCalled = false;
            var chat = new Chat();

            firstTurnOptions.AddFunction("LookupMuseumHours", "Returns the closing time for a museum.", (string museum) =>
            {
                museumFunctionCalled = true;
                return museum.Contains("Royal Castle", StringComparison.OrdinalIgnoreCase)
                    ? "The Royal Castle closes at 18:00."
                    : $"Error: Unsupported museum '{museum}'.";
            });

            firstTurnOptions.AddFunction("LookupTramLine", "Returns the recommended tram line for a route.", (string from, string to) =>
            {
                tramFunctionCalled = true;
                if (!from.Contains("Central Station", StringComparison.OrdinalIgnoreCase)
                    || !to.Contains("Old Town", StringComparison.OrdinalIgnoreCase))
                {
                    return $"Error: Unsupported route '{from}' -> '{to}'.";
                }

                return "Take tram line 4 from Central Station to Old Town.";
            });

            chat.FromUser("Use LookupMuseumHours for the Royal Castle and LookupTramLine from Central Station to Old Town, then answer with one short sentence ending GEMINI_MULTI_TOOL_OK.");
            var firstTurnResponse = await client.CompleteAsync(chat, firstTurnOptions, usageTracker);

            if (!museumFunctionCalled || !tramFunctionCalled)
            {
                throw new InvalidOperationException("Gemini did not invoke both functions during the multi-tool turn.");
            }

            ExpectContains(firstTurnResponse, "18:00", "Gemini multi-tool response omitted the museum closing time.");
            ExpectContainsNormalizedNumber(firstTurnResponse, "4", "Gemini multi-tool response omitted the tram line.");
            ExpectContains(firstTurnResponse, "GEMINI_MULTI_TOOL_OK", "Gemini multi-tool turn did not finish with the expected marker.");

            // This follow-up reuses the same chat but removes live tools to verify that Gemini can
            // continue from the stored tool-call and tool-response history without re-executing them.
            var followUpOptions = CreateGeminiOptions(ChatCompletionModels.Gemini.Gemini20Flash);
            chat.FromUser("Without calling any tools, restate the museum closing time and tram line in one short sentence ending GEMINI_TOOL_MEMORY_OK.");
            var followUpResponse = await client.CompleteAsync(chat, followUpOptions, usageTracker);

            ExpectContains(followUpResponse, "18:00", "Gemini follow-up after tool calls lost the museum closing time.");
            ExpectContainsNormalizedNumber(followUpResponse, "4", "Gemini follow-up after tool calls lost the tram line.");
            ExpectContains(followUpResponse, "GEMINI_TOOL_MEMORY_OK", "Gemini follow-up after tool calls did not finish with the expected marker.");
        });

        await RunTestAsync("multi-step tool chain", async () =>
        {
            var options = CreateGeminiOptions(ChatCompletionModels.Gemini.Gemini20Flash);
            options.IsStrictFunctionCallingOn = false;
            var resolvedStopIds = new List<string>();
            var requestedStopIds = new List<string>();

            options.AddFunction("ResolveStopId", "Returns an opaque stop id for a named stop.", (string stopName) =>
            {
                resolvedStopIds.Add(stopName);
                return stopName.Contains("Old Town", StringComparison.OrdinalIgnoreCase)
                    ? "{\"stop_id\":\"stop_old_town_91c2\"}"
                    : $"Error: Unsupported stop '{stopName}'.";
            });

            options.AddFunction("LookupDepartureBoard", "Returns the next tram departure for a stop id.", (string stopId) =>
            {
                requestedStopIds.Add(stopId);
                return stopId.Equals("stop_old_town_91c2", StringComparison.Ordinal)
                    ? "{\"line\":\"4\",\"departure_time\":\"18:12\"}"
                    : $"Error: Unsupported stop id '{stopId}'.";
            });

            var response = await client.CompleteAsync(
                "First call ResolveStopId for Old Town. Then call LookupDepartureBoard using the returned stop id. Answer with the tram line and departure time, ending with GEMINI_TOOL_CHAIN_OK.",
                options,
                usageTracker);

            ExpectContains(string.Join(' ', resolvedStopIds), "Old Town", "Gemini tool chain never resolved the Old Town stop id.");
            ExpectContains(string.Join(' ', requestedStopIds), "stop_old_town_91c2", "Gemini tool chain did not pass the resolved stop id into the second tool.");
            ExpectContainsNormalizedNumber(response, "4", "Gemini tool chain response omitted the tram line.");
            ExpectContains(response, "18:12", "Gemini tool chain response omitted the departure time.");
            ExpectContains(response, "GEMINI_TOOL_CHAIN_OK", "Gemini tool chain did not finish with the expected marker.");
        });

        await RunTestAsync("double-check function execution", async () =>
        {
            var options = CreateGeminiOptions(ChatCompletionModels.Gemini.Gemini20Flash);
            var deleteInvocationCount = 0;

            options.AddFunction("DeleteReminder", "Deletes a reminder by id.", true, (string reminderId) =>
            {
                deleteInvocationCount++;
                return reminderId.Contains("42", StringComparison.OrdinalIgnoreCase)
                    ? "Reminder 42 deleted."
                    : $"Error: Unsupported reminder '{reminderId}'.";
            });

            var response = await client.CompleteAsync(
                "Delete reminder 42. If the tool asks you to confirm first, confirm and call DeleteReminder again. End with GEMINI_DOUBLE_CHECK_OK.",
                options,
                usageTracker);

            if (deleteInvocationCount != 1)
            {
                throw new InvalidOperationException($"Gemini double-check tool executed {deleteInvocationCount} times instead of exactly once after confirmation.");
            }

            ExpectContains(response, "42", "Gemini double-check response omitted the reminder id.");
            ExpectContains(response, "deleted", "Gemini double-check response omitted the deletion result.");
            ExpectContains(response, "GEMINI_DOUBLE_CHECK_OK", "Gemini double-check response did not finish with the expected marker.");
        });

        await RunTestAsync("advanced structured outputs", async () =>
        {
            var options = CreateGeminiOptions(ChatCompletionModels.Gemini.Gemini31FlashLitePreview);
            options.ResponseType = typeof(StructuredWeekendPlanResponse);

            var response = await client.CompleteAsync(
                "Return a structured weekend plan for Warsaw. Use itinerary_title \"Weekend in Warsaw\", travel_mode \"train\", optional_note null, and exactly two days. " +
                "Day 1 should have theme \"history\", estimated_budget_pln 120.5, and activities \"Old Town walk\" with tags [\"historic\",\"outdoor\"] and \"Royal Castle\" with tags [\"museum\",\"indoor\"]. " +
                "Day 2 should have theme \"river\", estimated_budget_pln 80.75, and activities \"Vistula Boulevards\" with tags [\"river\",\"outdoor\"] and \"Copernicus Science Centre\" with tags [\"science\",\"indoor\"]. " +
                "Set summary.total_budget_pln to 201.25 and summary.has_kid_option to true.",
                options,
                usageTracker);

            var result = JsonSerializer.Deserialize<StructuredWeekendPlanResponse>(response, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (result is null)
            {
                throw new InvalidOperationException("Gemini did not return valid advanced structured JSON.");
            }

            ExpectContains(result.ItineraryTitle, "Weekend in Warsaw", "Gemini advanced structured output returned the wrong itinerary title.");
            if (result.TravelMode != StructuredTravelMode.Train)
            {
                throw new InvalidOperationException($"Gemini advanced structured output returned the wrong travel mode: {result.TravelMode}.");
            }

            if (result.OptionalNote is not null)
            {
                throw new InvalidOperationException("Gemini advanced structured output should have returned optional_note as null.");
            }

            if (result.Days.Count != 2)
            {
                throw new InvalidOperationException($"Gemini advanced structured output returned {result.Days.Count} days instead of 2.");
            }

            ExpectContains(result.Days[0].Theme, "history", "Gemini advanced structured output returned the wrong theme for day 1.");
            ExpectContains(result.Days[1].Theme, "river", "Gemini advanced structured output returned the wrong theme for day 2.");
            ExpectContains(result.Days[0].Activities[0].Name + " " + result.Days[0].Activities[1].Name, "Old Town", "Gemini advanced structured output omitted Old Town walk.");
            ExpectContains(result.Days[1].Activities[0].Name + " " + result.Days[1].Activities[1].Name, "Copernicus", "Gemini advanced structured output omitted Copernicus Science Centre.");
            ExpectContains(string.Join(' ', result.Days[0].Activities[0].Tags), "historic", "Gemini advanced structured output omitted the historic tag.");
            ExpectContains(string.Join(' ', result.Days[1].Activities[0].Tags), "river", "Gemini advanced structured output omitted the river tag.");

            if (result.Summary.TotalBudgetPln != 201.25m)
            {
                throw new InvalidOperationException($"Gemini advanced structured output returned the wrong total budget: {result.Summary.TotalBudgetPln}.");
            }

            if (!result.Summary.HasKidOption)
            {
                throw new InvalidOperationException("Gemini advanced structured output returned the wrong has_kid_option flag.");
            }
        });
    }

    if (!runChatAndStructuredOnly)
    {
        await RunTestAsync("json mode", async () =>
        {
            var options = CreateGeminiOptions(ChatCompletionModels.Gemini.GeminiFlashLiteLatest);
            options.IsJsonMode = true;

            var response = await client.CompleteAsync("Return a JSON object with {\"status\":\"GEMINI_JSON_OK\"}.", options, usageTracker);
            using var document = JsonDocument.Parse(response);
            var status = document.RootElement.GetProperty("status").GetString();

            if (!string.Equals(status, "GEMINI_JSON_OK", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Gemini JSON mode did not return the expected status field.");
            }
        });

        await RunTestAsync("vision input", async () =>
        {
            var options = CreateGeminiOptions(ChatCompletionModels.Gemini.GeminiFlashLiteLatest);
            var chat = new Chat();
            chat.FromUser("What single color dominates this image? Reply with one word.", imageUrls: ["https://dummyimage.com/256x256/ff0000/ffffff.png"]);

            var response = await client.CompleteAsync(chat, options, usageTracker);
            ExpectContains(response, "red", "Gemini vision completion did not identify the dominant color.");
        });

        await RunTestAsync("embeddings", async () =>
        {
            var embedding = await client.GetEmbeddingAsync("Gemini embedding smoke test.", new ChatAIze.GenerativeCS.Options.Gemini.EmbeddingOptions
            {
                Dimensions = 16
            }, usageTracker);

            if (embedding.Length != 16)
            {
                throw new InvalidOperationException($"Gemini embedding request returned {embedding.Length} dimensions instead of 16.");
            }

            var base64Embedding = await client.GetBase64EmbeddingAsync("Gemini base64 embedding smoke test.", new ChatAIze.GenerativeCS.Options.Gemini.EmbeddingOptions
            {
                Dimensions = 16
            }, usageTracker);

            if (string.IsNullOrWhiteSpace(base64Embedding))
            {
                throw new InvalidOperationException("Gemini base64 embedding request returned an empty payload.");
            }

            var decodedEmbedding = Convert.FromBase64String(base64Embedding);
            if (decodedEmbedding.Length != 16 * sizeof(float))
            {
                throw new InvalidOperationException($"Gemini base64 embedding decoded to {decodedEmbedding.Length} bytes instead of {16 * sizeof(float)}.");
            }
        });

        await RunTestAsync("speech round-trip", async () =>
        {
            const string spanishText = "Hola mundo. El gato negro duerme en la silla.";
            var tempAudioPath = Path.Combine(Path.GetTempPath(), $"gemini-smoke-{Guid.NewGuid():N}.wav");

            try
            {
                var audio = await client.SynthesizeSpeechAsync(spanishText, new ChatAIze.GenerativeCS.Options.Gemini.TextToSpeechOptions
                {
                    VoiceName = GeminiTextToSpeechVoices.Puck,
                    ResponseFormat = VoiceResponseFormat.Default
                });

                if (audio.Length < 4)
                {
                    throw new InvalidOperationException("Gemini text-to-speech returned an unexpectedly short audio payload.");
                }

                var riffHeader = Encoding.ASCII.GetString(audio, 0, 4);
                if (!string.Equals(riffHeader, "RIFF", StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("Gemini text-to-speech did not return WAV-wrapped audio.");
                }

                await File.WriteAllBytesAsync(tempAudioPath, audio);

                var transcript = await client.TranscriptAsync(tempAudioPath, new ChatAIze.GenerativeCS.Options.Gemini.TranscriptionOptions(language: "es")
                {
                    Model = ChatCompletionModels.Gemini.Gemini31FlashLitePreview,
                    ResponseFormat = TranscriptionResponseFormat.Text
                });

                ExpectContains(transcript, "gato", "Gemini transcription did not preserve the expected Spanish content.");

                var transcriptJson = await client.TranscriptAsync(tempAudioPath, new ChatAIze.GenerativeCS.Options.Gemini.TranscriptionOptions(language: "es")
                {
                    Model = ChatCompletionModels.Gemini.Gemini31FlashLitePreview,
                    ResponseFormat = TranscriptionResponseFormat.Json
                });

                using (var transcriptDocument = JsonDocument.Parse(transcriptJson))
                {
                    var transcriptText = transcriptDocument.RootElement.GetProperty("text").GetString() ?? string.Empty;
                    ExpectContains(transcriptText, "gato", "Gemini JSON transcription did not include the expected text payload.");
                }

                var transcriptVerboseJson = await client.TranscriptAsync(tempAudioPath, new ChatAIze.GenerativeCS.Options.Gemini.TranscriptionOptions(language: "es")
                {
                    Model = ChatCompletionModels.Gemini.Gemini31FlashLitePreview,
                    ResponseFormat = TranscriptionResponseFormat.VerboseJson
                });

                using (var verboseTranscriptDocument = JsonDocument.Parse(transcriptVerboseJson))
                {
                    var transcriptText = verboseTranscriptDocument.RootElement.GetProperty("text").GetString() ?? string.Empty;
                    ExpectContains(transcriptText, "gato", "Gemini verbose JSON transcription did not include the expected text payload.");
                    if (!verboseTranscriptDocument.RootElement.TryGetProperty("segments", out _))
                    {
                        throw new InvalidOperationException("Gemini verbose JSON transcription did not include the expected segments property.");
                    }
                }

                var translation = await client.TranslateAsync(tempAudioPath, new ChatAIze.GenerativeCS.Options.Gemini.TranslationOptions
                {
                    Model = ChatCompletionModels.Gemini.Gemini31FlashLitePreview,
                    Prompt = "The spoken language is Spanish. Translate the spoken content into English only. Do not repeat the Spanish transcript.",
                    ResponseFormat = TranscriptionResponseFormat.Text
                });

                ExpectContains(translation, "cat", "Gemini translation did not produce the expected English content.");

                var translationJson = await client.TranslateAsync(tempAudioPath, new ChatAIze.GenerativeCS.Options.Gemini.TranslationOptions
                {
                    Model = ChatCompletionModels.Gemini.Gemini31FlashLitePreview,
                    Prompt = "The spoken language is Spanish. Translate the spoken content into English only. Do not repeat the Spanish transcript.",
                    ResponseFormat = TranscriptionResponseFormat.Json
                });

                using (var translationDocument = JsonDocument.Parse(translationJson))
                {
                    var translationText = translationDocument.RootElement.GetProperty("text").GetString() ?? string.Empty;
                    ExpectContains(translationText, "cat", "Gemini JSON translation did not include the expected translated text.");
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
            services.AddGeminiClient(configure =>
            {
                configure.ApiKey = client.ApiKey;
                configure.DefaultCompletionOptions = CreateGeminiOptions(ChatCompletionModels.Gemini.GeminiFlashLiteLatest);
            });

            using var serviceProvider = services.BuildServiceProvider();
            var resolvedClient = serviceProvider.GetRequiredService<GeminiClient>();
            var response = await resolvedClient.CompleteAsync("Reply with exactly GEMINI_DI_OK.");

            ExpectContains(response, "GEMINI_DI_OK", "The DI-registered Gemini client did not complete successfully.");
        });
    }

    Console.WriteLine();
    Console.WriteLine($"{suiteName} passed. Prompt tokens: {usageTracker.PromptTokens}, cached tokens: {usageTracker.CachedTokens}, completion tokens: {usageTracker.CompletionTokens}");
}

static async Task RunGeminiLocalRegressionTestsAsync()
{
    Console.WriteLine("Gemini local regression tests starting...");

    await RunTestAsync("double-check function execution (local)", async () =>
    {
        var handler = new SequenceHttpMessageHandler(new Func<RecordedHttpRequest, int, HttpResponseMessage>[]
        {
            (request, _) =>
            {
                ExpectContains(request.Content, "\"tools\"", "The first local Gemini request did not include tool declarations.");
                return CreateGeminiJsonResponse(CreateGeminiFunctionCallCandidate("delete_reminder", new JsonObject
                {
                    ["reminder_id"] = "42"
                }));
            },
            (request, _) =>
            {
                ExpectContains(request.Content, "Before executing, are you sure the user wants to run this function?", "The second local Gemini request did not include the double-check confirmation prompt.");
                return CreateGeminiJsonResponse(CreateGeminiFunctionCallCandidate("delete_reminder", new JsonObject
                {
                    ["reminder_id"] = "42"
                }));
            },
            (request, _) =>
            {
                ExpectContains(request.Content, "Reminder 42 deleted.", "The final local Gemini request did not include the function result.");
                return CreateGeminiJsonResponse(CreateGeminiTextCandidate("Reminder 42 deleted. GEMINI_LOCAL_DOUBLE_CHECK_OK"));
            }
        });

        using var httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromMinutes(1)
        };

        var client = CreateGeminiOfflineClient(httpClient);
        var options = CreateGeminiOptions(ChatCompletionModels.Gemini.Gemini20Flash);
        var callbackInvocations = 0;
        options.AddFunction("DeleteReminder", "Deletes a reminder by id.", true, new FunctionParameter(typeof(string), "reminder_id", "Reminder id."));
        options.DefaultFunctionCallback = (name, arguments, _) =>
        {
            callbackInvocations++;
            if (!name.Equals("DeleteReminder", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Unexpected local double-check callback name: {name}.");
            }

            ExpectContains(arguments, "42", "The local double-check callback received the wrong arguments.");
            return ValueTask.FromResult<object?>("Reminder 42 deleted.");
        };

        var response = await client.CompleteAsync(
            "Delete reminder 42. If the tool asks for confirmation, confirm and let it run again. End with GEMINI_LOCAL_DOUBLE_CHECK_OK.",
            options);

        if (callbackInvocations != 1)
        {
            throw new InvalidOperationException($"The local Gemini double-check callback ran {callbackInvocations} times instead of once.");
        }

        if (handler.Requests.Count != 3)
        {
            throw new InvalidOperationException($"The local Gemini double-check flow made {handler.Requests.Count} requests instead of 3.");
        }

        ExpectContains(response, "GEMINI_LOCAL_DOUBLE_CHECK_OK", "The local Gemini double-check flow did not return the expected marker.");
    });

    await RunTestAsync("streaming duplicate tool call deduplication (local)", async () =>
    {
        var handler = new SequenceHttpMessageHandler(new Func<RecordedHttpRequest, int, HttpResponseMessage>[]
        {
            (_, _) => CreateGeminiSseResponse(
                CreateGeminiFunctionCallCandidate("lookup_river_length", new JsonObject
                {
                    ["river"] = "Vistula"
                }),
                CreateGeminiFunctionCallCandidate("lookup_river_length", new JsonObject
                {
                    ["river"] = "Vistula"
                }, finishReason: "STOP")),
            (request, _) =>
            {
                ExpectContains(request.Content, "1047 kilometers long", "The local Gemini streaming follow-up request did not include the function result.");
                return CreateGeminiSseResponse(CreateGeminiTextCandidate("The Vistula river is 1047 kilometers long. GEMINI_LOCAL_STREAM_TOOL_OK", finishReason: "STOP"));
            }
        });

        using var httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromMinutes(1)
        };

        var client = CreateGeminiOfflineClient(httpClient);
        var options = CreateGeminiOptions(ChatCompletionModels.Gemini.Gemini20Flash);
        var callbackInvocations = 0;
        options.AddFunction("LookupRiverLength", "Returns the length of a river in kilometers.", new FunctionParameter(typeof(string), "river", "River name."));
        options.DefaultFunctionCallback = (name, arguments, _) =>
        {
            callbackInvocations++;
            if (!name.Equals("LookupRiverLength", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Unexpected local streaming callback name: {name}.");
            }

            ExpectContains(arguments, "Vistula", "The local streaming callback received the wrong arguments.");
            return ValueTask.FromResult<object?>("The Vistula river is 1047 kilometers long.");
        };

        var streamedBuilder = new StringBuilder();
        await foreach (var chunk in client.StreamCompletionAsync(
            "Use LookupRiverLength for the Vistula and end with GEMINI_LOCAL_STREAM_TOOL_OK.",
            options))
        {
            _ = streamedBuilder.Append(chunk);
        }

        if (callbackInvocations != 1)
        {
            throw new InvalidOperationException($"The local Gemini streaming callback ran {callbackInvocations} times instead of once.");
        }

        if (handler.Requests.Count != 2)
        {
            throw new InvalidOperationException($"The local Gemini streaming flow made {handler.Requests.Count} requests instead of 2.");
        }

        var response = streamedBuilder.ToString();
        ExpectContainsNormalizedNumber(response, "1047", "The local Gemini streaming flow omitted the normalized river length.");
        ExpectContains(response, "GEMINI_LOCAL_STREAM_TOOL_OK", "The local Gemini streaming flow did not return the expected marker.");
    });

    await RunTestAsync("ignore previous function calls request shaping (local)", async () =>
    {
        var handler = new SequenceHttpMessageHandler(new Func<RecordedHttpRequest, int, HttpResponseMessage>[]
        {
            (request, _) =>
            {
                ExpectContains(request.Content, "Earlier tool result should be ignored.", "The local Gemini request did not include the latest user message.");
                if (request.Content.Contains("legacy_lookup_weather", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("The local Gemini request still contained earlier function call history even though IsIgnoringPreviousFunctionCalls was enabled.");
                }

                return CreateGeminiJsonResponse(CreateGeminiTextCandidate("GEMINI_LOCAL_IGNORE_TOOLS_OK"));
            }
        });

        using var httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromMinutes(1)
        };

        var client = CreateGeminiOfflineClient(httpClient);
        var options = CreateGeminiOptions(ChatCompletionModels.Gemini.Gemini20Flash);
        options.IsIgnoringPreviousFunctionCalls = true;

        var chat = new Chat();
        chat.FromUser("Look up Warsaw weather.");
        chat.FromChatbot(new FunctionCall
        {
            ToolCallId = "legacy-call",
            Name = "legacy_lookup_weather",
            Arguments = "{\"city\":\"Warsaw\"}"
        });
        chat.FromFunction(new FunctionResult
        {
            ToolCallId = "legacy-call",
            Name = "legacy_lookup_weather",
            Value = "Sunny and 21 C."
        });
        chat.FromUser("Earlier tool result should be ignored. Reply with GEMINI_LOCAL_IGNORE_TOOLS_OK.");

        var response = await client.CompleteAsync(chat, options);
        ExpectContains(response, "GEMINI_LOCAL_IGNORE_TOOLS_OK", "The local Gemini ignore-tools flow did not return the expected marker.");
    });

    await RunTestAsync("structured follow-up request shaping (local)", async () =>
    {
        var handler = new SequenceHttpMessageHandler(new Func<RecordedHttpRequest, int, HttpResponseMessage>[]
        {
            (_, _) => CreateGeminiJsonResponse(CreateGeminiFunctionCallCandidate("get_city_temperature", new JsonObject
            {
                ["city"] = "Warsaw"
            })),
            (request, _) =>
            {
                ExpectContains(request.Content, "\"response_json_schema\"", "The local Gemini structured follow-up request did not include response_json_schema.");
                ExpectContains(request.Content, "temperature_c", "The local Gemini structured follow-up request omitted the structured schema fields.");
                if (request.Content.Contains("\"tools\"", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("The local Gemini structured follow-up request still included tools after the tool result was available.");
                }

                return CreateGeminiJsonResponse(CreateGeminiTextCandidate("{\"readings\":[{\"city\":\"Warsaw\",\"temperature_c\":21}],\"warmest_city\":\"Warsaw\",\"difference_c\":0}"));
            }
        });

        using var httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromMinutes(1)
        };

        var client = CreateGeminiOfflineClient(httpClient);
        var options = CreateGeminiOptions(ChatCompletionModels.Gemini.Gemini20Flash);
        options.ResponseType = typeof(StructuredWeatherSummaryResponse);
        options.AddFunction("GetCityTemperature", "Returns the temperature for a city.", new FunctionParameter(typeof(string), "city", "City name."));
        options.DefaultFunctionCallback = (_, arguments, _) =>
        {
            ExpectContains(arguments, "Warsaw", "The local Gemini structured follow-up callback received the wrong arguments.");
            return ValueTask.FromResult<object?>("{\"city\":\"Warsaw\",\"temperature_c\":21}");
        };

        var response = await client.CompleteAsync("Call GetCityTemperature for Warsaw, then return structured JSON.", options);
        var result = JsonSerializer.Deserialize<StructuredWeatherSummaryResponse>(response, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (result is null)
        {
            throw new InvalidOperationException("The local Gemini structured follow-up flow returned invalid JSON.");
        }

        ExpectContains(result.WarmestCity, "Warsaw", "The local Gemini structured follow-up flow returned the wrong warmest city.");
    });

    Console.WriteLine();
    Console.WriteLine("Gemini local regression tests passed.");
}

static ChatAIze.GenerativeCS.Options.Gemini.ChatCompletionOptions CreateGeminiOptions(string model = ChatCompletionModels.Gemini.GeminiFlashLiteLatest)
{
    return new ChatAIze.GenerativeCS.Options.Gemini.ChatCompletionOptions
    {
        // The preview suite defaults to Gemini Flash Lite Latest to spread live checks across models with available quota.
        // The library default remains Gemini 2.5 Flash.
        Model = model,
        MaxAttempts = 3,
        MaxOutputTokens = 512,
        Temperature = 0,
        TopP = 1,
        ReasoningEffort = ReasoningEffort.None,
        IsStrictFunctionCallingOn = true,
        UserTrackingId = "preview-gemini-smoke"
    };
}

static GeminiClient CreateGeminiOfflineClient(HttpClient httpClient)
{
    return new GeminiClient(httpClient, Options.Create(new ChatAIze.GenerativeCS.Options.Gemini.GeminiClientOptions
    {
        ApiKey = "offline-gemini-key"
    }));
}

static HttpResponseMessage CreateGeminiJsonResponse(JsonObject payload)
{
    var response = new HttpResponseMessage(HttpStatusCode.OK)
    {
        Content = new StringContent(payload.ToJsonString(), Encoding.UTF8)
    };
    response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
    return response;
}

static HttpResponseMessage CreateGeminiSseResponse(params JsonObject[] payloads)
{
    var builder = new StringBuilder();
    foreach (var payload in payloads)
    {
        _ = builder.Append("data: ").Append(payload.ToJsonString()).Append("\n\n");
    }

    _ = builder.Append("data: [DONE]\n\n");

    var response = new HttpResponseMessage(HttpStatusCode.OK)
    {
        Content = new StringContent(builder.ToString(), Encoding.UTF8)
    };
    response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/event-stream");
    return response;
}

static JsonObject CreateGeminiTextCandidate(string text, string finishReason = "STOP")
{
    return new JsonObject
    {
        ["candidates"] = new JsonArray
        {
            new JsonObject
            {
                ["content"] = new JsonObject
                {
                    ["parts"] = new JsonArray
                    {
                        new JsonObject
                        {
                            ["text"] = text
                        }
                    }
                },
                ["finishReason"] = finishReason
            }
        },
        ["usageMetadata"] = new JsonObject
        {
            ["promptTokenCount"] = 1,
            ["candidatesTokenCount"] = 1
        }
    };
}

static JsonObject CreateGeminiFunctionCallCandidate(string name, JsonObject args, string finishReason = "STOP")
{
    return new JsonObject
    {
        ["candidates"] = new JsonArray
        {
            new JsonObject
            {
                ["content"] = new JsonObject
                {
                    ["parts"] = new JsonArray
                    {
                        new JsonObject
                        {
                            ["functionCall"] = new JsonObject
                            {
                                ["name"] = name,
                                ["args"] = args
                            }
                        }
                    }
                },
                ["finishReason"] = finishReason
            }
        },
        ["usageMetadata"] = new JsonObject
        {
            ["promptTokenCount"] = 1,
            ["candidatesTokenCount"] = 1
        }
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

file sealed class SequenceHttpMessageHandler(IReadOnlyList<Func<RecordedHttpRequest, int, HttpResponseMessage>> responders) : HttpMessageHandler
{
    public List<RecordedHttpRequest> Requests { get; } = [];

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var requestContent = request.Content is null
            ? string.Empty
            : await request.Content.ReadAsStringAsync(cancellationToken);

        var recordedRequest = new RecordedHttpRequest
        {
            Method = request.Method.Method,
            RequestUri = request.RequestUri?.ToString() ?? string.Empty,
            Content = requestContent
        };

        Requests.Add(recordedRequest);
        var requestIndex = Requests.Count - 1;
        if (requestIndex >= responders.Count)
        {
            throw new InvalidOperationException($"No offline Gemini responder was configured for request #{requestIndex + 1}.");
        }

        return responders[requestIndex](recordedRequest, requestIndex);
    }
}

file sealed class RecordedHttpRequest
{
    public string Method { get; init; } = string.Empty;

    public string RequestUri { get; init; } = string.Empty;

    public string Content { get; init; } = string.Empty;
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

[JsonConverter(typeof(JsonStringEnumConverter<StructuredTravelMode>))]
file enum StructuredTravelMode
{
    Train,
    Car
}

file sealed class StructuredWeekendPlanResponse
{
    [JsonPropertyName("itinerary_title")]
    public string ItineraryTitle { get; set; } = string.Empty;

    [JsonPropertyName("travel_mode")]
    public StructuredTravelMode TravelMode { get; set; }

    [JsonPropertyName("optional_note")]
    public string? OptionalNote { get; set; }

    [JsonPropertyName("days")]
    public List<StructuredWeekendPlanDayResponse> Days { get; set; } = [];

    [JsonPropertyName("summary")]
    public StructuredWeekendPlanSummaryResponse Summary { get; set; } = new();
}

file sealed class StructuredWeekendPlanDayResponse
{
    [JsonPropertyName("theme")]
    public string Theme { get; set; } = string.Empty;

    [JsonPropertyName("estimated_budget_pln")]
    public decimal EstimatedBudgetPln { get; set; }

    [JsonPropertyName("activities")]
    public List<StructuredWeekendPlanActivityResponse> Activities { get; set; } = [];
}

file sealed class StructuredWeekendPlanActivityResponse
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = [];
}

file sealed class StructuredWeekendPlanSummaryResponse
{
    [JsonPropertyName("total_budget_pln")]
    public decimal TotalBudgetPln { get; set; }

    [JsonPropertyName("has_kid_option")]
    public bool HasKidOption { get; set; }
}
