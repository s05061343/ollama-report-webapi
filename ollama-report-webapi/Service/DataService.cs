namespace ollama_report_webapi.Service
{
    public class DataService : IDataService
    {
        // 廠務系統資料服務 - 實際應用中這裡會連接到廠務資料庫
        public async Task<List<Dictionary<string, object>>> GetDataByDateRange(string dataSource, DateTime startDate, DateTime endDate, List<string>? filters = null)
        {
            await Task.Delay(100); // 模擬資料庫查詢延遲

            var data = new List<Dictionary<string, object>>();
            var random = new Random();

            // 根據廠務系統資料源類型生成模擬資料
            switch (dataSource.ToLower())
            {
                case "chiller_system":
                    for (int i = 0; i < 100; i++)
                    {
                        var recordTime = startDate.AddHours(random.Next((int)(endDate - startDate).TotalHours));
                        data.Add(new Dictionary<string, object>
                        {
                            ["RecordTime"] = recordTime,
                            ["IceWaterOutletTemp"] = Math.Round(6.0 + random.NextDouble() * 3.0, 1), // 6-9°C
                            ["IceWaterReturnTemp"] = Math.Round(10.0 + random.NextDouble() * 4.0, 1), // 10-14°C
                            ["ChillerOutletWaterTemp"] = Math.Round(30.0 + random.NextDouble() * 10.0, 1), // 30-40°C
                            ["ChillerReturnWaterTemp"] = Math.Round(28.0 + random.NextDouble() * 8.0, 1), // 28-36°C
                            ["OutdoorTemp"] = Math.Round(25.0 + random.NextDouble() * 10.0, 1), // 25-35°C
                            ["OutdoorHumidity"] = Math.Round(60.0 + random.NextDouble() * 20.0, 1), // 60-80%
                            ["OutdoorEnthalpy"] = Math.Round(80.0 + random.NextDouble() * 20.0, 1), // 80-100 kJ/kg
                            ["SystemLoadRate"] = Math.Round(60.0 + random.NextDouble() * 30.0, 1), // 60-90%
                            ["IceWaterFlow"] = Math.Round(80.0 + random.NextDouble() * 40.0, 1), // 80-120 m³/h
                            ["CoolingWaterFlow"] = Math.Round(100.0 + random.NextDouble() * 50.0, 1), // 100-150 m³/h
                            ["SystemType"] = new[] { "標準中央空調系統", "變頻中央空調系統", "磁浮離心機系統" }[random.Next(3)],
                            ["OperationMode"] = new[] { "精確模式", "節能模式", "標準模式" }[random.Next(3)],
                            ["SearchFileCount"] = random.Next(1, 10)
                        });
                    }
                    break;

                case "energy_analysis":
                    for (int i = 0; i < 50; i++)
                    {
                        var analysisTime = startDate.AddDays(random.Next((endDate - startDate).Days));
                        data.Add(new Dictionary<string, object>
                        {
                            ["AnalysisTime"] = analysisTime,
                            ["RecommendedIceWaterOutletTemp"] = Math.Round(7.0 + random.NextDouble() * 2.0, 1), // 7-9°C
                            ["RecommendedCoolingWaterTemp"] = Math.Round(26.0 + random.NextDouble() * 4.0, 1), // 26-30°C
                            ["IceWaterTempAdjustmentEfficiency"] = Math.Round(1.0 + random.NextDouble() * 3.0, 1), // 1-4%
                            ["CoolingWaterTempAdjustmentEfficiency"] = Math.Round(10.0 + random.NextDouble() * 8.0, 1), // 10-18%
                            ["CombinedEfficiency"] = Math.Round(8.0 + random.NextDouble() * 8.0, 1), // 8-16%
                            ["AnnualEnergySaving"] = Math.Round(25000 + random.NextDouble() * 15000, 0), // 25000-40000 kWh
                            ["AnnualCostSaving"] = Math.Round(30000 + random.NextDouble() * 20000, 0), // 30000-50000 元
                            ["SystemType"] = new[] { "標準中央空調系統", "變頻中央空調系統", "磁浮離心機系統" }[random.Next(3)]
                        });
                    }
                    break;

                case "maintenance":
                    for (int i = 0; i < 30; i++)
                    {
                        data.Add(new Dictionary<string, object>
                        {
                            ["MaintenanceDate"] = startDate.AddDays(random.Next((endDate - startDate).Days)),
                            ["EquipmentId"] = $"CHILLER-{random.Next(1, 5):D2}",
                            ["MaintenanceType"] = new[] { "定期保養", "故障維修", "系統優化", "清洗保養" }[random.Next(4)],
                            ["Status"] = new[] { "已完成", "進行中", "待處理" }[random.Next(3)],
                            ["TechnicianName"] = $"技師{random.Next(101, 199)}",
                            ["WorkHours"] = random.Next(2, 12),
                            ["Cost"] = random.Next(5000, 50000)
                        });
                    }
                    break;

                default:
                    // 一般廠務資料格式
                    for (int i = 0; i < 25; i++)
                    {
                        data.Add(new Dictionary<string, object>
                        {
                            ["RecordTime"] = startDate.AddHours(random.Next((int)(endDate - startDate).TotalHours)),
                            ["Parameter"] = $"參數{random.Next(1, 10)}",
                            ["Value"] = Math.Round(random.NextDouble() * 100, 2),
                            ["Unit"] = new[] { "°C", "%", "kW", "m³/h" }[random.Next(4)],
                            ["Status"] = new[] { "正常", "警告", "異常" }[random.Next(3)]
                        });
                    }
                    break;
            }

            return data;
        }
    }
}
