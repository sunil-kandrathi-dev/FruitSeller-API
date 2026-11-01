using System.Data;
using FruitSeller.Application.Dtos;
using FruitSeller.Application.Services;
using FruitSeller.Application.Validation;
using FruitSeller.Domain.Entities;
using FruitSeller.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FruitSeller.Infrastructure.Services;

public class PurchaseService : IPurchaseService
{
    private readonly FruitSellerDbContext _db;

    public PurchaseService(FruitSellerDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<PurchaseDto>> GetPurchasesAsync(DateOnly? fromDate, DateOnly? toDate)
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

        return purchases.Select(x => new PurchaseDto
        {
            PurchaseId = x.PurchaseId,
            FruitId = x.FruitId,
            FruitName = fruitNames.TryGetValue(x.FruitId, out var fruitName) ? fruitName : string.Empty,
            SupplierName = x.SupplierName,
            BoxQuantity = x.BoxQuantity,
            TotalCost = x.TotalCost,
            PurchaseDate = x.PurchaseDate,
            CreatedAt = x.CreatedAt
        }).ToList();
    }

    public async Task<PurchaseDto> CreatePurchaseAsync(CreatePurchaseRequest request)
    {
        if (request is null)
        {
            throw new InvalidOperationException(ValidationHelpers.DefaultApiError);
        }

        var fruitName = ValidationHelpers.NormalizeText(request.FruitName);
        var supplierName = ValidationHelpers.NormalizeText(request.SupplierName);

        if (string.IsNullOrWhiteSpace(fruitName) || fruitName.Length > 100)
        {
            throw new InvalidOperationException("Fruit name is required and must be 100 characters or less.");
        }

        if (string.IsNullOrWhiteSpace(supplierName) || supplierName.Length > 100)
        {
            throw new InvalidOperationException("Supplier name is required and must be 100 characters or less.");
        }

        if (request.BoxQuantity <= 0)
        {
            throw new InvalidOperationException("Box quantity must be greater than zero.");
        }

        if (request.TotalCost <= 0m)
        {
            throw new InvalidOperationException("Total cost must be greater than zero.");
        }

        if (request.PurchaseDate > DateOnly.FromDateTime(DateTime.UtcNow))
        {
            throw new InvalidOperationException("Purchase date cannot be in the future.");
        }

        var fruit = await _db.Fruits.AsNoTracking().SingleOrDefaultAsync(x => x.FruitName.ToLower() == fruitName.ToLower());
        if (fruit is null)
        {
            throw new InvalidOperationException("Fruit name does not match an existing fruit record.");
        }

        using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        var lockedFruit = await _db.Fruits
            .FromSqlInterpolated($"SELECT * FROM dbo.Fruits WITH (UPDLOCK, HOLDLOCK) WHERE FruitId = {fruit.FruitId}")
            .SingleAsync();

        var purchase = new Purchase
        {
            FruitId = lockedFruit.FruitId,
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

        return new PurchaseDto
        {
            PurchaseId = purchase.PurchaseId,
            FruitId = fruit.FruitId,
            FruitName = fruit.FruitName,
            SupplierName = purchase.SupplierName,
            BoxQuantity = purchase.BoxQuantity,
            TotalCost = purchase.TotalCost,
            PurchaseDate = purchase.PurchaseDate,
            CreatedAt = purchase.CreatedAt
        };
    }
}
