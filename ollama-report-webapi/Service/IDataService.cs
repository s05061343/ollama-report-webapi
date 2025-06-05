namespace ollama_report_webapi.Service
{
    public interface IDataService
    {
        Task<List<Dictionary<string, object>>> GetDataByDateRange(string dataSource, DateTime startDate, DateTime endDate, List<string>? filters = null);
    }
}
