using Microsoft.AspNetCore.Mvc;
using AnalyticsBackend.Services;

namespace AnalyticsBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportsController : ControllerBase
    {
        private readonly ReportsService _svc;
        public ReportsController(ReportsService svc) => _svc = svc;

        
        [HttpGet("sales-vs-consumption")]
        public async Task<IActionResult> SalesVsConsumption(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to)
            => Ok(await _svc.GetSalesVsConsumption(from, to));

        
        [HttpGet("stock-forecast")]
        public async Task<IActionResult> StockForecast(
            [FromQuery] int year = 0,
            [FromQuery] int fromMonth = 7,
            [FromQuery] int? regID = null)
            => Ok(await _svc.GetStockForecast(year == 0 ? DateTime.Now.Year : year, fromMonth, regID));

        
        [HttpGet("stock-in-months")]
        public async Task<IActionResult> StockInMonths([FromQuery] DateTime? asOf)
            => Ok(await _svc.GetStockInMonths(asOf ?? DateTime.Today));

        
        [HttpGet("plan-by-employee")]
        public async Task<IActionResult> PlanByEmployee(
            [FromQuery] int? empID,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to)
            => Ok(await _svc.GetPlanFulfillmentByEmployee(empID, from, to));

       
        [HttpGet("plan-by-macro")]
        public async Task<IActionResult> PlanByMacro(
            [FromQuery] int? areaID,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to)
            => Ok(await _svc.GetPlanFulfillmentByMacroTerritory(areaID, from, to));

        
        [HttpGet("plan-by-region")]
        public async Task<IActionResult> PlanByRegion(
            [FromQuery] int? regID,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to)
            => Ok(await _svc.GetPlanFulfillmentByRegion(regID, from, to));

       
        [HttpGet("forecast-sales")]
        public async Task<IActionResult> ForecastSales(
            [FromQuery] int year = 0,
            [FromQuery] decimal targetStockMonths = 2.1m)
            => Ok(await _svc.GetForecastSalesForTargetStockMonths(
                year == 0 ? DateTime.Now.Year : year,
                targetStockMonths));
    }
}