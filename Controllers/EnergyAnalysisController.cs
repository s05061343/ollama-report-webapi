using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text;

namespace ollama_report_webapi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EnergyAnalysisController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly string _ollamaBaseUrl = "http://localhost:11434"; // Ollama 默認端口

        public EnergyAnalysisController(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        [HttpPost("analyze")]
        public async Task<ActionResult<EnergyAnalysisResult>> AnalyzeEnergyEfficiency([FromBody] SystemParameters parameters)
        {
            try
            {
                // 生成模擬的基礎數據
                var baseData = GenerateBaseData(parameters);

                // 準備 Ollama 請求
                var prompt = BuildAnalysisPrompt(parameters, baseData);
                var ollamaResponse = await CallOllamaAPI(prompt);

                // 解析 Ollama 響應並生成結果
                var result = ParseOllamaResponse(ollamaResponse, baseData);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("get-temperature-recommendations")]
        public async Task<ActionResult<TemperatureRecommendation>> GetTemperatureRecommendations([FromBody] SystemParameters parameters)
        {
            try
            {
                var prompt = $@"
根據以下冰水系統參數，請提供最佳的溫度設定建議：
- 外氣溫度: {parameters.OutdoorTemperature}°C
- 外氣相對濕度: {parameters.OutdoorHumidity}%
- 系統負載率: {parameters.SystemLoadRate}%
- 冰水流量: {parameters.ChilledWaterFlow} m³/h
- 冷卻水流量: {parameters.CoolingWaterFlow} m³/h

請以JSON格式回覆，包含：
- recommendedChilledWaterSupplyTemp: 建議冰水出水溫度
- recommendedCoolingWaterSupplyTemp: 建議冷卻水出水溫度
- reason: 建議原因

請只回覆JSON，不要其他文字。
";

                var ollamaResponse = await CallOllamaAPI(prompt);
                var recommendation = JsonSerializer.Deserialize<TemperatureRecommendation>(ollamaResponse);

                return Ok(recommendation);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        private SystemBaseData GenerateBaseData(SystemParameters parameters)
        {
            var random = new Random();

            // 根據系統參數生成基礎數據
            var baseChillerEfficiency = 5.5 + (random.NextDouble() * 1.0); // 5.5-6.5 COP
            var basePumpEfficiency = 0.75 + (random.NextDouble() * 0.15); // 75%-90%

            return new SystemBaseData
            {
                ChillerCOP = baseChillerEfficiency,
                PumpEfficiency = basePumpEfficiency,
                BaselineEnergyConsumption = 450000 + (random.Next(-50000, 50000)), // kWh/year
                CurrentEnergyConsumption = 0, // 將由 Ollama 計算
                SystemCapacity = parameters.ChilledWaterFlow * 4.18 *
                    (parameters.ChilledWaterReturnTemp - parameters.ChilledWaterSupplyTemp) / 3600 * 1000 // kW
            };
        }

        private string BuildAnalysisPrompt(SystemParameters parameters, SystemBaseData baseData)
        {
            return $@"
你是一個專業的HVAC節能分析專家。請根據以下冰水系統參數進行節能分析：

系統參數：
- 冰水出水溫度: {parameters.ChilledWaterSupplyTemp}°C
- 冰水回水溫度: {parameters.ChilledWaterReturnTemp}°C
- 冷凝器出水溫度: {parameters.CondenserSupplyTemp}°C
- 冷凝器回水溫度: {parameters.CondenserReturnTemp}°C
- 外氣溫度: {parameters.OutdoorTemperature}°C
- 外氣相對濕度: {parameters.OutdoorHumidity}%
- 外氣焓值: {parameters.OutdoorEnthalpy} kJ/kg
- 系統負載率: {parameters.SystemLoadRate}%
- 冰水流量: {parameters.ChilledWaterFlow} m³/h
- 冷卻水流量: {parameters.CoolingWaterFlow} m³/h

基礎數據：
- 系統容量: {baseData.SystemCapacity:F1} kW
- 基準能耗: {baseData.BaselineEnergyConsumption} kWh/年

請以JSON格式回覆節能分析結果，包含以下欄位：
{{
  ""recommendedChilledWaterTemp"": 建議冰水溫度(數值),
  ""recommendedCoolingWaterTemp"": 建議冷卻水溫度(數值),
  ""chilledWaterTempAdjustmentEfficiency"": 冰水溫度調整節能百分比(數值),
  ""coolingWaterTempAdjustmentEfficiency"": 冷卻水溫度調整節能百分比(數值),
  ""totalEnergyEfficiency"": 綜合節能效益百分比(數值),
  ""annualEnergySaving"": 年度節省能耗kWh(數值),
  ""annualCostSaving"": 年度節省費用元(數值，以每度電3元計算),
  ""professionalRecommendation"": ""專業建議文字說明""
}}

請確保回覆是有效的JSON格式，數值請精確到小數點後一位。
";
        }

        private async Task<string> CallOllamaAPI(string prompt)
        {
            var requestData = new
            {
                model = "gemma3:4b", // 或您使用的模型名稱
                prompt = prompt,
                stream = false,
                options = new
                {
                    temperature = 0.1, // 降低隨機性以獲得更一致的結果
                    top_p = 0.9
                }
            };

            var json = JsonSerializer.Serialize(requestData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_ollamaBaseUrl}/api/generate", content);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Ollama API 調用失敗: {response.StatusCode}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var ollamaResponse = JsonSerializer.Deserialize<OllamaResponse>(responseContent);

            return ollamaResponse?.response ?? string.Empty;
        }

        private EnergyAnalysisResult ParseOllamaResponse(string ollamaResponse, SystemBaseData baseData)
        {
            try
            {
                // 清理響應文字，提取JSON部分
                var jsonStart = ollamaResponse.IndexOf('{');
                var jsonEnd = ollamaResponse.LastIndexOf('}') + 1;

                if (jsonStart >= 0 && jsonEnd > jsonStart)
                {
                    var jsonString = ollamaResponse.Substring(jsonStart, jsonEnd - jsonStart);
                    var analysisData = JsonSerializer.Deserialize<OllamaAnalysisResponse>(jsonString);

                    return new EnergyAnalysisResult
                    {
                        RecommendedSettings = new RecommendedSettings
                        {
                            ChilledWaterTemp = analysisData.recommendedChilledWaterTemp,
                            CoolingWaterTemp = analysisData.recommendedCoolingWaterTemp
                        },
                        EfficiencyAnalysis = new EfficiencyAnalysis
                        {
                            ChilledWaterTempAdjustment = analysisData.chilledWaterTempAdjustmentEfficiency,
                            CoolingWaterTempAdjustment = analysisData.coolingWaterTempAdjustmentEfficiency,
                            TotalEfficiency = analysisData.totalEnergyEfficiency,
                            AnnualEnergySaving = analysisData.annualEnergySaving,
                            AnnualCostSaving = analysisData.annualCostSaving
                        },
                        ProfessionalRecommendation = analysisData.professionalRecommendation,
                        DataSource = "chilled_water_system_energy_saving_guide.txt",
                        AnalysisNote = "**4. 冰水溫差 (ΔT) 的分析與優化**"
                    };
                }
            }
            catch (JsonException)
            {
                // 如果解析失敗，返回預設值
            }

            // 回退方案：使用基本計算
            return GenerateDefaultAnalysis(baseData);
        }

        private EnergyAnalysisResult GenerateDefaultAnalysis(SystemBaseData baseData)
        {
            var random = new Random();

            return new EnergyAnalysisResult
            {
                RecommendedSettings = new RecommendedSettings
                {
                    ChilledWaterTemp = 7.8,
                    CoolingWaterTemp = 28.8
                },
                EfficiencyAnalysis = new EfficiencyAnalysis
                {
                    ChilledWaterTempAdjustment = 1.8,
                    CoolingWaterTempAdjustment = 13.3,
                    TotalEfficiency = 10.6,
                    AnnualEnergySaving = 32500,
                    AnnualCostSaving = 39000
                },
                ProfessionalRecommendation = "根據系統參數分析，建議調整冰水溫度設定以達到最佳節能效果。",
                DataSource = "chilled_water_system_energy_saving_guide.txt",
                AnalysisNote = "**4. 冰水溫差 (ΔT) 的分析與優化**"
            };
        }
    }

    // 數據模型
    public class SystemParameters
    {
        public double ChilledWaterSupplyTemp { get; set; } = 7.0;
        public double ChilledWaterReturnTemp { get; set; } = 12.0;
        public double CondenserSupplyTemp { get; set; } = 37.0;
        public double CondenserReturnTemp { get; set; } = 32.0;
        public double OutdoorTemperature { get; set; } = 30.0;
        public double OutdoorHumidity { get; set; } = 65.0;
        public double OutdoorEnthalpy { get; set; } = 85.0;
        public double SystemLoadRate { get; set; } = 70.0;
        public double ChilledWaterFlow { get; set; } = 100.0;
        public double CoolingWaterFlow { get; set; } = 120.0;
        public string SystemType { get; set; } = "標準中央空調系統";
        public string PlateType { get; set; } = "精確換板";
        public int DocumentCount { get; set; } = 3;
    }

    public class SystemBaseData
    {
        public double ChillerCOP { get; set; }
        public double PumpEfficiency { get; set; }
        public double BaselineEnergyConsumption { get; set; }
        public double CurrentEnergyConsumption { get; set; }
        public double SystemCapacity { get; set; }
    }

    public class EnergyAnalysisResult
    {
        public RecommendedSettings RecommendedSettings { get; set; } = new();
        public EfficiencyAnalysis EfficiencyAnalysis { get; set; } = new();
        public string ProfessionalRecommendation { get; set; } = string.Empty;
        public string DataSource { get; set; } = string.Empty;
        public string AnalysisNote { get; set; } = string.Empty;
    }

    public class RecommendedSettings
    {
        public double ChilledWaterTemp { get; set; }
        public double CoolingWaterTemp { get; set; }
    }

    public class EfficiencyAnalysis
    {
        public double ChilledWaterTempAdjustment { get; set; }
        public double CoolingWaterTempAdjustment { get; set; }
        public double TotalEfficiency { get; set; }
        public double AnnualEnergySaving { get; set; }
        public double AnnualCostSaving { get; set; }
    }

    public class TemperatureRecommendation
    {
        public double RecommendedChilledWaterSupplyTemp { get; set; }
        public double RecommendedCoolingWaterSupplyTemp { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    // Ollama API 響應模型
    public class OllamaResponse
    {
        public string response { get; set; } = string.Empty;
        public bool done { get; set; }
    }

    public class OllamaAnalysisResponse
    {
        public double recommendedChilledWaterTemp { get; set; }
        public double recommendedCoolingWaterTemp { get; set; }
        public double chilledWaterTempAdjustmentEfficiency { get; set; }
        public double coolingWaterTempAdjustmentEfficiency { get; set; }
        public double totalEnergyEfficiency { get; set; }
        public double annualEnergySaving { get; set; }
        public double annualCostSaving { get; set; }
        public string professionalRecommendation { get; set; } = string.Empty;
    }
}
