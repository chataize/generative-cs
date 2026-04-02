using ChatAIze.GenerativeCS.Constants;

namespace ChatAIze.GenerativeCS.Options.Gemini;

/// <summary>
/// Configures Gemini embedding requests.
/// </summary>
public record EmbeddingOptions
{
    /// <summary>
    /// Initializes embedding options.
    /// </summary>
    /// <param name="model">Model identifier used for embeddings.</param>
    /// <param name="apiKey">Optional API key overriding the client default.</param>
    public EmbeddingOptions(string model = DefaultModels.Gemini.Embedding, string? apiKey = null)
    {
        Model = model;
        ApiKey = apiKey;
    }

    /// <summary>
    /// Gets or sets the model identifier used for embeddings.
    /// </summary>
    public string Model { get; set; } = DefaultModels.Gemini.Embedding;

    /// <summary>
    /// Gets or sets an optional API key that overrides the client-level key.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Gets or sets optional output dimensions.
    /// </summary>
    /// <remarks>Gemini only honors this on supported embedding models.</remarks>
    public int? Dimensions { get; set; }

    /// <summary>
    /// Gets or sets an optional stable end-user identifier for compatibility with existing calling code.
    /// </summary>
    /// <remarks>The current Gemini embedding endpoint does not expose a documented user id field, so this value is ignored.</remarks>
    public string? UserTrackingId { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of retry attempts for a failed request.
    /// </summary>
    public int MaxAttempts { get; set; } = 5;
}
