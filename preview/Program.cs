using ChatAIze.GenerativeCS.Clients;
using ChatAIze.GenerativeCS.Models;
using ChatAIze.GenerativeCS.Options.OpenAI;

var client = new OpenAIClient();
var conversation = new ChatConversation();

var options = new ChatCompletionOptions
{
    IsStoringOutputs = true,
    IsTimeAware = true,
    IsIgnoringPreviousFunctionCalls = true,
    IsParallelFunctionCallingOn = false,
    IsDebugMode = true
};

options.AddFunction("CheckCityTemperature", (string city) => Random.Shared.Next(0, 100));
options.AddFunction("CheckCountryTemperature", async (string country) => {
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

    conversation.FromUser(input);

    var response = await client.CompleteAsync(conversation, options);
    Console.WriteLine(response);
}
