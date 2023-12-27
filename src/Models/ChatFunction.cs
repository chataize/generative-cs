using System.Diagnostics.CodeAnalysis;

namespace GenerativeCS.Models;

public record ChatFunction
{
    public ChatFunction() { }

    [SetsRequiredMembers]
    public ChatFunction(Delegate operation)
    {
        Name = operation.Method.Name;
        Operation = operation;
    }

    [SetsRequiredMembers]
    public ChatFunction(string name, Delegate operation)
    {
        Name = name;
        Operation = operation;
    }

    [SetsRequiredMembers]
    public ChatFunction(string name, string? description, Delegate operation)
    {
        Name = name;
        Description = description;
        Operation = operation;
    }

    [SetsRequiredMembers]
    public ChatFunction(string name, bool requiresConfirmation, Delegate operation)
    {
        Name = name;
        RequiresConfirmation = requiresConfirmation;
        Operation = operation;
    }

    [SetsRequiredMembers]
    public ChatFunction(string name, string? description, bool requiresConfirmation, Delegate operation)
    {
        Name = name;
        Description = description;
        RequiresConfirmation = requiresConfirmation;
        Operation = operation;
    }

    public required string Name { get; set; }

    public string? Description { get; set; }

    public bool RequiresConfirmation { get; set; }

    public required Delegate Operation { get; set; }
}
