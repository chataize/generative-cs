using ChatAIze.GenerativeCS.Clients;
using ChatAIze.GenerativeCS.Constants;
using ChatAIze.GenerativeCS.Enums;
using ChatAIze.GenerativeCS.Models;
using ChatAIze.GenerativeCS.Options.OpenAI;

var client = new OpenAIClient();
var chat = new Chat();

var options = new ChatCompletionOptions
{
    Model = ChatCompletionModels.OpenAI.GPT51,
    IsStoringOutputs = true,
    IsDebugMode = true,
    ReasoningEffort = ReasoningEffort.None,
    Verbosity = Verbosity.High
};

options.AddFunction("Check City Temperature", (string city) => Random.Shared.Next(0, 100));
options.AddFunction("Check Country Temperature", async (string country) =>
{
    await Task.Delay(5000);
    return new { Temp = Random.Shared.Next(0, 100) };
});

while (true)
{
    var input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input))
    {
        break;
    }

    chat.FromUser(input);

    var response = await client.CompleteAsync(chat, options);
    Console.WriteLine(response);
}
