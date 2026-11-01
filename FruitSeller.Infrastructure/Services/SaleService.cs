using System.Data;
using FruitSeller.Application.Dtos;
using FruitSeller.Application.Services;
using FruitSeller.Application.Validation;
using FruitSeller.Domain.BusinessRules;
using FruitSeller.Domain.Entities;
using FruitSeller.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FruitSeller.Infrastructure.Services;

public class SaleService : ISaleService
{
    private readonly FruitSellerDbContext _db;

    public SaleService(FruitSellerDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<SaleDto>> GetSalesAsync(DateOnly? fromDate, DateOnly? toDate)
    {
        var query = _db.Sales.AsNoTracking().AsQueryable();

        if (fromDate.HasValue)
        {
            query = query.Where(x => x.SaleDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(x => x.SaleDate <= toDate.Value);
        }

        var sales = await query.OrderByDescending(x => x.SaleDate).ThenByDescending(x => x.SaleId).ToListAsync();
        var shops = await _db.Shops.AsNoTracking().ToDictionaryAsync(x => x.ShopId, x => x.ShopName);
        var fruits = await _db.Fruits.AsNoTracking().ToDictionaryAsync(x => x.FruitId, x => x.FruitName);

        return sales.Select(x => new SaleDto
        {
            SaleId = x.SaleId,
            ShopId = x.ShopId,
            ShopName = shops.TryGetValue(x.ShopId, out var shopName) ? shopName : string.Empty,
            FruitId = x.FruitId,
            FruitName = fruits.TryGetValue(x.FruitId, out var fruitName) ? fruitName : string.Empty,
            BoxQuantity = x.BoxQuantity,
            PricePerBox = x.PricePerBox,
            TotalAmount = x.TotalAmount,
            SaleDate = x.SaleDate,
            CreatedAt = x.CreatedAt
        }).ToList();
    }

    public async Task<SaleDto> CreateSaleAsync(CreateSaleRequest request)
    {
        if (request is null)
        {
            throw new InvalidOperationException(ValidationHelpers.DefaultApiError);
        }

        var shopName = ValidationHelpers.NormalizeText(request.ShopName);
        var fruitName = ValidationHelpers.NormalizeText(request.FruitName);

        if (string.IsNullOrWhiteSpace(shopName) || shopName.Length > 100)
        {
            throw new InvalidOperationException("Shop name is required and must be 100 characters or less.");
        }

        if (string.IsNullOrWhiteSpace(fruitName) || fruitName.Length > 100)
        {
            throw new InvalidOperationException("Fruit name is required and must be 100 characters or less.");
        }

        if (request.BoxQuantity <= 0)
        {
            throw new InvalidOperationException("Box quantity must be greater than zero.");
        }

        if (!BusinessRules.IsValidPaymentAmount(request.PricePerBox))
        {
            throw new InvalidOperationException("Price per box must be greater than zero.");
        }

        if (request.SaleDate > DateOnly.FromDateTime(DateTime.UtcNow))
        {
            throw new InvalidOperationException("Sale date cannot be in the future.");
        }

        var shop = await _db.Shops.AsNoTracking().SingleOrDefaultAsync(x => x.ShopName.ToLower() == shopName.ToLower());
        if (shop is null)
        {
            throw new InvalidOperationException("Shop name does not match an existing shop record.");
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

        var currentStock = await _db.InventoryMovements
            .FromSqlInterpolated($"SELECT * FROM dbo.InventoryMovements WITH (UPDLOCK, HOLDLOCK) WHERE FruitId = {fruit.FruitId}")
            .SumAsync(x => (int?)x.QuantityChange ?? 0);

        if (!BusinessRules.HasSufficientStock(currentStock, request.BoxQuantity))
        {
            throw new InvalidOperationException("The requested sale exceeds the available stock. Negative stock is not allowed for this operation.");
        }

        var sale = new Sale
        {
            ShopId = shop.ShopId,
            FruitId = fruit.FruitId,
            BoxQuantity = request.BoxQuantity,
            PricePerBox = request.PricePerBox,
            SaleDate = request.SaleDate,
            CreatedAt = DateTime.UtcNow
        };

        _db.Sales.Add(sale);
        await _db.SaveChangesAsync();

        var movement = new InventoryMovement
        {
            FruitId = fruit.FruitId,
            MovementType = "Sale",
            QuantityChange = -request.BoxQuantity,
            MovementDate = request.SaleDate,
            SaleId = sale.SaleId,
            CreatedAt = DateTime.UtcNow
        };

        _db.InventoryMovements.Add(movement);
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return new SaleDto
        {
            SaleId = sale.SaleId,
            ShopId = shop.ShopId,
            ShopName = shop.ShopName,
            FruitId = fruit.FruitId,
            FruitName = fruit.FruitName,
            BoxQuantity = sale.BoxQuantity,
            PricePerBox = sale.PricePerBox,
            TotalAmount = sale.TotalAmount,
            SaleDate = sale.SaleDate,
            CreatedAt = sale.CreatedAt
        };
    }
}
