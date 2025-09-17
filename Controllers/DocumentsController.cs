using Microsoft.AspNetCore.Mvc;
using ollama_report_webapi.Service.OllamaTextEmbedding;

namespace ollama_report_webapi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DocumentsController : ControllerBase
    {
        private readonly IDocumentService _documentService;
        private readonly IVectorStore _vectorStore;

        public DocumentsController(IDocumentService documentService, IVectorStore vectorStore)
        {
            _documentService = documentService;
            _vectorStore = vectorStore;
        }

        /// <summary>
        /// 上傳文件並進行向量化處理
        /// </summary>
        /// <param name="file">要上傳的文件</param>
        /// <returns>處理結果</returns>
        [HttpPost("upload")]
        public async Task<IActionResult> UploadDocument(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest(new { Error = "請選擇一個文件" });

                var result = await _documentService.ProcessDocumentAsync(file);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        /// <summary>
        /// 獲取所有文件
        /// </summary>
        /// <returns>所有文件清單</returns>
        [HttpGet]
        public async Task<IActionResult> GetAllDocuments()
        {
            try
            {
                var documents = await _vectorStore.GetAllDocumentsAsync();
                return Ok(documents);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        /// <summary>
        /// 刪除文件
        /// </summary>
        /// <param name="id">文件ID</param>
        /// <returns>刪除結果</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDocument(string id)
        {
            try
            {
                await _vectorStore.DeleteDocumentAsync(id);
                return Ok(new { Message = "文件已刪除" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }
    }
}
