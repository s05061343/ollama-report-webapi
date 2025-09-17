using Microsoft.AspNetCore.Mvc;
using ollama_report_webapi.Service.OllamaTextEmbedding;

namespace ollama_report_webapi.Controllers
{
    public record EmbeddingRequest(string Text);
    public record EmbeddingResponse(string Text, float[] Embedding, int Dimension);

    [ApiController]
    [Route("api/[controller]")]
    public class EmbeddingsController : ControllerBase
    {
        private readonly IEmbeddingService _embeddingService;

        public EmbeddingsController(IEmbeddingService embeddingService)
        {
            _embeddingService = embeddingService;
        }

        /// <summary>
        /// 生成文本向量
        /// </summary>
        /// <param name="request">文本請求</param>
        /// <returns>向量結果</returns>
        [HttpPost("generate")]
        public async Task<IActionResult> GenerateEmbedding([FromBody] EmbeddingRequest request)
        {
            try
            {
                var embedding = await _embeddingService.GenerateEmbeddingAsync(request.Text);
                return Ok(new EmbeddingResponse(
                    Text: request.Text,
                    Embedding: embedding,
                    Dimension: embedding.Length));
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }
    }
}
