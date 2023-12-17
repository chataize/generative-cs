using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using GenerativeCS.Interfaces;
using GenerativeCS.Models;

namespace GenerativeCS.CompletionProviders;

public class ChatGPT<TConversation, TMessage> : ICompletionProvider<TConversation, TMessage> where TConversation : IChatConversation<TMessage>, new() where TMessage : IChatMessage, new()
{
    private readonly HttpClient _client = new();

    public ChatGPT() { }

    [SetsRequiredMembers]
    public ChatGPT(string apiKey, string model = "gpt-3.5-turbo")
    {
        ApiKey = apiKey;
        Model = model;
    }

    public required string ApiKey
    {
        get => _client.DefaultRequestHeaders.Authorization?.Parameter!;
        set => _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", value);
    }

    public string Model { get; set; } = "gpt-3.5-turbo";

    public async Task<string> CompleteAsync(string prompt)
    {
        var conversation = new TConversation();
        conversation.FromSystem(prompt);

        return await CompleteAsync(conversation);
    }

    public async Task<string> CompleteAsync(TConversation conversation)
    {
        var request = new CompletionRequest<TMessage>(Model, conversation.Messages);
        var response = await _client.PostAsJsonAsync("https://api.openai.com/v1/chat/completions", request);

        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStreamAsync();
        var document = await JsonDocument.ParseAsync(content);
        var message = document.RootElement.GetProperty("choices")[0].GetProperty("text").GetString()!;

        conversation.FromAssistant(message);
        return message;
    }
}

public class ChatGPT : ChatGPT<ChatConversation, ChatMessage>
{
    public ChatGPT() { }

    [SetsRequiredMembers]
    public ChatGPT(string apiKey, string model = "gpt-3.5-turbo") : base(apiKey, model) { }
}
