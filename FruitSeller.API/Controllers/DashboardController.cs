using FruitSeller.API.Dtos;
using FruitSeller.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FruitSeller.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly FruitSellerDbContext _db;

    public DashboardController(FruitSellerDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<DashboardDto>> Get()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var todaySalesTotal = await _db.Sales.AsNoTracking()
            .Where(x => x.SaleDate == today)
            .SumAsync(x => (decimal?)x.TotalAmount ?? 0m);

        var todayBoxesDistributed = await _db.Sales.AsNoTracking()
            .Where(x => x.SaleDate == today)
            .SumAsync(x => (int?)x.BoxQuantity ?? 0);

        var todayPaymentCollections = await _db.Payments.AsNoTracking()
            .Where(x => x.PaymentDate == today)
            .SumAsync(x => (decimal?)x.Amount ?? 0m);

        var shops = await _db.Shops.AsNoTracking().ToListAsync();
        var totalOutstandingBalance = 0m;

        foreach (var shop in shops)
        {
            var salesTotal = await _db.Sales.AsNoTracking()
                .Where(x => x.ShopId == shop.ShopId)
                .SumAsync(x => (decimal?)x.TotalAmount ?? 0m);

            var paymentsTotal = await _db.Payments.AsNoTracking()
                .Where(x => x.ShopId == shop.ShopId)
                .SumAsync(x => (decimal?)x.Amount ?? 0m);

            totalOutstandingBalance += shop.OpeningBalance + salesTotal - paymentsTotal;
        }

        var stockByFruit = await _db.InventoryMovements.AsNoTracking()
            .GroupBy(x => x.FruitId)
            .Select(x => new
            {
                FruitId = x.Key,
                CurrentStock = x.Sum(y => y.QuantityChange)
            })
            .Join(_db.Fruits.AsNoTracking(), x => x.FruitId, f => f.FruitId, (x, f) => new FruitStockSummaryDto
            {
                FruitId = f.FruitId,
                FruitName = f.FruitName,
                CurrentStock = x.CurrentStock
            })
            .OrderBy(x => x.FruitName)
            .ToListAsync();

        return Ok(new DashboardDto
        {
            TodaySalesTotal = todaySalesTotal,
            TodayBoxesDistributed = todayBoxesDistributed,
            TodayPaymentCollections = todayPaymentCollections,
            TotalOutstandingBalance = totalOutstandingBalance,
            CurrentStockByFruit = stockByFruit
        });
    }
}
