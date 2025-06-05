namespace ollama_report_webapi.Model
{
    public class ReportMetadata
    {
        public int TotalRecords { get; set; }
        public DateTime DateRange_Start { get; set; }
        public DateTime DateRange_End { get; set; }
        public string DataSource { get; set; } = string.Empty;
        public double ProcessingTime { get; set; }
    }
}
