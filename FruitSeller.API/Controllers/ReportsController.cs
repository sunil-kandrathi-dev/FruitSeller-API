using FruitSeller.Application.Dtos;
using FruitSeller.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace FruitSeller.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet]
    public async Task<ActionResult<ReportDto>> Get([FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate)
    {
        try
        {
            return Ok(await _reportService.GetReportAsync(fromDate, toDate));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["dateRange"] = [ex.Message]
            }));
        }
    }
}
