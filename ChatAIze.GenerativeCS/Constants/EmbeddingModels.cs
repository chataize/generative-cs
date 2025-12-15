namespace ChatAIze.GenerativeCS.Constants;

/// <summary>
/// Supported embedding model identifiers.
/// </summary>
public static class EmbeddingModels
{
    /// <summary>
    /// OpenAI embedding models.
    /// </summary>
    public static class OpenAI
    {
        /// <summary>
        /// Embedding model <c>text-embedding-3-small</c>.
        /// </summary>
        public const string TextEmbedding3Small = "text-embedding-3-small";

        /// <summary>
        /// Embedding model <c>text-embedding-3-large</c>.
        /// </summary>
        public const string TextEmbedding3Large = "text-embedding-3-large";

        /// <summary>
        /// Legacy embedding model <c>text-embedding-ada-002</c>.
        /// </summary>
        public const string TextEmbeddingAda002 = "text-embedding-ada-002";
    }
}
