namespace GenerativeCS.Options;

public record ChatGPTOptions
{
    public required string ApiKey { get; set; }

    public string Model { get; set; } = "gpt-3.5-turbo";
}
