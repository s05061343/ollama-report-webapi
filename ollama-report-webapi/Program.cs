using Microsoft.Extensions.AI;
using Microsoft.Extensions.FileProviders;
using Microsoft.SemanticKernel;
using ollama_report_webapi.Service.OllamaTextEmbedding;

var builder = WebApplication.CreateBuilder(args);

// 添加服務
builder.Services.AddControllers();
builder.Services.AddHttpClient();

// 添加 Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();



//// 建立 Semantic Kernel
//builder.Services.AddSingleton<Kernel>(sp =>
//{
//    var builder = Kernel.CreateBuilder();

//    builder.AddOllamaChatCompletion(
//        modelId: "gemma3:4b",
//        endpoint: new Uri("http://localhost:11434/v1"));

//    return builder.Build();
//});

// 配置 Semantic Kernel
builder.Services.AddSingleton<Kernel>(serviceProvider =>
{
    var configuration = serviceProvider.GetService<IConfiguration>();
    var kernelBuilder = Kernel.CreateBuilder();

    // 添加 OpenAI Chat Completion 服務
    kernelBuilder.AddOllamaChatCompletion(
        modelId: "gemma3:4b",
        endpoint: new Uri("http://localhost:11434/v1")
    );

    // 添加 OpenAI Text Embedding 服務
    kernelBuilder.AddOllamaTextEmbeddingGeneration(
        //modelId: "text-embedding-ada-002",
        modelId: "nomic-embed-text",
        endpoint: new Uri("http://localhost:11434/v1")
    );

    return kernelBuilder.Build();
});

// 註冊自定義服務
builder.Services.AddScoped<IEmbeddingService, EmbeddingService>();
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<IVectorStore, InMemoryVectorStore>();

//啟用 Session 支援
builder.Services.AddDistributedMemoryCache(); // 暫存
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // session 保存 30 分鐘
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddControllers().AddJsonOptions(o =>
{
    o.JsonSerializerOptions.PropertyNamingPolicy = null;
});

var app = builder.Build();

// 配置管道
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSession(); // 記得放在 UseRouting 之前
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
#region FileServer
var fsOptions = new FileServerOptions
{
    RequestPath = "", // 根目錄
    FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot")),
};
fsOptions.StaticFileOptions.OnPrepareResponse = (context) =>
{
    // Disable caching of all static files.
    context.Context.Response.Headers["Cache-Control"] = "no-cache, no-store";
    context.Context.Response.Headers["Pragma"] = "no-cache";
    context.Context.Response.Headers["Expires"] = "-1";
};
app.UseFileServer(fsOptions);
#endregion
app.Run();