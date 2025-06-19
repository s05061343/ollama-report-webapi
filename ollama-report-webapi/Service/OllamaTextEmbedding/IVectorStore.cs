using ollama_report_webapi.Model.TextEmbedding;

namespace ollama_report_webapi.Service.OllamaTextEmbedding
{
    // 向量存儲介面
    public interface IVectorStore
    {
        Task StoreDocumentChunksAsync(List<DocumentChunk> chunks);
        Task<List<DocumentChunk>> SearchSimilarAsync(string query, int topK);
        Task<List<DocumentChunk>> GetAllDocumentsAsync();
        Task DeleteDocumentAsync(string documentId);
    }
}
