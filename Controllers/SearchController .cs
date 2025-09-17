using Microsoft.AspNetCore.Mvc;
using ollama_report_webapi.Model.TextEmbedding;
using ollama_report_webapi.Service.OllamaTextEmbedding;

namespace ollama_report_webapi.Controllers
{
    public record SimilaritySearchRequest(string Query, int? TopK = 5);
    public record SimilaritySearchResponse(List<DocumentChunk> Results);

    [ApiController]
    [Route("api/[controller]")]
    public class SearchController : ControllerBase
    {
        private readonly IVectorStore _vectorStore;

        public SearchController(IVectorStore vectorStore)
        {
            _vectorStore = vectorStore;
        }

        /// <summary>
        /// 向量相似度搜尋
        /// </summary>
        /// <param name="request">搜尋請求</param>
        /// <returns>搜尋結果</returns>
        [HttpPost("similarity")]
        public async Task<IActionResult> SimilaritySearch([FromBody] SimilaritySearchRequest request)
        {
            try
            {
                var results = await _vectorStore.SearchSimilarAsync(request.Query, request.TopK ?? 5);
                return Ok(new SimilaritySearchResponse(Results: results));
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }
    }
}
