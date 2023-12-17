using System.Diagnostics.CodeAnalysis;
using GenerativeCS.Interfaces;

namespace GenerativeCS.Models;

public record FunctionCall : IFunctionCall
{
    public FunctionCall() { }

    [SetsRequiredMembers]
    public FunctionCall(string name, string arguments)
    {
        Name = name;
        Arguments = arguments;
    }

    public required string Name { get; set; }

    public required string Arguments { get; set; }
}
