using Microsoft.AspNetCore.Mvc;
using ollama_report_webapi.Model;
using ollama_report_webapi.Service;

namespace ollama_report_webapi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportController : ControllerBase
    {
        private readonly IReportService _reportService;

        public ReportController(IReportService reportService)
        {
            _reportService = reportService;
        }

        [HttpPost("generate")]
        public async Task<ActionResult<ReportResponse>> GenerateReport([FromBody] ReportRequest request)
        {
            try
            {
                // 驗證請求
                if (request.StartDate >= request.EndDate)
                {
                    return BadRequest("開始日期必須早於結束日期");
                }

                if (string.IsNullOrWhiteSpace(request.DataSource))
                {
                    return BadRequest("資料來源不能為空");
                }

                var response = await _reportService.GenerateReportAsync(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("health")]
        public IActionResult HealthCheck()
        {
            return Ok(new { status = "healthy", timestamp = DateTime.Now });
        }
    }
}
