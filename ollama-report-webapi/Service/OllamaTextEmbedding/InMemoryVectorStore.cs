using ollama_report_webapi.Model.TextEmbedding;

namespace ollama_report_webapi.Service.OllamaTextEmbedding
{
    // 記憶體向量存儲實作
    public class InMemoryVectorStore : IVectorStore
    {
        private readonly List<DocumentChunk> _documents = new();
        private readonly IEmbeddingService _embeddingService;

        public InMemoryVectorStore(IEmbeddingService embeddingService)
        {
            _embeddingService = embeddingService;
        }

        public Task StoreDocumentChunksAsync(List<DocumentChunk> chunks)
        {
            _documents.AddRange(chunks);
            return Task.CompletedTask;
        }

        public async Task<List<DocumentChunk>> SearchSimilarAsync(string query, int topK)
        {
            var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(query);

            var results = new List<DocumentChunk>();

            foreach (var doc in _documents)
            {
                if (doc.Embedding != null)
                {
                    var similarity = await _embeddingService.CalculateSimilarityAsync(queryEmbedding, doc.Embedding);
                    doc.SimilarityScore = similarity;
                    results.Add(doc);
                }
            }

            return results
                .OrderByDescending(x => x.SimilarityScore)
                .Take(topK)
                .ToList();
        }

        public Task<List<DocumentChunk>> GetAllDocumentsAsync()
        {
            return Task.FromResult(_documents.ToList());
        }

        public Task DeleteDocumentAsync(string documentId)
        {
            _documents.RemoveAll(x => x.DocumentId == documentId);
            return Task.CompletedTask;
        }
    }
}
