namespace ollama_report_webapi.Model
{
    public class ReportRequest
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string DataSource { get; set; } = "chiller_system"; // chiller_system, energy_analysis, maintenance, etc.
        public string ReportType { get; set; } = "summary"; // summary, detailed, analysis
        public List<string>? Filters { get; set; }
    }
}
