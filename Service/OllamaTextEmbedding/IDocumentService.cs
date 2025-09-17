using ollama_report_webapi.Model.DocumentUpload;

namespace ollama_report_webapi.Service.OllamaTextEmbedding
{
    public interface IDocumentService
    {
        Task<DocumentUploadResult> ProcessDocumentAsync(IFormFile file);
        Task<string> ChatWithDocumentsAsync(string question, int topK);
    }
}
