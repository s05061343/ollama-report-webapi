namespace ollama_report_webapi.Model.TextEmbedding
{
    // 文件塊模型
    public class DocumentChunk
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string DocumentId { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int ChunkIndex { get; set; }
        public float[]? Embedding { get; set; }
        public float SimilarityScore { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
