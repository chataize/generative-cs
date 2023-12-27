using System.Diagnostics.CodeAnalysis;

namespace GenerativeCS.Models;

public record ChatFunction
{
    public ChatFunction() { }

    [SetsRequiredMembers]
    public ChatFunction(Delegate function)
    {
        Name = function.Method.Name;
        Function = function;
    }

    [SetsRequiredMembers]
    public ChatFunction(string name, Delegate function)
    {
        Name = name;
        Function = function;
    }

    [SetsRequiredMembers]
    public ChatFunction(string name, string? description, Delegate function)
    {
        Name = name;
        Description = description;
        Function = function;
    }

    [SetsRequiredMembers]
    public ChatFunction(string name, bool requireConfirmation, Delegate function)
    {
        Name = name;
        RequireConfirmation = requireConfirmation;
        Function = function;
    }

    [SetsRequiredMembers]
    public ChatFunction(string name, string? description, bool requireConfirmation, Delegate function)
    {
        Name = name;
        Description = description;
        RequireConfirmation = requireConfirmation;
        Function = function;
    }

    public required string Name { get; set; }

    public string? Description { get; set; }

    public bool RequireConfirmation { get; set; }

    public Delegate? Function { get; set; }
}
