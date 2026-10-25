using FruitSeller.Application.Dtos;
using FruitSeller.Application.Services;
using FruitSeller.Application.Validation;
using Microsoft.AspNetCore.Mvc;

namespace FruitSeller.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FruitsController : ControllerBase
{
    private readonly IFruitService _fruitService;

    public FruitsController(IFruitService fruitService)
    {
        _fruitService = fruitService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<FruitDto>>> Get()
    {
        return Ok(await _fruitService.GetFruitsAsync());
    }

    [HttpPost]
    public async Task<ActionResult<FruitDto>> Post([FromBody] CreateFruitRequest request)
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
            var fruit = await _fruitService.CreateFruitAsync(request);
            return CreatedAtAction(nameof(Get), new { id = fruit.FruitId }, fruit);
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
