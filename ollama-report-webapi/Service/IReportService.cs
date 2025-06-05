using ollama_report_webapi.Model;

namespace ollama_report_webapi.Service
{
    public interface IReportService
    {
        Task<ReportResponse> GenerateReportAsync(ReportRequest request);
    }
}
