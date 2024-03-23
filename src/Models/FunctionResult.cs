namespace ChatAIze.GenerativeCS.Models;

public record FunctionResult
{
    public FunctionResult() { }

    public FunctionResult(string name, string? value = null)
    {
        Name = name;
        Value = value;
    }

    public FunctionResult(string id, string name, string? value = null)
    {
        Id = id;
        Name = name;
        Value = value;
    }

    public string? Id { get; set; }

    public string? Name { get; set; }

    public string? Value { get; set; }
}
