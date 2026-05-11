using ChatAIze.GenerativeCS.Constants;
using ChatAIze.GenerativeCS.Enums;
using ChatAIze.Utilities.Extensions;

namespace ChatAIze.GenerativeCS.Tests;

public sealed class ModelDefaultsTests
{
    [Fact]
    public void OpenAI_GPT55Constants_MatchDocumentedModelIds()
    {
        Assert.Equal("gpt-5.5", ChatCompletionModels.OpenAI.GPT55);
        Assert.Equal("gpt-5.5-2026-04-23", ChatCompletionModels.OpenAI.GPT5520260423);
    }

    [Fact]
    public void OpenAI_ChatCompletionDefault_UsesGPT55()
    {
        var options = new ChatAIze.GenerativeCS.Options.OpenAI.ChatCompletionOptions();

        Assert.Equal(ChatCompletionModels.OpenAI.GPT55, DefaultModels.OpenAI.ChatCompletion);
        Assert.Equal(ChatCompletionModels.OpenAI.GPT55, options.Model);
    }

    [Fact]
    public void ReasoningEffort_XHigh_SerializesToOpenAIValue()
    {
        Assert.Equal("xhigh", ReasoningEffort.XHigh.ToString().ToSnakeLower());
    }
}
