# GenerativeCS
Generative AI library for .NET 8.0 with built-in OpenAI ChatGPT and Google Gemini API clients and support for C# function calling via reflection.

## Installation
### .NET CLI
```bash
dotnet add package ChatAIze.GenerativeCS
```
### Package Manager Console
```powershell
Install-Package ChatAIze.GenerativeCS
```

## Clients
### Single Instance
```cs
using ChatAIze.GenerativeCS.Clients;

var openAIClient = new OpenAIClient("<OPENAI API KEY>");
var geminiClient = new GeminiClient("<GEMINI API KEY>");
```
### Dependency Injection
```cs
using ChatAIze.GenerativeCS.Extensions;

builder.Services.AddOpenAIClient("<OPENAI API KEY>");
builder.Services.AddGeminiClient("<GEMINI API KEY>");
```

## Chat Completion
### Simple Prompt
```cs
using ChatAIze.GenerativeCS.Clients;

var client = new OpenAIClient("<OPENAI API KEY>");
string response = await client.CompleteAsync("Write an article about Bitcoin.");

Console.WriteLine(response);
```
### Streamed Prompt
```cs
using ChatAIze.GenerativeCS.Clients;

var client = new OpenAIClient("<OPENAI API KEY>");
await foreach (string chunk in client.StreamCompletionAsync("Write an article about Bitcoin."))
{
    Console.Write(chunk);
}
```
### Conversation
```cs
using ChatAIze.GenerativeCS.Clients;
using ChatAIze.GenerativeCS.Models;

var client = new OpenAIClient("<OPENAI API KEY>");
var conversation = new ChatConversation();

while (true)
{
    string message = Console.ReadLine()!;
    conversation.FromUser(message);

    string response = await client.CompleteAsync(conversation);
    Console.WriteLine(response);
}   
```
### Streamed Conversation
```cs
using ChatAIze.GenerativeCS.Clients;
using ChatAIze.GenerativeCS.Models;

var client = new OpenAIClient("<OPENAI API KEY>");
var conversation = new ChatConversation();

while (true)
{
    string message = Console.ReadLine()!;
    conversation.FromUser(message);

    await foreach (string chunk in client.StreamCompletionAsync(conversation))
    {
        Console.Write(chunk);
    }
}
```
> [!NOTE]
> Assistant responses, function calls, and function results are automatically added to the conversation.
## Embeddings
```cs
using ChatAIze.GenerativeCS.Clients;

var client = new OpenAIClient("<OPENAI API KEY>");
float[] vectorEmbedding = await client.GetEmbeddingAsync("The quick brown fox jumps over the lazy dog");
string base64Embedding = await client.GetBase64EmbeddingAsync("The quick brown fox jumps over the lazy dog");
```
## Audio
### Text-to-Speech
#### Synthesize to File
```cs
var client = new OpenAIClient("<OPENAI API KEY>");
await client.SynthesizeSpeechAsync("The quick brown fox jumps over the lazy dog", "speech.mp3");
```
#### Synthesize to Byte Array
```cs
using ChatAIze.GenerativeCS.Clients;

var client = new OpenAIClient("<OPENAI API KEY>");
byte[] speech = await client.SynthesizeSpeechAsync("The quick brown fox jumps over the lazy dog");
```
### Speech-to-Text
#### Transcript From File
```cs
using ChatAIze.GenerativeCS.Clients;

var client = new OpenAIClient("<OPENAI API KEY>");
string transcript = await client.TranscriptAsync("speech.mp3");
```
#### Transcript From Byte Array
```cs
using ChatAIze.GenerativeCS.Clients;

var client = new OpenAIClient("<OPENAI API KEY>");
byte[] audio = await File.ReadAllBytesAsync("speech.mp3");
string transcript = await client.TranscriptAsync(audio);
```
#### Translate From File
```cs
using ChatAIze.GenerativeCS.Clients;

var client = new OpenAIClient("<OPENAI API KEY>");
string translation = await client.TranslateAsync("speech.mp3");
```
#### Translate From Byte Array
```cs
using ChatAIze.GenerativeCS.Clients;

var client = new OpenAIClient("<OPENAI API KEY>");
byte[] audio = await File.ReadAllBytesAsync("speech.mp3");
string translation = await client.TranslateAsync(audio);
```

## Moderation
```cs
using ChatAIze.GenerativeCS.Clients;

var client = new OpenAIClient("<OPENAI API KEY>");
var result = await client.ModerateAsync("I am going going to blow up your house in Minecraft.");

Console.WriteLine(result.IsFlagged); //true
Console.WriteLine(result.IsViolence); //true 
Console.WriteLine(result.ViolenceScore); // 0,908397912979126
```

## Options
> [!NOTE]
> Per-request options take precedence over default client options.

> [!TIP]
> If you use **OpenAI** client add:
> ```cs
> using ChatAIze.GenerativeCS.Options.OpenAI;
> ```
> If you use **Gemini** client add:
> ```cs
> using ChatAIze.GenerativeCS.Options.Gemini;
> ```
### Dependency Injection
#### OpenAI Client
```cs
using ChatAIze.GenerativeCS.Constants;
using ChatAIze.GenerativeCS.Extensions;

builder.Services.AddOpenAIClient(configure =>
{
    configure.ApiKey = "<OPENAI API KEY>";
    configure.DefaultCompletionOptions = new ChatCompletionOptions()
    {
        Model = ChatCompletionModels.GPT_3_5_TURBO_1106,
        Temperature = 1.0
        // set other chat completion options here
    };
    configure.DefaultEmbeddingOptions = new EmbeddingOptions()
    {
        Model = EmbeddingModels.TEXT_EMBEDDING_ADA_002,
        MaxAttempts = 5
        // set other embeding options here
    };
    // set other options here
});
```
#### Gemini Client
```cs
using ChatAIze.GenerativeCS.Constants;
using ChatAIze.GenerativeCS.Extensions;

builder.Services.AddGeminiClient(configure =>
{
    configure.ApiKey = "<GEMINI API KEY>";
    configure.DefaultCompletionOptions = new ChatCompletionOptions()
    {
        Model = ChatCompletionModels.GEMINI_PRO,
        MessageLimit = 10
        // set other chat completion options here
    };
    // set other options here
});
```
### Chat Completion
#### OpenAI Client
```cs
using ChatAIze.GenerativeCS.Clients;
using ChatAIze.GenerativeCS.Constants;
using ChatAIze.GenerativeCS.Models;
using ChatAIze.GenerativeCS.Options.OpenAI;

var options = new ChatCompletionOptions
{
    Model = ChatCompletionModels.GPT_3_5_TURBO_1106,
    User = "USER_ID_1234",
    MaxAttempts = 5,
    MaxOutputTokens = 2000,
    MessageLimit = 10,
    CharacterLimit = 20000,
    Seed = 1234,
    Temperature = 1.0,
    TopP = 1,
    FrequencyPenalty = 0.0,
    PresencePenalty = 0.0,
    IsJsonMode = false,
    IsTimeAware = true,
    StopWords = ["11.", "end"],
    Functions = [new ChatFunction("ToggleDarkMode")],
    DefaultFunctionCallback = async (name, arguments, cancellationToken) =>
    {
        await Console.Out.WriteLineAsync($"Function {name} called with arguments {arguments}");
        return new { Success = true, Property1 = "ABC", Property2 = 123 };
    },
    AddMessageCallback = async (message) =>
    {
        // Called every time a new message is added, including function calls and results:
        await Console.Out.WriteLineAsync($"Message added: {message}");
    },
    TimeCallback = () => DateTime.Now
};

// Set for entire client:
var client = new OpenAIClient("<OPENAI API KEY>", options); // via constructor
client.DefaultCompletionOptions = options; // via property

// Set for single request:
string response = await client.CompleteAsync(prompt, options);
string response = await client.CompleteAsync(conversation, options);
```
#### Gemini Client
```cs
using ChatAIze.GenerativeCS.Clients;
using ChatAIze.GenerativeCS.Constants;
using ChatAIze.GenerativeCS.Models;
using ChatAIze.GenerativeCS.Options.Gemini;

var options = new ChatCompletionOptions
{
    Model = ChatCompletionModels.GPT_3_5_TURBO_1106,
    MaxAttempts = 5,
    MessageLimit = 10,
    CharacterLimit = 20000,
    IsTimeAware = true,
    Functions = [new ChatFunction("ToggleDarkMode")],
    DefaultFunctionCallback = async (name, arguments, cancellationToken) =>
    {
        await Console.Out.WriteLineAsync($"Function {name} called with arguments {arguments}");
        return new { Success = true, Property1 = "ABC", Property2 = 123 };
    },
    TimeCallback = () => DateTime.Now
};

// Set for entire client:
var client = new GeminiClient("<GEMINI API KEY>", options); // via constructor
client.DefaultCompletionOptions = options; // via property

// Set for single request:
string response = await client.CompleteAsync(prompt, options);
string response = await client.CompleteAsync(conversation, options);
```
### Embeddings
```cs
using ChatAIze.GenerativeCS.Clients;
using ChatAIze.GenerativeCS.Constants;
using ChatAIze.GenerativeCS.Options.OpenAI;

var options = new EmbeddingOptions
{
    Model = EmbeddingModels.TEXT_EMBEDDING_ADA_002,
    User = "USER_ID_1234",
    MaxAttempts = 5
};

// Set for entire client:
var client = new OpenAIClient("<OPENAI API KEY>", options); // via constructor
client.DefaultEmbeddingOptions = options; // via property

// Set for single request:
float[] embedding = await client.GetEmbeddingAsync("The quick brown fox jumps over the lazy dog", options);
```
### Audio
#### Text-to-Speech
```cs
using ChatAIze.GenerativeCS.Clients;
using ChatAIze.GenerativeCS.Constants;
using ChatAIze.GenerativeCS.Enums;
using ChatAIze.GenerativeCS.Options.OpenAI;

var options = new TextToSpeechOptions
{
    Model = TextToSpeechModels.TTS_1,
    Voice = TextToSpeechVoice.Alloy,
    Speed = 1.0,
    MaxAttempts = 5,
    ResponseFormat = VoiceResponseFormat.MP3
};

// Set for entire client:
var client = new OpenAIClient("<OPENAI API KEY>", options); // via constructor
client.DefaultTextToSpeechOptions = options; // via property

// Set for single request:
await client.SynthesizeSpeechAsync("The quick brown fox jumps over the lazy dog", "speech.mp3", options);
```
#### Transcription
```cs
using ChatAIze.GenerativeCS.Clients;
using ChatAIze.GenerativeCS.Constants;
using ChatAIze.GenerativeCS.Enums;
using ChatAIze.GenerativeCS.Options.OpenAI;

var options = new TranscriptionOptions
{
    Model = SpeechRecognitionModels.WHISPER_1,
    Language = "en",
    Prompt = "ZyntriQix, Digique Plus, CynapseFive, VortiQore V8, EchoNix Array, ...",
    Temperature = 0.0,
    MaxAttempts = 5,
    ResponseFormat = TranscriptionResponseFormat.Text
};

// Set for entire client:
var client = new OpenAIClient("<OPENAI API KEY>", options); // via constructor
client.DefaultTranscriptionOptions = options; // via property

// Set for single request:
string transcript = await client.TranscriptAsync("speech.mp3", options);
```
#### Translation
```cs
using ChatAIze.GenerativeCS.Clients;
using ChatAIze.GenerativeCS.Constants;
using ChatAIze.GenerativeCS.Enums;
using ChatAIze.GenerativeCS.Options.OpenAI;

var options = new TranslationOptions
{
    Model = SpeechRecognitionModels.WHISPER_1,
    Prompt = "ZyntriQix, Digique Plus, CynapseFive, VortiQore V8, EchoNix Array, ...",
    Temperature = 0.0,
    MaxAttempts = 5,
    ResponseFormat = TranscriptionResponseFormat.Text
};

// Set for entire client:
var client = new OpenAIClient("<OPENAI API KEY>", options); // via constructor
client.DefaultTranslationOptions = options; // via property

// Set for single request:
string translation = await client.TranslateAsync("speech.mp3", options);
```
#### Moderation
```cs
using ChatAIze.GenerativeCS.Clients;
using ChatAIze.GenerativeCS.Constants;
using ChatAIze.GenerativeCS.Options.OpenAI;

var options = new ModerationOptions
{
    Model = ModerationModels.TEXT_MODERATION_LATEST,
    MaxAttempts = 5
};

// Set for entire client:
var client = new OpenAIClient("<OPENAI API KEY>", options); // via constructor
client.DefaultModerationOptions = options; // via property

// Set for single request:
var result = await client.ModerateAsync("I am going going to blow up your house in Minecraft.", options);
```

## Function Calling
### Top-Level Methods
```cs
using ChatAIze.GenerativeCS.Options.OpenAI;
// or
using ChatAIze.GenerativeCS.Options.Gemini;

void ToggleDarkMode(bool isOn)
{
    Console.WriteLine($"Dark mode set to: {isOn}");
}

string GetCurrentWeather(string location)
{
    return $"The weather in {location} is 72 degrees and sunny.";
}

async Task<object> SendEmailAsync(string recipient, string subject, string body)
{
    await Task.Delay(3000);
    return new { Success = true, Property1 = "ABC", Property2 = 123 };
}

var options = new ChatCompletionOptions();

options.AddFunction(ToggleDarkMode);
options.AddFunction(GetCurrentWeather);
options.AddFunction(SendEmailAsync);
```
### Static Class Methods
```cs
using System.ComponentModel;
using ChatAIze.GenerativeCS.Options.OpenAI;
// or
using ChatAIze.GenerativeCS.Options.Gemini;

var options = new ChatCompletionOptions();

options.AddFunction(SmartHome.CheckFrontCamera);
options.AddFunction(SmartHome.SetFrontDoorLockAsync);
options.AddFunction(SmartHome.SetTemperature);

public static class SmartHome
{
    [Description("Checks if there is someone waiting at the front door.")]
    public static object CheckFrontCamera()
    {
        return new { Success = true, IsPersonDetected = true };
    }

    public static async Task SetFrontDoorLockAsync(bool isLocked)
    {
        await Task.Delay(3000);
        Console.WriteLine($"Front door locked: {isLocked}");
    }

    public static void SetTemperature(string room, int temperature)
    {
        Console.WriteLine($"Temperature in {room} has been set to {temperature} degrees.");
    }
}
```
### Class Instance Methods
```cs
using ChatAIze.GenerativeCS.Options.OpenAI;
// or
using ChatAIze.GenerativeCS.Options.Gemini;

var options = new ChatCompletionOptions();
var product = new Product();

options.AddFunction(product.GetDescription);
options.AddFunction(product.Rename);
options.AddFunction(product.Delete);

public class Product
{
    public string? Name { get; set; }

    public string GetDescription()
    {
        return $"This is a {Name}";
    }

    public void Rename(string name)
    {
        Name = name;
    }

    public void Delete()
    {
        Console.WriteLine($"Deleting product: {Name}");
    }
}
```
### Anonymous Functions
```cs
using ChatAIze.GenerativeCS.Options.OpenAI;
// or
using ChatAIze.GenerativeCS.Options.Gemini;

var options = new ChatCompletionOptions();

options.AddFunction("GetCurrentWeather", (string location) => 
{
    return "The current weather is sunny";
});

options.AddFunction("GetCurrentWeather", async () =>
{
    await Task.Delay(3000);
    return "The current weather is sunny";
});

options.AddFunction("GetCurrentWeather", "Gets the current weathe in default location.", async () =>
{
    await Task.Delay(3000);
    return new WeatherData(20, 50);
});

public record WeatherData(int Temperature, int Humidity);
```
### Default Function Callback
```cs
using ChatAIze.GenerativeCS.Models;
using ChatAIze.GenerativeCS.Options.OpenAI;
// or
using ChatAIze.GenerativeCS.Options.Gemini;

var options = new ChatCompletionOptions();

options.AddFunction("GetUserLocation");
options.AddFunction("GetCurrentWeather", new FunctionParameter(typeof(string), "location"));

List<FunctionParameter> parameters = [new(typeof(string), "room"), new(typeof(int), "temperature")];
options.AddFunction("SetRoomTemperature", parameters);

options.DefaultFunctionCallback = async (name, parameters, cancellationToken) =>
{
    if (name == "GetUserLocation")
    {
        return "London";
    }

    if (name == "GetCurrentWeather")
    {
        return new { Temperature = 20, Weather = "Sunny" };
    }

    if (name == "SetRoomTemperature")
    {
        await Task.Delay(3000, cancellationToken);
        return new { IsSuccess = true };
    }

    return new { Error = $"Unknown function: {name}" };
};
```

## Additional Features
### Time Awareness
You can configure both Gemini and OpenAI clients to be aware of the current date and time.
```cs
using ChatAIze.GenerativeCS.Options.OpenAI;
// or
using ChatAIze.GenerativeCS.Options.Gemini;

var options = new ChatCompletionOptions
{
    IsTimeAware = true,
    // other completion options
};
```
By default, GenerativeCS uses `DateTime.Now`, but you can change the source of current time by specifying custom `TimeCallback`
```cs
using ChatAIze.GenerativeCS.Options.OpenAI;
// or
using ChatAIze.GenerativeCS.Options.Gemini;

var options = new ChatCompletionOptions
{
    IsTimeAware = true,
    TimeCallback = () => new DateTime(2024, 1, 14),
};
```
### Limits
#### Message Limit
The maximum number of messages sent in a single chat completion request. The oldest messages will be removed one by one until the limit is satisfied.
- Pinned messages count toward the limit and have priority but are never truncated.
- The limit does include function calls and results. 
- Function definitions are not considered messages.
```cs
using ChatAIze.GenerativeCS.Options.OpenAI;
// or
using ChatAIze.GenerativeCS.Options.Gemini;

var options = new ChatCompletionOptions
{
    MessageLimit = 10,
};
```
#### Character Limit
The maximum number of characters sent in a single chat completion request. The oldest messages will be removed one by one until the limit is satisfied.
- Pinned messages count toward the limit and have priority but are never truncated.
- The limit does include function calls and results. 
- Function definitions are not considered messages.
```cs
using ChatAIze.GenerativeCS.Options.OpenAI;
// or
using ChatAIze.GenerativeCS.Options.Gemini;

var options = new ChatCompletionOptions
{
    CharacterLimit = 10,
};
```
### Message Pinning
Messages can be pinned to ensure they stay in the conversation even when message and character limits are exceeded.
```cs
using ChatAIze.GenerativeCS.Enums;
using ChatAIze.GenerativeCS.Models;

var conversation = new ChatConversation();

conversation.FromUser("This will always be the first message", PinLocation.Begin);
conversation.FromSystem("This message will never be truncated due to limits.", PinLocation.Automatic);
conversation.FromUser("This will always be the last (most recent) message", PinLocation.End);
```
