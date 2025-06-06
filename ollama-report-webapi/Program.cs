using Microsoft.Extensions.FileProviders;
using ollama_report_webapi.Service;

var builder = WebApplication.CreateBuilder(args);

// �K�[�A��
builder.Services.AddControllers();
builder.Services.AddHttpClient();
builder.Services.AddScoped<IDataService, DataService>();
builder.Services.AddScoped<IOllamaService, OllamaService>();
builder.Services.AddScoped<IReportService, ReportService>();

// �K�[ Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// �t�m�޹D
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

#region FileServer
var fsOptions = new FileServerOptions
{
    RequestPath = "", // �ڥؿ�
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