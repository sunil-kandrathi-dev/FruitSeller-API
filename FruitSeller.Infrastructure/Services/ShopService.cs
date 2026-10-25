using FruitSeller.Application.Dtos;
using FruitSeller.Application.Services;
using FruitSeller.Application.Validation;
using FruitSeller.Domain.Entities;
using FruitSeller.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FruitSeller.Infrastructure.Services;

public class ShopService : IShopService
{
    private readonly FruitSellerDbContext _db;

    public ShopService(FruitSellerDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<ShopDto>> GetShopsAsync()
    {
        var shops = await _db.Shops.AsNoTracking().OrderBy(x => x.ShopName).ToListAsync();
        var results = new List<ShopDto>();

        foreach (var shop in shops)
        {
            var salesTotal = await _db.Sales.AsNoTracking().Where(x => x.ShopId == shop.ShopId).SumAsync(x => (decimal?)x.TotalAmount ?? 0m);
            var paymentsTotal = await _db.Payments.AsNoTracking().Where(x => x.ShopId == shop.ShopId).SumAsync(x => (decimal?)x.Amount ?? 0m);

            results.Add(new ShopDto
            {
                ShopId = shop.ShopId,
                ShopName = shop.ShopName,
                OwnerName = shop.OwnerName,
                PhoneNumber = shop.PhoneNumber,
                OpeningBalance = shop.OpeningBalance,
                CurrentOutstandingBalance = shop.OpeningBalance + salesTotal - paymentsTotal,
                CreatedAt = shop.CreatedAt,
                UpdatedAt = shop.UpdatedAt
            });
        }

        return results;
    }

    public async Task<ShopDto> CreateShopAsync(CreateShopRequest request)
    {
        var shopName = ValidationHelpers.NormalizeText(request.ShopName);
        var ownerName = ValidationHelpers.NormalizeText(request.OwnerName);
        var phoneNumber = ValidationHelpers.NormalizeText(request.PhoneNumber);

        if (string.IsNullOrWhiteSpace(shopName) || shopName.Length > 100)
        {
            throw new InvalidOperationException("Shop name is required and must be 100 characters or less.");
        }

        if (string.IsNullOrWhiteSpace(ownerName) || ownerName.Length > 100)
        {
            throw new InvalidOperationException("Owner name is required and must be 100 characters or less.");
        }

        if (!ValidationHelpers.IsValidIndianMobile(phoneNumber))
        {
            throw new InvalidOperationException("Phone number must be 10 digits and the first digit must be 6 to 9.");
        }

        if (await _db.Shops.AnyAsync(x => x.ShopName.ToLower() == shopName.ToLower()))
        {
            throw new InvalidOperationException("A shop with this name already exists.");
        }

        var openingBalance = request.OpeningBalance ?? 0m;
        if (openingBalance < 0)
        {
            throw new InvalidOperationException("Opening balance must be zero or greater.");
        }

        var shop = new Shop
        {
            ShopName = shopName,
            OwnerName = ownerName,
            PhoneNumber = phoneNumber,
            OpeningBalance = openingBalance,
            CreatedAt = DateTime.UtcNow
        };

        _db.Shops.Add(shop);
        await _db.SaveChangesAsync();

        return new ShopDto
        {
            ShopId = shop.ShopId,
            ShopName = shop.ShopName,
            OwnerName = shop.OwnerName,
            PhoneNumber = shop.PhoneNumber,
            OpeningBalance = shop.OpeningBalance,
            CurrentOutstandingBalance = shop.OpeningBalance,
            CreatedAt = shop.CreatedAt,
            UpdatedAt = shop.UpdatedAt
        };
    }
}
