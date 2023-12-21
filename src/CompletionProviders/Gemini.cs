using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text.Json;
using System.Text.Json.Nodes;
using GenerativeCS.Enums;
using GenerativeCS.Interfaces;
using GenerativeCS.Models;
using GenerativeCS.Utilities;

namespace GenerativeCS.CompletionProviders;

public class Gemini<TConversation, TMessage> : ICompletionProvider<TConversation, TMessage> where TConversation : IChatConversation<TMessage>, new() where TMessage : IChatMessage, new()
{
    private readonly HttpClient _client = new();

    public Gemini() { }

    [SetsRequiredMembers]
    public Gemini(string apiKey, string model = "gemini-pro")
    {
        ApiKey = apiKey;
        Model = model;
    }

    public required string ApiKey { get; set; }

    public string Model { get; set; } = "gemini-pro";

    public ICollection<Delegate> Functions { get; set; } = new List<Delegate>();

    public async Task<string> CompleteAsync(string prompt, CancellationToken cancellationToken = default)
    {
        var partObject = new JsonObject
        {
            { "text", prompt }
        };

        var partsArray = new JsonArray
        {
            partObject
        };

        var contentObject = new JsonObject
        {
            { "parts", partsArray}
        };

        var contentsArray = new JsonArray
        {
            contentObject
        };

        var requestObject = new JsonObject
        {
            { "contents", contentsArray }
        };

        var response = await _client.PostAsJsonAsync($"https://generativelanguage.googleapis.com/v1beta/models/{Model}:generateContent?key={ApiKey}", requestObject, cancellationToken);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStreamAsync(cancellationToken);
        var document = await JsonDocument.ParseAsync(content, cancellationToken: cancellationToken);
        var message = document.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString()!;

        return message;
    }

    public async Task<string> CompleteAsync(TConversation conversation, CancellationToken cancellationToken = default)
    {

        var contentsArray = new JsonArray();
        foreach (var message in conversation.Messages)
        {
            var partObject = new JsonObject();
            if (message.FunctionCall != null)
            {
                var functionCallObject = new JsonObject
                {
                    { "name", message.FunctionCall.Name },
                    { "args", JsonObject.Create(message.FunctionCall.Arguments) }
                };

                partObject.Add("functionCall", functionCallObject);
            }
            else if (message.FunctionResult != null)
            {
                var responseObject = new JsonObject
                {
                    { "name", message.FunctionResult.Name },
                    { "content", JsonSerializer.SerializeToNode(message.FunctionResult.Result) }
                };

                var functionResponseObject = new JsonObject
                {
                    { "name", message.FunctionResult.Name },
                    { "response", responseObject }
                };

                partObject.Add("functionResponse", functionResponseObject);
            }
            else
            {
                partObject.Add("text", message.Content);
            }

            var partsArray = new JsonArray
            {
                partObject
            };

            var contentObject = new JsonObject
            {
                { "role", GetRoleName(message.Role) },
                { "parts", partsArray }
            };

            contentsArray.Add(contentObject);
        }

        var functionsObject = new JsonObject
        {
            { "function_declarations", FunctionSerializer.Serialize(Functions) }
        };

        var toolsArray = new JsonArray
        {
            functionsObject
        };

        var requestObject = new JsonObject
        {
            { "contents", contentsArray },
            { "tools", toolsArray }
        };

        var response = await _client.PostAsJsonAsync($"https://generativelanguage.googleapis.com/v1beta/models/{Model}:generateContent?key={ApiKey}", requestObject, cancellationToken);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStreamAsync(cancellationToken);
        var document = await JsonDocument.ParseAsync(content, cancellationToken: cancellationToken);
        var parts = document.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts");

        string text = null!;
        foreach (var part in parts.EnumerateArray())
        {
            if (part.TryGetProperty("functionCall", out var functionCall))
            {
                var functionName = functionCall.GetProperty("name").GetString()!;
                var arguments = functionCall.GetProperty("args");

                conversation.FromAssistant(new FunctionCall(functionName, arguments));

                var simplifiedName = functionName.Replace("_", "");
                var function = Functions.FirstOrDefault(f => f.Method.Name.Equals(simplifiedName, StringComparison.InvariantCultureIgnoreCase));

                if (function != null)
                {
                    var result = await FunctionInvoker.InvokeAsync(function, arguments, cancellationToken);
                    conversation.FromFunction(new FunctionResult(functionName, result));

                    return await CompleteAsync(conversation, cancellationToken);
                }
                else
                {
                    conversation.FromFunction(new FunctionResult(functionName, $"Function '{simplifiedName}' not found."));
                }
            }

            text = part.GetProperty("text").GetString()!;
            conversation.FromAssistant(text);
        }

        return text;
    }

    private static string GetRoleName(ChatRole role)
    {
        return role switch
        {
            ChatRole.Assistant => "model",
            ChatRole.Function => "function",
            _ => "user"
        };
    }
}

public class Gemini : Gemini<ChatConversation, ChatMessage>
{
    public Gemini() { }

    [SetsRequiredMembers]
    public Gemini(string apiKey, string model = "gemini-pro") : base(apiKey, model) { }
}
