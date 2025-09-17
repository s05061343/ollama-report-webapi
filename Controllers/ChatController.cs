using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using ollama_report_webapi.Service.OllamaTextEmbedding;
using System.Text.Json;

namespace ollama_report_webapi.Controllers
{
    //[Route("api/[controller]")]
    //[ApiController]
    //public class ChatController : ControllerBase
    //{
    //    private readonly Kernel _kernel;
    //    private readonly IChatCompletionService _chatService;
    //    private const string SessionKey = "ChatHistory";

    //    public ChatController(Kernel kernel)
    //    {
    //        _kernel = kernel;
    //        _chatService = _kernel.GetRequiredService<IChatCompletionService>();
    //    }

    //    [HttpGet("ask")]
    //    public async Task<IActionResult> Ask([FromQuery] string question)
    //    {
    //        // 從 Session 取得歷史對話
    //        var history = GetChatHistory();

    //        var prompt = $"""
    //            你是知識型助理，根據以下資訊回答問題：

    //            內容：

    //            問題：{question}
    //            """;


    //        // 新增提問
    //        history.AddUserMessage(question);

    //        var reply = await _chatService.GetChatMessageContentsAsync(history);
    //        var answer = reply[^1].Content;

    //        // 加入回覆到歷史
    //        history.AddAssistantMessage(answer);

    //        // 存回 Session
    //        SaveChatHistory(history);

    //        return Ok(new { answer });
    //    }

    //    [HttpPost("reset")]
    //    public IActionResult Reset()
    //    {
    //        HttpContext.Session.Remove(SessionKey);
    //        return Ok("Chat history cleared.");
    //    }

    //    private ChatHistory GetChatHistory()
    //    {
    //        var json = HttpContext.Session.GetString(SessionKey);
    //        return string.IsNullOrEmpty(json)
    //            ? new ChatHistory()
    //            : JsonSerializer.Deserialize<ChatHistory>(json)!;
    //    }

    //    private void SaveChatHistory(ChatHistory history)
    //    {
    //        var json = JsonSerializer.Serialize(history);
    //        HttpContext.Session.SetString(SessionKey, json);
    //    }
    //}

    public record RAGChatRequest(string Question, int? TopK = 3);
    public record RAGChatResponse(string Answer);

    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly Kernel _kernel;
        private readonly IChatCompletionService _chatService;
        private const string SessionKey = "ChatHistory";
        private readonly IDocumentService _documentService;

        public ChatController(
            Kernel kernel,
            IDocumentService documentService)
        {
            _kernel = kernel;
            _chatService = _kernel.GetRequiredService<IChatCompletionService>();
            _documentService = documentService;
        }

        /// <summary>
        /// RAG 問答
        /// </summary>
        /// <param name="request">問答請求</param>
        /// <returns>回答結果</returns>
        [HttpPost("rag")]
        public async Task<IActionResult> RAGChat([FromBody] RAGChatRequest request)
        {
            try
            {
                var response = await _documentService.ChatWithDocumentsAsync(request.Question, request.TopK ?? 3);
                return Ok(new RAGChatResponse(Answer: response));
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("ask")]
        public async Task<IActionResult> Ask([FromQuery] string question)
        {
            // 從 Session 取得歷史對話
            var history = GetChatHistory();

            var prompt = $"""
                    你是知識型助理，根據以下資訊回答問題：

                    內容：

                    問題：{question}
                    """;


            // 新增提問
            history.AddUserMessage(question);

            var reply = await _chatService.GetChatMessageContentsAsync(history);
            var answer = reply[^1].Content;

            // 加入回覆到歷史
            history.AddAssistantMessage(answer);

            // 存回 Session
            SaveChatHistory(history);

            return Ok(new { answer });
        }

        [HttpPost("reset")]
        public IActionResult Reset()
        {
            HttpContext.Session.Remove(SessionKey);
            return Ok("Chat history cleared.");
        }

        private ChatHistory GetChatHistory()
        {
            var json = HttpContext.Session.GetString(SessionKey);
            return string.IsNullOrEmpty(json)
                ? new ChatHistory()
                : JsonSerializer.Deserialize<ChatHistory>(json)!;
        }

        private void SaveChatHistory(ChatHistory history)
        {
            var json = JsonSerializer.Serialize(history);
            HttpContext.Session.SetString(SessionKey, json);
        }
    }
}
