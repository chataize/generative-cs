using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace ChatAIze.GenerativeCS.Models;

public record FunctionCall
{
    public FunctionCall() { }

    [SetsRequiredMembers]
    public FunctionCall(string name, string? arguments = null)
    {
        Name = name;
        Arguments = arguments;
    }

    [SetsRequiredMembers]
    public FunctionCall(string id, string name, string? arguments = null)
    {
        Id = id;
        Name = name;
        Arguments = arguments;
    }

    public string? Id { get; set; }

    public required string Name { get; set; }

    public string? Arguments { get; set; }
}
