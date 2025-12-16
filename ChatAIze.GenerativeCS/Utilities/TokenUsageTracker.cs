namespace ChatAIze.GenerativeCS.Utilities;

/// <summary>
/// Tracks token usage across multiple model requests.
/// </summary>
public sealed class TokenUsageTracker
{
    /// <summary>
    /// Gets the number of prompt tokens consumed.
    /// </summary>
    /// <remarks>Increment-only; caller is responsible for external synchronization if used across threads.</remarks>
    public int PromptTokens { get; private set; }

    /// <summary>
    /// Gets the number of cached prompt tokens reused.
    /// </summary>
    /// <remarks>Increment-only; caller is responsible for external synchronization if used across threads.</remarks>
    public int CachedTokens { get; private set; }

    /// <summary>
    /// Gets the number of completion tokens generated.
    /// </summary>
    /// <remarks>Increment-only; caller is responsible for external synchronization if used across threads.</remarks>
    public int CompletionTokens { get; private set; }

    /// <summary>
    /// Increments the prompt token count.
    /// </summary>
    /// <param name="tokens">Number of tokens to add.</param>
    public void AddPromptTokens(int tokens)
    {
        PromptTokens += tokens;
    }

    /// <summary>
    /// Increments the cached token count.
    /// </summary>
    /// <param name="tokens">Number of tokens to add.</param>
    public void AddCachedTokens(int tokens)
    {
        CachedTokens += tokens;
    }

    /// <summary>
    /// Increments the completion token count.
    /// </summary>
    /// <param name="tokens">Number of tokens to add.</param>
    public void AddCompletionTokens(int tokens)
    {
        CompletionTokens += tokens;
    }
}
