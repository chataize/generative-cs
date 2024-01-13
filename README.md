# GenerativeCS
Generative AI library for .NET 8.0 with built-in OpenAI ChatGPT and Google Gemini API clients and support for C# function calling via reflection.

## Installation
### .NET CLI
```bash
dotnet add package GenerativeCS
```
### Package Manager Console
```powershell
Install-Package GenerativeCS
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
**Synthesize to File**
```cs
var client = new OpenAIClient("<OPENAI API KEY>");
await client.SynthesizeSpeechAsync("The quick brown fox jumps over the lazy dog", "speech.mp3");
```
**Synthesize to Byte Array**
```cs
using ChatAIze.GenerativeCS.Clients;

var client = new OpenAIClient("<OPENAI API KEY>");
byte[] speech = await client.SynthesizeSpeechAsync("The quick brown fox jumps over the lazy dog");
```
### Speech-to-Text
**Transcript From File**
```cs
using ChatAIze.GenerativeCS.Clients;

var client = new OpenAIClient("<OPENAI API KEY>");
string transcript = await client.TranscriptAsync("speech.mp3");
```
**Transcript From Byte Array**
```cs
using ChatAIze.GenerativeCS.Clients;

var client = new OpenAIClient("<OPENAI API KEY>");
byte[] audio = await File.ReadAllBytesAsync("speech.mp3");
string transcript = await client.TranscriptAsync(audio);
```
**Translate From File**
```cs
using ChatAIze.GenerativeCS.Clients;

var client = new OpenAIClient("<OPENAI API KEY>");
string translation = await client.TranslateAsync("speech.mp3");
```
**Translate From Byte Array**
```cs
using ChatAIze.GenerativeCS.Clients;

var client = new OpenAIClient("<OPENAI API KEY>");
byte[] audio = await File.ReadAllBytesAsync("speech.mp3");
string translation = await client.TranslateAsync(audio);
```
## Options
> [!TIP]
> If you use **OpenAI** client, add:
> ```cs
> using ChatAIze.GenerativeCS.Options.OpenAI;
> ```
> If you use **Gemini** client, add:
> ```cs
> using ChatAIze.GenerativeCS.Options.Gemini;
> ```
### Chat Completion
**OpenAI Client**
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
        await Console.Out.WriteLineAsync($"Message {message} added");
    },
    TimeCallback = () => DateTime.Now
};

// Set for entire client:
var client = new OpenAIClient("<OPENAI API KEY>", options); // via constructor
client.DefaultCompletionOptions = options; // via property

// Set for single completion:
string response = await client.CompleteAsync(prompt, options);
string response = await client.CompleteAsync(conversartion, options);
```
**Gemini Client**
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

// Set for single completion:
string response = await client.CompleteAsync(prompt, options);
string response = await client.CompleteAsync(conversartion, options);
```
### Embeddings
```cs
using ChatAIze.GenerativeCS.Clients;
using ChatAIze.GenerativeCS.Options.OpenAI;

var options = new EmbeddingOptions
{
    Model = "text-embedding-ada-002",
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
**Text-to-Speech**
```cs
using ChatAIze.GenerativeCS.Clients;
using ChatAIze.GenerativeCS.Enums;
using ChatAIze.GenerativeCS.Options.OpenAI;

var options = new TextToSpeechOptions
{
    Model = "tts-1",
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
**Transcription**
```cs
using ChatAIze.GenerativeCS.Clients;
using ChatAIze.GenerativeCS.Enums;
using ChatAIze.GenerativeCS.Options.OpenAI;

var options = new TranscriptionOptions
{
    Model = "whisper-1",
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
**Translation**
```cs
using ChatAIze.GenerativeCS.Clients;
using ChatAIze.GenerativeCS.Enums;
using ChatAIze.GenerativeCS.Options.OpenAI;

var options = new TranslationOptions
{
    Model = "whisper-1",
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
