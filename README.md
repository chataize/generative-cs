# Generative CS
Generative AI library for .NET 9.0 with built-in OpenAI ChatGPT and Google Gemini API clients and support for C# function calling via reflection.

![](https://github.com/chataize/generative-cs/assets/124832798/a0b46290-105d-487b-9145-6ce57a1879f7)

## Supported Features
### OpenAI
- [x] Chat Completion
- [x] Text Embedding
- [x] Text-to-Speech
- [x] Speech-to-Text
    - [x] Transcription
    - [x] Translation
- [x] Moderation
- [x] Response Streaming
- [x] Function Calling
- [ ] Image Generation
- [ ] Assistants API
- [ ] Files API
### Gemini
- [x] Chat Completion
- [x] Function Calling
- [ ] Text Embedding
- [ ] Moderation
- [ ] Response Streaming
- [x] Multi-Modal Requests
### Miscellaneous
- [x] Dependency Injection
- [x] Time Awareness
- [x] Message/Character Count Limiting
- [x] Message Pinning
- [x] Auto-Reattempt on Failure 
- [ ] Token Counting
- [ ] XML Documentation
- [ ] Unit Tests

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
> [!NOTE]
> By default, both `OpenAIClient` and `GeminiClient` services are registered as singleton. It's advised not to change global client options after the web application has already been launched. Use per-request options instead.
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
### Chat
```cs
using ChatAIze.GenerativeCS.Clients;
using ChatAIze.GenerativeCS.Models;

var client = new OpenAIClient("<OPENAI API KEY>");
var chat = new Chat();

while (true)
{
    string message = Console.ReadLine()!;
    chat.FromUser(message);

    string response = await client.CompleteAsync(chat);
    Console.WriteLine(response);
}   
```
### Streamed Chat
```cs
using ChatAIze.GenerativeCS.Clients;
using ChatAIze.GenerativeCS.Models;

var client = new OpenAIClient("<OPENAI API KEY>");
var chat = new Chat();

while (true)
{
    string message = Console.ReadLine()!;
    chat.FromUser(message);

    await foreach (string chunk in client.StreamCompletionAsync(chat))
    {
        Console.Write(chunk);
    }
}
```
> [!NOTE]
> Chatbot responses, function calls, and function results are automatically added to the chat.
> You don't need to and should not call ```chat.FromAssistant(...)``` manually, unless you want to *inject* custom messages (e.g. welcome message).
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

## Multi-Modal Requests (Gemini)

To send multi-modal requests with the Gemini client (e.g., text combined with uploaded files like PDFs, images, videos), you first need to upload the file using the `FileService` (exposed via `GeminiClient.Files`) and then reference it in your chat message.

### 1. Accessing the File Service

The `IFileService` is accessible via the `Files` property of your `GeminiClient` instance.

**If using `GeminiClient` as a single instance:**

```cs
using ChatAIze.GenerativeCS.Clients;
using ChatAIze.GenerativeCS.Models.Gemini; // For GeminiFile
using ChatAIze.GenerativeCS.Providers.Gemini; // For IFileService
using System.IO;

var geminiClient = new GeminiClient("<GEMINI API KEY>");
IFileService fileService = geminiClient.Files;

// Example usage:
// string filePath = "path/to/your/file.pdf";
// string mimeType = "application/pdf"; // Adjust mime type accordingly
// GeminiFile? uploadedFile = await fileService.UploadFileAsync(filePath, mimeType, Path.GetFileName(filePath));

// if (uploadedFile != null)
// {
//    Console.WriteLine($"File uploaded: {uploadedFile.Name}, URI: {uploadedFile.Uri}");
//    // Now use uploadedFile.Uri in a ChatMessage
// }
```

**If using Dependency Injection:**

You register `GeminiClient` (which includes `IFileService` registration) during setup. You can then inject `GeminiClient` and access its `Files` property, or inject `IFileService` directly if you only need the file operations.

```cs
// In your Startup.cs or Program.cs (service registration shown in previous DI examples)
// builder.Services.AddGeminiClient("<GEMINI API KEY>");

// In your class, Option 1: Inject GeminiClient
// private readonly GeminiClient _geminiClient;
// private readonly IFileService _fileService; // Derived from GeminiClient
// public YourService(GeminiClient geminiClient)
// {
//     _geminiClient = geminiClient;
//     _fileService = geminiClient.Files; 
// }

// In your class, Option 2: Inject IFileService directly (if preferred for just file ops)
// private readonly IFileService _fileService;
// public YourService(IFileService fileService) // Assumes IFileService is registered as shown previously
// {
//     _fileService = fileService;
// }

// async Task ProcessFile()
// {
//     string filePath = "path/to/your/file.pdf";
//     string mimeType = "application/pdf";
//     GeminiFile? uploadedFile = await _fileService.UploadFileAsync(filePath, mimeType, Path.GetFileName(filePath));
//     // ...
// }
```

### 2. Uploading a File

Once you have an `IFileService` instance (e.g., from `geminiClient.Files`):

```cs
using ChatAIze.GenerativeCS.Clients;      // For GeminiClient
using ChatAIze.GenerativeCS.Models.Gemini; // For GeminiFile
using ChatAIze.GenerativeCS.Providers.Gemini; // For IFileService
using System.IO;

// Assuming 'geminiClient' is an initialized GeminiClient instance
IFileService fileService = geminiClient.Files;

string filePath = "path/to/your/document.pdf";
string mimeType = "application/pdf"; // Change for other types e.g. "image/png", "video/mp4"
string displayName = Path.GetFileName(filePath);

GeminiFile? uploadedFile = await fileService.UploadFileAsync(filePath, mimeType, displayName);

if (uploadedFile != null)
{
    Console.WriteLine($"File uploaded. Name: {uploadedFile.Name}, URI: {uploadedFile.Uri}");
    // Store uploadedFile.Uri to use in a chat message
}
else
{
    Console.WriteLine("File upload failed.");
}
```

### 3. Sending a Chat Message with the File

After uploading the file, you use its `Uri` (which typically starts with `files/your-file-id`) and `MimeType` in a `ChatMessage` by adding a `FileDataPart`.

```cs
using ChatAIze.GenerativeCS.Clients;
using ChatAIze.GenerativeCS.Models;
using ChatAIze.Abstractions.Chat; // For ChatRole

// Assuming 'geminiClient' is an initialized GeminiClient
// Assuming 'uploadedFile' is the GeminiFile object from the successful upload

if (uploadedFile != null && uploadedFile.Uri != null && uploadedFile.MimeType != null)
{
    var chat = new Chat();
    var userMessage = new ChatMessage();
    userMessage.Role = ChatRole.User;
    userMessage.Parts.Add(new TextPart("Please summarize this document."));
    userMessage.Parts.Add(new FileDataPart(new FileDataSource(uploadedFile.MimeType, uploadedFile.Uri)));
    
    chat.Messages.Add(userMessage);

    // Using the existing CompleteAsync method which now supports parts
    string response = await geminiClient.CompleteAsync(chat);
    Console.WriteLine(response);
}
```

Supported file types and their MIME types for Gemini include a wide range (PDF, common document formats, images, audio, video). Refer to the official Google Gemini API documentation for the most up-to-date list of supported MIME types.

## Moderation
```cs
using ChatAIze.GenerativeCS.Clients;

var client = new OpenAIClient("<OPENAI API KEY>");
var result = await client.ModerateAsync("I am going to blow up your house in Minecraft.");

Console.WriteLine(result.IsFlagged); // true
Console.WriteLine(result.IsViolence); // true 
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
        Model = ChatCompletionModels.OpenAI.GPT4o,
        Temperature = 1.0
        // set other chat completion options here
    };
    configure.DefaultEmbeddingOptions = new EmbeddingOptions()
    {
        Model = EmbeddingModels.OpenAI.TextEmbedding3Large,
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
        Model = ChatCompletionModels.Gemini.GeminiPro,
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
    Model = ChatCompletionModels.OpenAI.GPT4o,
    UserTrackingId = "USER_ID_1234",
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
string response = await client.CompleteAsync(chat, options);
```
#### Gemini Client
```cs
using ChatAIze.GenerativeCS.Clients;
using ChatAIze.GenerativeCS.Constants;
using ChatAIze.GenerativeCS.Models;
using ChatAIze.GenerativeCS.Options.Gemini;

var options = new ChatCompletionOptions
{
    Model = ChatCompletionModels.Gemini.Gemini15Flash,
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
string response = await client.CompleteAsync(chat, options);
```
### Embeddings
```cs
using ChatAIze.GenerativeCS.Clients;
using ChatAIze.GenerativeCS.Constants;
using ChatAIze.GenerativeCS.Options.OpenAI;

var options = new EmbeddingOptions
{
    Model = EmbeddingModels.OpenAI.TextEmbedding3Large,
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
    Model = TextToSpeechModels.OpenAI.TTS1,
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
    Model = SpeechRecognitionModels.OpenAI.Whisper1,
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
    Model = SpeechRecognitionModels.OpenAI.Whisper1,
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
    Model = ModerationModels.OpenAI.TextModerationStable,
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
Messages can be pinned to ensure they stay in the chat even when message and character limits are exceeded.
```cs
using ChatAIze.GenerativeCS.Enums;
using ChatAIze.GenerativeCS.Models;

var chat = new Chat();

chat.FromUser("This will always be the first message", PinLocation.Begin);
chat.FromSystem("This message will never be truncated due to limits.", PinLocation.Automatic);
chat.FromUser("This will always be the last (most recent) message", PinLocation.End);
```
