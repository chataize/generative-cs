namespace GenerativeCS.Interfaces;

public interface IEmbeddingProvider
{
    Task<List<float>> GetEmbeddingAsync(string text, CancellationToken cancellationToken);
}
