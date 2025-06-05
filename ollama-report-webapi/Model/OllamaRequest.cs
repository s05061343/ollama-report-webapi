namespace ollama_report_webapi.Model
{
    public class OllamaRequest
    {
        public string model { get; set; } = "gemma3:4b";
        public string prompt { get; set; } = string.Empty;
        public bool stream { get; set; } = false;
        public OllamaOptions? options { get; set; }
    }
}
