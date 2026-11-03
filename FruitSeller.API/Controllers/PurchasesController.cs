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
    private readonly ISupplierPaymentService _supplierPaymentService;

    public PurchasesController(IPurchaseService purchaseService, ISupplierPaymentService supplierPaymentService)
    {
        _purchaseService = purchaseService;
        _supplierPaymentService = supplierPaymentService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PurchaseDto>>> Get([FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate)
    {
        return Ok(await _purchaseService.GetPurchasesAsync(fromDate, toDate));
    }

    [HttpGet("{purchaseId:int}/supplier-payments")]
    public async Task<ActionResult<IEnumerable<SupplierPaymentDto>>> GetSupplierPayments(int purchaseId)
    {
        try
        {
            return Ok(await _supplierPaymentService.GetSupplierPaymentsAsync(purchaseId));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Purchase not found.",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound
            });
        }
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

    [HttpPost("{purchaseId:int}/supplier-payments")]
    public async Task<ActionResult<SupplierPaymentDto>> PostSupplierPayment(int purchaseId, [FromBody] CreateSupplierPaymentRequest request)
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
            var payment = await _supplierPaymentService.CreateSupplierPaymentAsync(purchaseId, request);
            return CreatedAtAction(nameof(GetSupplierPayments), new { purchaseId }, payment);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Purchase not found.",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound
            });
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
