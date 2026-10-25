using FruitSeller.Application.Dtos;
using FruitSeller.Application.Services;
using FruitSeller.Application.Validation;
using FruitSeller.Domain.Entities;
using FruitSeller.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FruitSeller.Infrastructure.Services;

public class FruitService : IFruitService
{
    private readonly FruitSellerDbContext _db;

    public FruitService(FruitSellerDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<FruitDto>> GetFruitsAsync()
    {
        var fruits = await _db.Fruits.AsNoTracking().OrderBy(x => x.FruitName).ToListAsync();
        var stockMap = await _db.InventoryMovements.AsNoTracking()
            .GroupBy(x => x.FruitId)
            .Select(x => new { FruitId = x.Key, CurrentStock = x.Sum(y => y.QuantityChange) })
            .ToDictionaryAsync(x => x.FruitId, x => x.CurrentStock);

        return fruits.Select(item => new FruitDto
        {
            FruitId = item.FruitId,
            FruitName = item.FruitName,
            Variety = item.Variety,
            CurrentStock = stockMap.GetValueOrDefault(item.FruitId, 0),
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt
        }).ToList();
    }

    public async Task<FruitDto> CreateFruitAsync(CreateFruitRequest request)
    {
        var fruitName = ValidationHelpers.NormalizeText(request.FruitName);
        var variety = ValidationHelpers.NormalizeText(request.Variety);

        if (string.IsNullOrWhiteSpace(fruitName) || fruitName.Length > 100)
        {
            throw new InvalidOperationException("Fruit name is required and must be 100 characters or less.");
        }

        if (string.IsNullOrWhiteSpace(variety) || variety.Length > 100)
        {
            throw new InvalidOperationException("Variety is required and must be 100 characters or less.");
        }

        if (request.OpeningQuantity <= 0)
        {
            throw new InvalidOperationException("Opening box quantity must be a positive number.");
        }

        if (await _db.Fruits.AnyAsync(x => x.FruitName.ToLower() == fruitName.ToLower()))
        {
            throw new InvalidOperationException("A fruit with this name already exists.");
        }

        using var transaction = await _db.Database.BeginTransactionAsync();

        var fruit = new Fruit
        {
            FruitName = fruitName,
            Variety = variety,
            CreatedAt = DateTime.UtcNow
        };

        _db.Fruits.Add(fruit);
        await _db.SaveChangesAsync();

        var movement = new InventoryMovement
        {
            FruitId = fruit.FruitId,
            MovementType = "Opening",
            QuantityChange = request.OpeningQuantity,
            MovementDate = DateOnly.FromDateTime(DateTime.UtcNow),
            CreatedAt = DateTime.UtcNow
        };

        _db.InventoryMovements.Add(movement);
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return new FruitDto
        {
            FruitId = fruit.FruitId,
            FruitName = fruit.FruitName,
            Variety = fruit.Variety,
            CurrentStock = request.OpeningQuantity,
            CreatedAt = fruit.CreatedAt,
            UpdatedAt = fruit.UpdatedAt
        };
    }
}
