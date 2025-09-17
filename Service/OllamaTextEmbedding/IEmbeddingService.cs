namespace ollama_report_webapi.Service.OllamaTextEmbedding
{
    // Embedding 服務介面
    public interface IEmbeddingService
    {
        Task<float[]> GenerateEmbeddingAsync(string text);
        Task<List<float[]>> GenerateEmbeddingsAsync(List<string> texts);
        Task<float> CalculateSimilarityAsync(float[] embedding1, float[] embedding2);
    }
}
