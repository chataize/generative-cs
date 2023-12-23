using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using GenerativeCS.Interfaces;

namespace GenerativeCS.Models;

public record FunctionCall : IFunctionCall
{
    public FunctionCall() { }

    [SetsRequiredMembers]
    public FunctionCall(string id, string name, JsonElement arguments)
    {
        Id = id;
        Name = name;
        Arguments = arguments;
    }

    public required string Id { get; set; }

    public required string Name { get; set; }

    public required JsonElement Arguments { get; set; }
}
