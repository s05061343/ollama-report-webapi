namespace ollama_report_webapi.Model
{
    public class ChillerSystemParameter
    {
        public int Id { get; set; }
        public DateTime RecordTime { get; set; }
        public double IceWaterOutletTemp { get; set; } // 冰水出水溫度
        public double IceWaterReturnTemp { get; set; } // 冰水回水溫度
        public double ChillerOutletWaterTemp { get; set; } // 冷凝器出水溫度
        public double ChillerReturnWaterTemp { get; set; } // 冷凝器回水溫度
        public double OutdoorTemp { get; set; } // 外氣溫度
        public double OutdoorHumidity { get; set; } // 外氣相對濕度
        public double OutdoorEnthalpy { get; set; } // 外氣焓值
        public double SystemLoadRate { get; set; } // 系統負載率
        public double IceWaterFlow { get; set; } // 冰水流量
        public double CoolingWaterFlow { get; set; } // 冷卻水流量
        public string SystemType { get; set; } = string.Empty; // 系統類型
        public string OperationMode { get; set; } = string.Empty; // 操作模式類型
        public int SearchFileCount { get; set; } // 檢索文檔數量
    }
}
