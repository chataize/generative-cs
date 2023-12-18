using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text.Json;
using GenerativeCS.Interfaces;
using GenerativeCS.Models;

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

    public async Task<string> CompleteAsync(string prompt)
    {
        var payload = new
        {
            Contents = new
            {
                Parts = new object[] {
                    new {
                        Text = prompt
                    }
                }
            }
        };

        var response = await _client.PostAsJsonAsync($"https://generativelanguage.googleapis.com/v1beta/models/{Model}:generateContent?key={ApiKey}", payload);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStreamAsync();
        var document = await JsonDocument.ParseAsync(content);
        var message = document.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString()!;

        return message;
    }

    public Task<string> CompleteAsync(TConversation conversation)
    {
        throw new NotImplementedException();
    }
}

public class Gemini : Gemini<ChatConversation, ChatMessage>
{
    public Gemini() { }

    [SetsRequiredMembers]
    public Gemini(string apiKey, string model = "gemini-pro") : base(apiKey, model) { }
}
