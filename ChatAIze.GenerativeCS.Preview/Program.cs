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
var shouldRunOpenAISmoke = argsList.Any(arg => arg.Equals("openai-smoke", StringComparison.OrdinalIgnoreCase));
var shouldRunInteractiveOpenAI = argsList.Any(arg => arg.Equals("openai-chat", StringComparison.OrdinalIgnoreCase));

if (shouldRunClaudeSmoke)
{
    await RunClaudeSmokeTestsAsync();
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

file sealed class StructuredCityResponse
{
    [JsonPropertyName("city")]
    public string City { get; set; } = string.Empty;

    [JsonPropertyName("country")]
    public string Country { get; set; } = string.Empty;

    [JsonPropertyName("continent")]
    public string Continent { get; set; } = string.Empty;
}
