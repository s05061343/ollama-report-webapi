# Semantic Kernel .NET 8 文件向量化 Web API 完整指南

## 概述

本指南展示如何使用 Microsoft Semantic Kernel 在 .NET 8 Web API 中實現文件向量化（embedding）功能，包含文件上傳、向量生成、語義搜尋和 RAG（檢索增強生成）問答系統。

## 主要功能

- 📄 **文件上傳與處理** - 支援文件上傳並自動分割成文本塊
- 🔢 **向量生成** - 使用 OpenAI Embedding API 將文本轉換為向量
- 🔍 **語義搜尋** - 基於餘弦相似度的智能搜尋
- 🤖 **RAG 問答** - 結合檢索和生成的文件問答功能
- 💾 **向量存儲** - 內建記憶體存儲（可擴展至其他向量數據庫）

## 系統架構

```
用戶請求 → Web API → Semantic Kernel → OpenAI API
                ↓
          向量存儲 ← 文本分割 ← 文件處理
```

## 環境設置

### 1. 建立專案

```bash
dotnet new webapi -n SemanticKernelVectorAPI
cd SemanticKernelVectorAPI
```

### 2. 安裝 NuGet 套件

```bash
dotnet add package Microsoft.SemanticKernel
dotnet add package Microsoft.SemanticKernel.Connectors.OpenAI
```

### 3. 配置 appsettings.json

```json
{
  "OpenAI": {
    "ApiKey": "your-openai-api-key-here"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

## 核心程式碼

### Program.cs 主要配置

```csharp
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.Connectors.OpenAI;

var builder = WebApplication.CreateBuilder(args);

// 基本服務配置
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors();

// Semantic Kernel 配置
builder.Services.AddSingleton<Kernel>(serviceProvider =>
{
    var configuration = serviceProvider.GetService<IConfiguration>();
    var kernelBuilder = Kernel.CreateBuilder();
    
    // 添加 OpenAI 服務
    kernelBuilder.AddOpenAIChatCompletion(
        modelId: "gpt-3.5-turbo",
        apiKey: configuration["OpenAI:ApiKey"]
    );
    
    kernelBuilder.AddOpenAITextEmbeddingGeneration(
        modelId: "text-embedding-ada-002",
        apiKey: configuration["OpenAI:ApiKey"]
    );
    
    return kernelBuilder.Build();
});

// 註冊自定義服務
builder.Services.AddScoped<IEmbeddingService, EmbeddingService>();
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddSingleton<IVectorStore, InMemoryVectorStore>();
```

### 資料模型定義

```csharp
// 文件塊模型
public class DocumentChunk
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string DocumentId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int ChunkIndex { get; set; }
    public float[]? Embedding { get; set; }
    public float SimilarityScore { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// API 請求/回應模型
public record EmbeddingRequest(string Text);
public record EmbeddingResponse(string Text, float[] Embedding, int Dimension);
public record SimilaritySearchRequest(string Query, int? TopK = 5);
public record RAGChatRequest(string Question, int? TopK = 3);
public record DocumentUploadResult(string DocumentId, string FileName, int ChunkCount, string Status);
```

### 向量服務實作

```csharp
public interface IEmbeddingService
{
    Task<float[]> GenerateEmbeddingAsync(string text);
    Task<List<float[]>> GenerateEmbeddingsAsync(List<string> texts);
    Task<float> CalculateSimilarityAsync(float[] embedding1, float[] embedding2);
}

public class EmbeddingService : IEmbeddingService
{
    private readonly ITextEmbeddingGenerationService _embeddingService;

    public EmbeddingService(Kernel kernel)
    {
        _embeddingService = kernel.GetRequiredService<ITextEmbeddingGenerationService>();
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text)
    {
        var embedding = await _embeddingService.GenerateEmbeddingAsync(text);
        return embedding.ToArray();
    }

    public Task<float> CalculateSimilarityAsync(float[] embedding1, float[] embedding2)
    {
        // 餘弦相似度計算
        var dotProduct = embedding1.Zip(embedding2, (a, b) => a * b).Sum();
        var magnitude1 = Math.Sqrt(embedding1.Sum(x => x * x));
        var magnitude2 = Math.Sqrt(embedding2.Sum(x => x * x));
        
        var similarity = (float)(dotProduct / (magnitude1 * magnitude2));
        return Task.FromResult(similarity);
    }
}
```

### 向量存儲實作

```csharp
public interface IVectorStore
{
    Task StoreDocumentChunksAsync(List<DocumentChunk> chunks);
    Task<List<DocumentChunk>> SearchSimilarAsync(string query, int topK);
    Task<List<DocumentChunk>> GetAllDocumentsAsync();
    Task DeleteDocumentAsync(string documentId);
}

public class InMemoryVectorStore : IVectorStore
{
    private readonly List<DocumentChunk> _documents = new();
    private readonly IEmbeddingService _embeddingService;

    public async Task<List<DocumentChunk>> SearchSimilarAsync(string query, int topK)
    {
        var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(query);
        var results = new List<DocumentChunk>();
        
        foreach (var doc in _documents.Where(d => d.Embedding != null))
        {
            var similarity = await _embeddingService.CalculateSimilarityAsync(
                queryEmbedding, doc.Embedding!);
            doc.SimilarityScore = similarity;
            results.Add(doc);
        }
        
        return results
            .OrderByDescending(x => x.SimilarityScore)
            .Take(topK)
            .ToList();
    }
}
```

### 文件處理服務

```csharp
public class DocumentService : IDocumentService
{
    public async Task<DocumentUploadResult> ProcessDocumentAsync(IFormFile file)
    {
        // 讀取文件內容
        using var reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8);
        var content = await reader.ReadToEndAsync();
        
        // 文件分割（1000字符每塊，200字符重疊）
        var chunks = SplitTextIntoChunks(content, 1000, 200);
        
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
        
        await _vectorStore.StoreDocumentChunksAsync(documentChunks);
        
        return new DocumentUploadResult(documentId, file.FileName, chunks.Count, "成功處理");
    }

    private List<string> SplitTextIntoChunks(string text, int chunkSize, int overlap)
    {
        var chunks = new List<string>();
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        for (int i = 0; i < words.Length; i += chunkSize - overlap)
        {
            var chunk = string.Join(" ", words.Skip(i).Take(chunkSize));
            if (!string.IsNullOrWhiteSpace(chunk))
                chunks.Add(chunk);
            
            if (i + chunkSize >= words.Length) break;
        }
        
        return chunks;
    }
}
```

## API 端點說明

### 1. 文件上傳與向量化

**端點**: `POST /api/documents/upload`

**功能**: 上傳文件並自動轉換為向量

**請求**: Multipart form data 包含文件

**回應**:
```json
{
  "documentId": "guid",
  "fileName": "document.txt",
  "chunkCount": 15,
  "status": "成功處理"
}
```

### 2. 文本向量生成

**端點**: `POST /api/embeddings/generate`

**功能**: 將文本轉換為向量

**請求**:
```json
{
  "text": "要轉換的文本內容"
}
```

**回應**:
```json
{
  "text": "要轉換的文本內容",
  "embedding": [0.1, 0.2, ...],
  "dimension": 1536
}
```

### 3. 語義相似度搜尋

**端點**: `POST /api/search/similarity`

**功能**: 搜尋與查詢相似的文件片段

**請求**:
```json
{
  "query": "搜尋關鍵詞",
  "topK": 5
}
```

**回應**:
```json
{
  "results": [
    {
      "id": "chunk-id",
      "content": "相關文本內容",
      "fileName": "source.txt",
      "similarityScore": 0.95
    }
  ]
}
```

### 4. RAG 智能問答

**端點**: `POST /api/chat/rag`

**功能**: 基於上傳的文件回答問題

**請求**:
```json
{
  "question": "請問文件中提到什麼重要概念？",
  "topK": 3
}
```

**回應**:
```json
{
  "answer": "根據文件內容，主要提到了以下重要概念..."
}
```

## 使用範例

### 1. 上傳文件

```bash
curl -X POST "https://localhost:7000/api/documents/upload" \
  -H "Content-Type: multipart/form-data" \
  -F "file=@document.txt"
```

### 2. 生成文本向量

```bash
curl -X POST "https://localhost:7000/api/embeddings/generate" \
  -H "Content-Type: application/json" \
  -d '{"text": "人工智慧是未來科技發展的重要方向"}'
```

### 3. 語義搜尋

```bash
curl -X POST "https://localhost:7000/api/search/similarity" \
  -H "Content-Type: application/json" \
  -d '{"query": "人工智慧應用", "topK": 5}'
```

### 4. 文件問答

```bash
curl -X POST "https://localhost:7000/api/chat/rag" \
  -H "Content-Type: application/json" \
  -d '{"question": "文件中提到了哪些關於AI的內容？", "topK": 3}'
```

## 進階配置與優化

### 1. 替換向量數據庫

可以將 `InMemoryVectorStore` 替換為專業的向量數據庫：

- **Pinecone**: 雲端向量數據庫
- **Weaviate**: 開源向量搜尋引擎
- **Qdrant**: 高性能向量數據庫
- **Chroma**: 輕量級向量數據庫

### 2. 文本分割策略優化

```csharp
// 智能分割（按段落）
private List<string> SmartSplitText(string text)
{
    var paragraphs = text.Split(new[] { "\n\n", "\r\n\r\n" }, 
        StringSplitOptions.RemoveEmptyEntries);
    
    var chunks = new List<string>();
    var currentChunk = "";
    
    foreach (var paragraph in paragraphs)
    {
        if (currentChunk.Length + paragraph.Length < 1000)
        {
            currentChunk += paragraph + "\n\n";
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(currentChunk))
                chunks.Add(currentChunk.Trim());
            currentChunk = paragraph + "\n\n";
        }
    }
    
    if (!string.IsNullOrWhiteSpace(currentChunk))
        chunks.Add(currentChunk.Trim());
    
    return chunks;
}
```

### 3. 快取機制

```csharp
// 添加記憶體快取
builder.Services.AddMemoryCache();

// 在服務中使用快取
public class CachedEmbeddingService : IEmbeddingService
{
    private readonly IEmbeddingService _baseService;
    private readonly IMemoryCache _cache;
    
    public async Task<float[]> GenerateEmbeddingAsync(string text)
    {
        var cacheKey = $"embedding_{text.GetHashCode()}";
        
        if (_cache.TryGetValue(cacheKey, out float[]? cached))
            return cached!;
        
        var embedding = await _baseService.GenerateEmbeddingAsync(text);
        _cache.Set(cacheKey, embedding, TimeSpan.FromHours(24));
        
        return embedding;
    }
}
```

### 4. 批次處理優化

```csharp
public async Task<List<float[]>> GenerateEmbeddingsBatchAsync(List<string> texts)
{
    const int batchSize = 10;
    var embeddings = new List<float[]>();
    
    for (int i = 0; i < texts.Count; i += batchSize)
    {
        var batch = texts.Skip(i).Take(batchSize).ToList();
        var batchTasks = batch.Select(GenerateEmbeddingAsync);
        var batchResults = await Task.WhenAll(batchTasks);
        embeddings.AddRange(batchResults);
        
        // 避免 API 限制
        await Task.Delay(100);
    }
    
    return embeddings;
}
```

## 安全性考量

### 1. API 金鑰保護

```csharp
// 使用 Azure Key Vault 或環境變數
builder.Services.Configure<OpenAIOptions>(options =>
{
    options.ApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") 
                     ?? builder.Configuration["OpenAI:ApiKey"];
});
```

### 2. 文件大小限制

```csharp
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 52428800; // 50MB
});
```

### 3. 輸入驗證

```csharp
public class DocumentUploadValidator
{
    private readonly string[] _allowedExtensions = { ".txt", ".md", ".docx", ".pdf" };
    
    public bool IsValidFile(IFormFile file)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        return _allowedExtensions.Contains(extension) && file.Length > 0;
    }
}
```

## 監控與日誌

### 1. 結構化日誌

```csharp
builder.Services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.AddApplicationInsights(); // 如果使用 Azure
});

// 在服務中使用
public class DocumentService
{
    private readonly ILogger<DocumentService> _logger;
    
    public async Task<DocumentUploadResult> ProcessDocumentAsync(IFormFile file)
    {
        _logger.LogInformation("開始處理文件: {FileName}, 大小: {FileSize}", 
            file.FileName, file.Length);
        
        // 處理邏輯...
        
        _logger.LogInformation("文件處理完成: {DocumentId}, 塊數: {ChunkCount}", 
            documentId, chunks.Count);
    }
}
```

### 2. 效能監控

```csharp
public class PerformanceMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var stopwatch = Stopwatch.StartNew();
        
        await next(context);
        
        stopwatch.Stop();
        
        if (stopwatch.ElapsedMilliseconds > 1000) // 超過1秒記錄
        {
            var logger = context.RequestServices.GetRequiredService<ILogger<PerformanceMiddleware>>();
            logger.LogWarning("緩慢請求: {Path} 耗時 {ElapsedMilliseconds}ms", 
                context.Request.Path, stopwatch.ElapsedMilliseconds);
        }
    }
}
```

## 部署建議

### 1. Docker 容器化

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["SemanticKernelVectorAPI.csproj", "."]
RUN dotnet restore
COPY . .
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SemanticKernelVectorAPI.dll"]
```

### 2. 雲端部署選項

- **Azure App Service**: 簡單的 PaaS 部署
- **Azure Container Apps**: 容器化微服務
- **AWS ECS/Fargate**: Amazon 容器服務
- **Google Cloud Run**: 無伺服器容器平台

## 常見問題與解決方案

### Q1: 向量維度不匹配錯誤

**解決方案**: 確保所有向量都使用相同的 embedding 模型

```csharp
// 驗證向量維度
private void ValidateEmbeddingDimension(float[] embedding)
{
    const int expectedDimension = 1536; // text-embedding-ada-002
    if (embedding.Length != expectedDimension)
    {
        throw new InvalidOperationException(
            $"向量維度不匹配: 期望 {expectedDimension}, 實際 {embedding.Length}");
    }
}
```

### Q2: OpenAI API 配額限制

**解決方案**: 實作重試機制和速率限制

```csharp
public class RateLimitedEmbeddingService : IEmbeddingService
{
    private readonly SemaphoreSlim _semaphore = new(5, 5); // 限制並發數
    
    public async Task<float[]> GenerateEmbeddingAsync(string text)
    {
        await _semaphore.WaitAsync();
        try
        {
            return await _baseService.GenerateEmbeddingAsync(text);
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
```

### Q3: 記憶體使用過高

**解決方案**: 實作分頁和懶載入

```csharp
public async Task<PagedResult<DocumentChunk>> GetDocumentsPaged(int page, int pageSize)
{
    var skip = (page - 1) * pageSize;
    var documents = _documents.Skip(skip).Take(pageSize).ToList();
    
    return new PagedResult<DocumentChunk>
    {
        Data = documents,
        TotalCount = _documents.Count,
        Page = page,
        PageSize = pageSize
    };
}
```

## 結論

這個 Semantic Kernel 文件向量化系統提供了完整的文件處理、向量生成和智能搜尋功能。通過模組化的設計，可以輕鬆擴展和客製化以滿足不同的業務需求。

關鍵優勢：
- 🚀 **高效能**: 批次處理和快取機制
- 🔧 **可擴展**: 模組化架構易於擴展
- 🛡️ **安全性**: 輸入驗證和 API 金鑰保護
- 📊 **可監控**: 完整的日誌和效能監控
- 🐳 **可部署**: 支援容器化和雲端部署

開始使用這個系統來建構您的智能文件處理應用吧！