using ChatAIze.GenerativeCS.Clients;
using ChatAIze.GenerativeCS.Models;
using ChatAIze.GenerativeCS.Options.OpenAI;

var client = new OpenAIClient();
var conversation = new ChatConversation();

var options = new ChatCompletionOptions
{
    IsIgnoringPreviousFunctionCalls = true,
    IsDebugMode = true
};

options.AddFunction("Foo", (int number) => Console.WriteLine($"Bar [{number}]"));

while (true)
{
    var input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input))
    {
        break;
    }

    conversation.FromUser(input);

    var response = await client.CompleteAsync(conversation, options);
    Console.WriteLine(response);
}
