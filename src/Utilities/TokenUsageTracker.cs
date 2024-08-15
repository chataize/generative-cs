namespace ChatAIze.GenerativeCS.Utilities;

public sealed class TokenUsageTracker
{
    public int PromptTokens { get; private set; }

    public int CompletionTokens { get; private set; }

    public void AddPromptTokens(int tokens)
    {
        PromptTokens += tokens;
    }

    public void AddCompletionTokens(int tokens)
    {
        CompletionTokens += tokens;
    }
}
