namespace ollama_report_webapi.Model
{
    public class ReportResponse
    {
        public string ReportContent { get; set; } = string.Empty;
        public string GeneratedAt { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        public ReportMetadata Metadata { get; set; } = new();
    }
}
