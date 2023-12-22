using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using GenerativeCS.Interfaces;
using GenerativeCS.Models;

namespace GenerativeCS.CompletionProviders;

public class ChatGPT<TConversation, TMessage, TFunction> : ICompletionProvider<TConversation, TMessage, TFunction>
    where TConversation : IChatConversation<TMessage, TFunction>, new()
    where TMessage : IChatMessage, new()
    where TFunction : IChatFunction, new()
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

    public ICollection<TFunction> Functions { get; set; } = new List<TFunction>();

    public async Task<string> CompleteAsync(string prompt, CancellationToken cancellationToken = default)
    {
        var conversation = new TConversation();
        conversation.FromSystem(prompt);

        return await CompleteAsync(conversation, cancellationToken);
    }

    public async Task<string> CompleteAsync(TConversation conversation, CancellationToken cancellationToken = default)
    {
        var request = new
        {
            Model,
            Messages = conversation.Messages.Select(m => new { m.Role, m.Author, m.Content }).ToList()
        };

        var response = await _client.PostAsJsonAsync("https://api.openai.com/v1/chat/completions", request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStreamAsync(cancellationToken);
        var document = await JsonDocument.ParseAsync(content, cancellationToken: cancellationToken);
        var message = document.RootElement.GetProperty("choices")[0].GetProperty("text").GetString()!;

        conversation.FromAssistant(message);
        return message;
    }
}

public class ChatGPT : ChatGPT<ChatConversation, ChatMessage, ChatFunction>
{
    public ChatGPT() { }

    [SetsRequiredMembers]
    public ChatGPT(string apiKey, string model = "gpt-3.5-turbo") : base(apiKey, model) { }
}
