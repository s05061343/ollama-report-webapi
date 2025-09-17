namespace ollama_report_webapi.Model
{
    public class EnergyAnalysisResult
    {
        public int Id { get; set; }
        public DateTime AnalysisTime { get; set; }
        public double RecommendedIceWaterOutletTemp { get; set; } // 建議冰水出水溫度
        public double RecommendedCoolingWaterTemp { get; set; } // 建議冷卻水溫度
        public double IceWaterTempAdjustmentEfficiency { get; set; } // 冰水溫度調整節能
        public double CoolingWaterTempAdjustmentEfficiency { get; set; } // 冷卻水溫度調整節能
        public double CombinedEfficiency { get; set; } // 綜合節能效益
        public double AnnualEnergySaving { get; set; } // 年度節省能耗 (kWh)
        public double AnnualCostSaving { get; set; } // 年度節省費用 (元)
        public string Recommendations { get; set; } = string.Empty; // 專業建議
        public int SystemId { get; set; }
    }
}
