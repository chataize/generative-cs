using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace ChatAIze.GenerativeCS.Models;

public record FunctionCall
{
    public FunctionCall() { }

    [SetsRequiredMembers]
    public FunctionCall(string name, JsonElement arguments)
    {
        Name = name;
        Arguments = arguments;
    }

    [SetsRequiredMembers]
    public FunctionCall(string id, string name, JsonElement arguments)
    {
        Id = id;
        Name = name;
        Arguments = arguments;
    }

    public string? Id { get; set; }

    public required string Name { get; set; }

    public required JsonElement Arguments { get; set; }
}
