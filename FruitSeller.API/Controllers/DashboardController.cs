using FruitSeller.Application.Dtos;
using FruitSeller.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace FruitSeller.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly IReportService _reportService;

    public DashboardController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet]
    public async Task<ActionResult<DashboardDto>> Get()
    {
        return Ok(await _reportService.GetDashboardAsync());
    }
}
