﻿namespace ollama_report_webapi.Model
{
    public class OllamaOptions
    {
        public double temperature { get; set; } = 0.7;
        public int num_predict { get; set; } = 2000;
    }
}
