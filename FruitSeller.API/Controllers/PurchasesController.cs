using FruitSeller.API.Dtos;
using FruitSeller.API.Utilities;
using FruitSeller.Domain.Entities;
using FruitSeller.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FruitSeller.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PurchasesController : ControllerBase
{
    private readonly FruitSellerDbContext _db;

    public PurchasesController(FruitSellerDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PurchaseDto>>> Get([FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate)
    {
        var query = _db.Purchases.AsNoTracking().AsQueryable();

        if (fromDate.HasValue)
        {
            query = query.Where(x => x.PurchaseDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(x => x.PurchaseDate <= toDate.Value);
        }

        var purchases = await query.OrderByDescending(x => x.PurchaseDate).ThenByDescending(x => x.PurchaseId).ToListAsync();
        var fruitNames = await _db.Fruits.AsNoTracking().ToDictionaryAsync(x => x.FruitId, x => x.FruitName);

        return Ok(purchases.Select(x => new PurchaseDto
        {
            PurchaseId = x.PurchaseId,
            FruitId = x.FruitId,
            FruitName = fruitNames.TryGetValue(x.FruitId, out var fruitName) ? fruitName : string.Empty,
            SupplierName = x.SupplierName,
            BoxQuantity = x.BoxQuantity,
            TotalCost = x.TotalCost,
            PurchaseDate = x.PurchaseDate,
            CreatedAt = x.CreatedAt
        }));
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

        var fruitName = ValidationHelpers.NormalizeText(request.FruitName);
        var supplierName = ValidationHelpers.NormalizeText(request.SupplierName);

        if (string.IsNullOrWhiteSpace(fruitName) || fruitName.Length > 100)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["fruitName"] = ["Fruit name is required and must be 100 characters or less."]
            }));
        }

        if (string.IsNullOrWhiteSpace(supplierName) || supplierName.Length > 100)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["supplierName"] = ["Supplier name is required and must be 100 characters or less."]
            }));
        }

        if (request.BoxQuantity <= 0)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["boxQuantity"] = ["Box quantity must be greater than zero."]
            }));
        }

        if (request.TotalCost <= 0m)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["totalCost"] = ["Total cost must be greater than zero."]
            }));
        }

        if (request.PurchaseDate > DateOnly.FromDateTime(DateTime.UtcNow))
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["purchaseDate"] = ["Purchase date cannot be in the future."]
            }));
        }

        var fruit = await _db.Fruits.AsNoTracking().SingleOrDefaultAsync(x => x.FruitName.ToLower() == fruitName.ToLower());
        if (fruit is null)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["fruitName"] = ["Fruit name does not match an existing fruit record."]
            }));
        }

        using var transaction = await _db.Database.BeginTransactionAsync();

        var purchase = new Purchase
        {
            FruitId = fruit.FruitId,
            SupplierName = supplierName,
            BoxQuantity = request.BoxQuantity,
            TotalCost = request.TotalCost,
            PurchaseDate = request.PurchaseDate,
            CreatedAt = DateTime.UtcNow
        };

        _db.Purchases.Add(purchase);
        await _db.SaveChangesAsync();

        var movement = new InventoryMovement
        {
            FruitId = fruit.FruitId,
            MovementType = "Purchase",
            QuantityChange = request.BoxQuantity,
            MovementDate = request.PurchaseDate,
            PurchaseId = purchase.PurchaseId,
            CreatedAt = DateTime.UtcNow
        };

        _db.InventoryMovements.Add(movement);
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return CreatedAtAction(nameof(Get), new { id = purchase.PurchaseId }, new PurchaseDto
        {
            PurchaseId = purchase.PurchaseId,
            FruitId = fruit.FruitId,
            FruitName = fruit.FruitName,
            SupplierName = purchase.SupplierName,
            BoxQuantity = purchase.BoxQuantity,
            TotalCost = purchase.TotalCost,
            PurchaseDate = purchase.PurchaseDate,
            CreatedAt = purchase.CreatedAt
        });
    }
}
