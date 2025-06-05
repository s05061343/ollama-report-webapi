using ollama_report_webapi.Model;
using System.Text;

namespace ollama_report_webapi.Service
{
    public class ReportService : IReportService
    {
        private readonly IDataService _dataService;
        private readonly IOllamaService _ollamaService;

        public ReportService(IDataService dataService, IOllamaService ollamaService)
        {
            _dataService = dataService;
            _ollamaService = ollamaService;
        }

        public async Task<ReportResponse> GenerateReportAsync(ReportRequest request)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // 1. 獲取資料
            var data = await _dataService.GetDataByDateRange(
                request.DataSource,
                request.StartDate,
                request.EndDate,
                request.Filters
            );

            // 2. 準備資料摘要
            var dataSummary = PrepareDataSummary(data, request.DataSource);

            // 3. 構建 Ollama 提示詞
            var prompt = BuildReportPrompt(request, dataSummary);

            // 4. 使用 Ollama 生成報表
            var reportContent = await _ollamaService.GenerateReportAsync(prompt);

            stopwatch.Stop();

            return new ReportResponse
            {
                ReportContent = reportContent,
                Metadata = new ReportMetadata
                {
                    TotalRecords = data.Count,
                    DateRange_Start = request.StartDate,
                    DateRange_End = request.EndDate,
                    DataSource = request.DataSource,
                    ProcessingTime = stopwatch.Elapsed.TotalSeconds
                }
            };
        }

        private string PrepareDataSummary(List<Dictionary<string, object>> data, string dataSource)
        {
            if (!data.Any()) return "無資料";

            var summary = new StringBuilder();
            summary.AppendLine($"資料筆數: {data.Count}");

            switch (dataSource.ToLower())
            {
                case "chiller_system":
                    var avgIceWaterOutletTemp = data.Average(d => Convert.ToDouble(d.GetValueOrDefault("IceWaterOutletTemp", 0)));
                    var avgIceWaterReturnTemp = data.Average(d => Convert.ToDouble(d.GetValueOrDefault("IceWaterReturnTemp", 0)));
                    var avgChillerOutletTemp = data.Average(d => Convert.ToDouble(d.GetValueOrDefault("ChillerOutletWaterTemp", 0)));
                    var avgSystemLoadRate = data.Average(d => Convert.ToDouble(d.GetValueOrDefault("SystemLoadRate", 0)));
                    var avgOutdoorTemp = data.Average(d => Convert.ToDouble(d.GetValueOrDefault("OutdoorTemp", 0)));
                    var avgOutdoorHumidity = data.Average(d => Convert.ToDouble(d.GetValueOrDefault("OutdoorHumidity", 0)));

                    summary.AppendLine($"平均冰水出水溫度: {avgIceWaterOutletTemp:F1}°C");
                    summary.AppendLine($"平均冰水回水溫度: {avgIceWaterReturnTemp:F1}°C");
                    summary.AppendLine($"平均冷凝器出水溫度: {avgChillerOutletTemp:F1}°C");
                    summary.AppendLine($"平均系統負載率: {avgSystemLoadRate:F1}%");
                    summary.AppendLine($"平均外氣溫度: {avgOutdoorTemp:F1}°C");
                    summary.AppendLine($"平均外氣濕度: {avgOutdoorHumidity:F1}%");

                    // 系統類型統計
                    var systemTypeStats = data.GroupBy(d => d.GetValueOrDefault("SystemType", "未知").ToString())
                        .Select(g => new { Type = g.Key, Count = g.Count() })
                        .OrderByDescending(x => x.Count);

                    summary.AppendLine("系統類型分布:");
                    foreach (var stat in systemTypeStats)
                    {
                        summary.AppendLine($"  {stat.Type}: {stat.Count} 筆記錄");
                    }

                    // 操作模式統計
                    var operationModeStats = data.GroupBy(d => d.GetValueOrDefault("OperationMode", "未知").ToString())
                        .Select(g => new { Mode = g.Key, Count = g.Count() })
                        .OrderByDescending(x => x.Count);

                    summary.AppendLine("操作模式分布:");
                    foreach (var stat in operationModeStats)
                    {
                        summary.AppendLine($"  {stat.Mode}: {stat.Count} 筆記錄");
                    }
                    break;

                case "energy_analysis":
                    var avgRecommendedIceTemp = data.Average(d => Convert.ToDouble(d.GetValueOrDefault("RecommendedIceWaterOutletTemp", 0)));
                    var avgRecommendedCoolingTemp = data.Average(d => Convert.ToDouble(d.GetValueOrDefault("RecommendedCoolingWaterTemp", 0)));
                    var avgCombinedEfficiency = data.Average(d => Convert.ToDouble(d.GetValueOrDefault("CombinedEfficiency", 0)));
                    var totalAnnualEnergySaving = data.Sum(d => Convert.ToDouble(d.GetValueOrDefault("AnnualEnergySaving", 0)));
                    var totalAnnualCostSaving = data.Sum(d => Convert.ToDouble(d.GetValueOrDefault("AnnualCostSaving", 0)));

                    summary.AppendLine($"建議平均冰水出水溫度: {avgRecommendedIceTemp:F1}°C");
                    summary.AppendLine($"建議平均冷卻水溫度: {avgRecommendedCoolingTemp:F1}°C");
                    summary.AppendLine($"平均綜合節能效益: {avgCombinedEfficiency:F1}%");
                    summary.AppendLine($"年度節能潛力: {totalAnnualEnergySaving:N0} kWh");
                    summary.AppendLine($"年度節省費用: {totalAnnualCostSaving:N0} 元");
                    break;

                case "maintenance":
                    var totalWorkHours = data.Sum(d => Convert.ToInt32(d.GetValueOrDefault("WorkHours", 0)));
                    var totalCost = data.Sum(d => Convert.ToDouble(d.GetValueOrDefault("Cost", 0)));
                    var avgWorkHours = data.Average(d => Convert.ToInt32(d.GetValueOrDefault("WorkHours", 0)));

                    summary.AppendLine($"總維護工時: {totalWorkHours} 小時");
                    summary.AppendLine($"總維護成本: {totalCost:N0} 元");
                    summary.AppendLine($"平均工時: {avgWorkHours:F1} 小時/次");

                    // 維護類型統計
                    var maintenanceTypeStats = data.GroupBy(d => d.GetValueOrDefault("MaintenanceType", "未知").ToString())
                        .Select(g => new { Type = g.Key, Count = g.Count() })
                        .OrderByDescending(x => x.Count);

                    summary.AppendLine("維護類型分布:");
                    foreach (var stat in maintenanceTypeStats)
                    {
                        summary.AppendLine($"  {stat.Type}: {stat.Count} 次");
                    }

                    // 狀態統計
                    var statusStats = data.GroupBy(d => d.GetValueOrDefault("Status", "未知").ToString())
                        .Select(g => new { Status = g.Key, Count = g.Count() })
                        .OrderByDescending(x => x.Count);

                    summary.AppendLine("處理狀態:");
                    foreach (var stat in statusStats)
                    {
                        summary.AppendLine($"  {stat.Status}: {stat.Count} 項");
                    }
                    break;

                default:
                    summary.AppendLine("廠務系統一般參數統計已準備完成");
                    break;
            }

            return summary.ToString();
        }

        private string BuildReportPrompt(ReportRequest request, string dataSummary)
        {
            var prompt = new StringBuilder();

            prompt.AppendLine("請根據以下廠務系統資料生成專業的中文報表:");
            prompt.AppendLine($"報表類型: {request.ReportType}");
            prompt.AppendLine($"資料來源: {request.DataSource}");
            prompt.AppendLine($"時間範圍: {request.StartDate:yyyy-MM-dd} 至 {request.EndDate:yyyy-MM-dd}");
            prompt.AppendLine();
            prompt.AppendLine("廠務系統資料摘要:");
            prompt.AppendLine(dataSummary);
            prompt.AppendLine();

            switch (request.ReportType.ToLower())
            {
                case "summary":
                    prompt.AppendLine("請生成一份廠務系統摘要報表，包含:");
                    prompt.AppendLine("1. 系統運行概況");
                    prompt.AppendLine("2. 關鍵性能指標 (溫度、負載率、流量等)");
                    prompt.AppendLine("3. 能效表現分析");
                    prompt.AppendLine("4. 系統優化建議");
                    break;

                case "detailed":
                    prompt.AppendLine("請生成一份詳細的廠務系統報表，包含:");
                    prompt.AppendLine("1. 詳細設備運行數據分析");
                    prompt.AppendLine("2. 溫度控制趨勢分析");
                    prompt.AppendLine("3. 能耗效率深度分析");
                    prompt.AppendLine("4. 異常狀況識別與分析");
                    prompt.AppendLine("5. 具體技術改善方案");
                    break;

                case "analysis":
                    prompt.AppendLine("請生成一份廠務系統分析報表，包含:");
                    prompt.AppendLine("1. 系統性能深度洞察");
                    prompt.AppendLine("2. 節能潛力評估");
                    prompt.AppendLine("3. 運行模式優化分析");
                    prompt.AppendLine("4. 預測性維護建議");
                    prompt.AppendLine("5. 投資回報率分析");
                    break;

                case "energy_saving":
                    prompt.AppendLine("請生成一份節能分析報表，包含:");
                    prompt.AppendLine("1. 當前能耗狀況分析");
                    prompt.AppendLine("2. 節能改善方案評估");
                    prompt.AppendLine("3. 投資效益分析");
                    prompt.AppendLine("4. 實施時程規劃");
                    break;
            }

            // 根據資料來源添加特定分析重點
            switch (request.DataSource.ToLower())
            {
                case "chiller_system":
                    prompt.AppendLine();
                    prompt.AppendLine("冰水系統分析重點:");
                    prompt.AppendLine("- 冰水出水/回水溫度效率分析");
                    prompt.AppendLine("- 冷凝器水溫最佳化建議");
                    prompt.AppendLine("- 系統負載率與外氣條件關聯性");
                    prompt.AppendLine("- 不同系統類型效能比較");
                    break;

                case "energy_analysis":
                    prompt.AppendLine();
                    prompt.AppendLine("節能分析重點:");
                    prompt.AppendLine("- 溫度調整節能效益量化");
                    prompt.AppendLine("- 年度節能潛力評估");
                    prompt.AppendLine("- 成本效益分析");
                    prompt.AppendLine("- 實施優先順序建議");
                    break;

                case "maintenance":
                    prompt.AppendLine();
                    prompt.AppendLine("維護管理重點:");
                    prompt.AppendLine("- 維護效率與成本分析");
                    prompt.AppendLine("- 預防性維護規劃");
                    prompt.AppendLine("- 設備健康狀況評估");
                    prompt.AppendLine("- 維護資源最佳化");
                    break;
            }

            prompt.AppendLine();
            prompt.AppendLine("報表格式要求:");
            prompt.AppendLine("- 使用繁體中文");
            prompt.AppendLine("- 包含專業的廠務工程術語");
            prompt.AppendLine("- 結構清晰，段落分明");
            prompt.AppendLine("- 包含具體的數值和工程單位");
            prompt.AppendLine("- 提供可執行的技術建議");
            prompt.AppendLine("- 符合台灣廠務管理標準");

            return prompt.ToString();
        }
    }
}
