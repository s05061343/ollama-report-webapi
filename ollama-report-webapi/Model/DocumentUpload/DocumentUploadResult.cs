namespace ollama_report_webapi.Model.DocumentUpload
{
    public record DocumentUploadResult(string DocumentId, string FileName, int ChunkCount, string Status);
}
