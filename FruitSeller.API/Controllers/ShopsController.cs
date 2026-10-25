using FruitSeller.Application.Dtos;
using FruitSeller.Application.Services;
using FruitSeller.Application.Validation;
using Microsoft.AspNetCore.Mvc;

namespace FruitSeller.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ShopsController : ControllerBase
{
    private readonly IShopService _shopService;

    public ShopsController(IShopService shopService)
    {
        _shopService = shopService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ShopDto>>> Get()
    {
        return Ok(await _shopService.GetShopsAsync());
    }

    [HttpPost]
    public async Task<ActionResult<ShopDto>> Post([FromBody] CreateShopRequest request)
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
            var shop = await _shopService.CreateShopAsync(request);
            return CreatedAtAction(nameof(Get), new { id = shop.ShopId }, shop);
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
