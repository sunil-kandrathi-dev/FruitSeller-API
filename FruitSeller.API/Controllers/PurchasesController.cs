using FruitSeller.Application.Dtos;
using FruitSeller.Application.Services;
using FruitSeller.Application.Validation;
using Microsoft.AspNetCore.Mvc;

namespace FruitSeller.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PurchasesController : ControllerBase
{
    private readonly IPurchaseService _purchaseService;

    public PurchasesController(IPurchaseService purchaseService)
    {
        _purchaseService = purchaseService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PurchaseDto>>> Get([FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate)
    {
        return Ok(await _purchaseService.GetPurchasesAsync(fromDate, toDate));
    }

    [HttpPost]
    public async Task<ActionResult<PurchaseDto>> Post([FromBody] CreatePurchaseRequest request)
    {
        if (request is null)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["request"] = [ValidationHelpers.DefaultApiError]
            }));
        }

        try
        {
            var purchase = await _purchaseService.CreatePurchaseAsync(request);
            return CreatedAtAction(nameof(Get), new { id = purchase.PurchaseId }, purchase);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["request"] = [ex.Message]
            }));
        }
    }
}
