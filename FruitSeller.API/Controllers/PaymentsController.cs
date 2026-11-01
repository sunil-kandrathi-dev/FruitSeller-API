using FruitSeller.Application.Dtos;
using FruitSeller.Application.Services;
using FruitSeller.Application.Validation;
using Microsoft.AspNetCore.Mvc;

namespace FruitSeller.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentsController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PaymentDto>>> Get([FromQuery] int? shopId, [FromQuery] string? method, [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate)
    {
        return Ok(await _paymentService.GetPaymentsAsync(shopId, method, fromDate, toDate));
    }

    [HttpPost]
    public async Task<ActionResult<PaymentDto>> Post([FromBody] CreatePaymentRequest request)
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
            var payment = await _paymentService.CreatePaymentAsync(request);
            return CreatedAtAction(nameof(Get), new { id = payment.PaymentId }, payment);
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
