using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;

namespace ollama_report_webapi.Service.OllamaTextEmbedding
{
    // Embedding 服務實作
    public class EmbeddingService : IEmbeddingService
    {
        private readonly ITextEmbeddingGenerationService _embeddingService;

        public EmbeddingService(Kernel kernel)
        {
            _embeddingService = kernel.GetRequiredService<ITextEmbeddingGenerationService>();
        }

        public async Task<float[]> GenerateEmbeddingAsync(string text)
        {
            var embedding = await _embeddingService.GenerateEmbeddingAsync(text);
            return embedding.ToArray();
        }

        public async Task<List<float[]>> GenerateEmbeddingsAsync(List<string> texts)
        {
            var embeddings = new List<float[]>();
            foreach (var text in texts)
            {
                var embedding = await GenerateEmbeddingAsync(text);
                embeddings.Add(embedding);
            }
            return embeddings;
        }

        public Task<float> CalculateSimilarityAsync(float[] embedding1, float[] embedding2)
        {
            // 計算餘弦相似度
            var dotProduct = embedding1.Zip(embedding2, (a, b) => a * b).Sum();
            var magnitude1 = Math.Sqrt(embedding1.Sum(x => x * x));
            var magnitude2 = Math.Sqrt(embedding2.Sum(x => x * x));

            var similarity = (float)(dotProduct / (magnitude1 * magnitude2));
            return Task.FromResult(similarity);
        }
    }
}
