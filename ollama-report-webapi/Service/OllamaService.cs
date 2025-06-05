using System.Text.Json;
using System.Text;
using ollama_report_webapi.Model;
using Microsoft.Extensions.Configuration;

namespace ollama_report_webapi.Service
{
    public class OllamaService : IOllamaService
    {
        private readonly HttpClient _httpClient;
        private readonly string _ollamaBaseUrl;
        private readonly IConfiguration _configuration;

        public OllamaService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _ollamaBaseUrl = _configuration.GetValue<string>("Ollama:BaseUrl") ?? "http://localhost:11434";
        }

        public async Task<string> GenerateReportAsync(string prompt)
        {
            try
            {
                var request = new OllamaRequest
                {
                    model = _configuration.GetValue<string>("Ollama:DefaultModel") ?? "gemma3:4b", // 或您偏好的模型
                    prompt = prompt,
                    stream = false,
                    options = new OllamaOptions
                    {
                        temperature = 0.3, // 較低的溫度以獲得更一致的報表格式
                        num_predict = 2000
                    }
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_ollamaBaseUrl}/api/generate", content);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                var ollamaResponse = JsonSerializer.Deserialize<OllamaResponse>(responseContent);

                return ollamaResponse?.response ?? "無法生成報表內容";
            }
            catch (Exception ex)
            {
                throw new Exception($"Ollama API 調用失敗: {ex.Message}", ex);
            }
        }
    }
}
