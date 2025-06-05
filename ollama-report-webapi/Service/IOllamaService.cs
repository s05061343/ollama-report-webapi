namespace ollama_report_webapi.Service
{
    public interface IOllamaService
    {
        Task<string> GenerateReportAsync(string prompt);
    }
}
