using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using ollama_report_webapi.Model.DocumentUpload;
using ollama_report_webapi.Model.TextEmbedding;
using System.Text;

namespace ollama_report_webapi.Service.OllamaTextEmbedding
{
    
    // 文件服務實作
    public class DocumentService : IDocumentService
    {
        private readonly IEmbeddingService _embeddingService;
        private readonly IVectorStore _vectorStore;
        private readonly IChatCompletionService _chatService;

        public DocumentService(IEmbeddingService embeddingService, IVectorStore vectorStore, Kernel kernel)
        {
            _embeddingService = embeddingService;
            _vectorStore = vectorStore;
            _chatService = kernel.GetRequiredService<IChatCompletionService>();
        }

        public async Task<DocumentUploadResult> ProcessDocumentAsync(IFormFile file)
        {
            // 讀取文件內容
            using var reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8);
            var content = await reader.ReadToEndAsync();

            // 將文件分割成塊
            var chunks = SplitTextIntoChunks(content, 1000, 200); // 1000字符每塊，200字符重疊

            var documentId = Guid.NewGuid().ToString();
            var documentChunks = new List<DocumentChunk>();

            // 為每個塊生成向量
            for (int i = 0; i < chunks.Count; i++)
            {
                var embedding = await _embeddingService.GenerateEmbeddingAsync(chunks[i]);

                documentChunks.Add(new DocumentChunk
                {
                    DocumentId = documentId,
                    FileName = file.FileName,
                    Content = chunks[i],
                    ChunkIndex = i,
                    Embedding = embedding
                });
            }

            // 存儲到向量數據庫
            await _vectorStore.StoreDocumentChunksAsync(documentChunks);

            return new DocumentUploadResult(
                DocumentId: documentId,
                FileName: file.FileName,
                ChunkCount: chunks.Count,
                Status: "成功處理"
            );
        }

        public async Task<string> ChatWithDocumentsAsync(string question, int topK)
        {
            // 搜尋相關文件片段
            var relevantChunks = await _vectorStore.SearchSimilarAsync(question, topK);

            if (!relevantChunks.Any())
            {
                return "抱歉，我在文件中找不到相關信息來回答您的問題。";
            }

            // 構建上下文
            var context = string.Join("\n\n", relevantChunks.Select(chunk =>
                $"文件：{chunk.FileName}\n內容：{chunk.Content}"));

            // 生成回答
            var prompt = $"""
            根據以下文件內容回答問題：

            上下文：
            {context}

            問題：{question}

            請根據上述文件內容回答問題。如果文件中沒有相關信息，請明確說明。
            回答應該：
            1. 基於提供的文件內容
            2. 準確且有幫助
            3. 使用繁體中文
            4. 如果可能，引用具體的文件片段
            """;

            var response = await _chatService.GetChatMessageContentAsync(prompt);
            return response.Content ?? "無法生成回應";
        }

        private List<string> SplitTextIntoChunks(string text, int chunkSize, int overlap)
        {
            var chunks = new List<string>();
            var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < words.Length; i += chunkSize - overlap)
            {
                var chunk = string.Join(" ", words.Skip(i).Take(chunkSize));
                if (!string.IsNullOrWhiteSpace(chunk))
                {
                    chunks.Add(chunk);
                }

                if (i + chunkSize >= words.Length)
                    break;
            }

            return chunks;
        }
    }
}
