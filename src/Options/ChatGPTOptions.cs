namespace GenerativeCS.Options;

public class ChatGPTOptions
{
    public required string ApiKey { get; set; }

    public string Model { get; set; } = "gpt-3.5-turbo";
}
