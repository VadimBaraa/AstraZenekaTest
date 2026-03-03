using AnalyticsBackend.Services;
using Microsoft.AspNetCore.Mvc;

namespace AnalyticsBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportsController : ControllerBase
    {
        private readonly ReportsService _service;
        public ReportsController(ReportsService service)
        {
            _service = service;
        }

        [HttpGet("sales-vs-consumption")]
        public async Task<IActionResult> GetSalesVsConsumption(DateTime? from, DateTime? to)
        {
            var data = await _service.GetSalesVsConsumption(from, to);
            return Ok(data);
        }
    }
}