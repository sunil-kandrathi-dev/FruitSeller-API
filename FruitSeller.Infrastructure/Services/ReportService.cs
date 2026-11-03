using FruitSeller.Application.Dtos;
using FruitSeller.Application.Services;
using FruitSeller.Domain.BusinessRules;
using FruitSeller.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FruitSeller.Infrastructure.Services;

public class ReportService : IReportService
{
    private readonly FruitSellerDbContext _db;

    public ReportService(FruitSellerDbContext db)
    {
        _db = db;
    }

    public async Task<ReportDto> GetReportAsync(DateOnly? fromDate, DateOnly? toDate)
    {
        if (fromDate.HasValue && toDate.HasValue && toDate.Value < fromDate.Value)
        {
            throw new InvalidOperationException("The end date must be on or after the start date.");
        }

        var salesQuery = _db.Sales.AsNoTracking().AsQueryable();
        var purchaseQuery = _db.Purchases.AsNoTracking().AsQueryable();
        var expenseQuery = _db.Expenses.AsNoTracking().AsQueryable();
        var paymentQuery = _db.Payments.AsNoTracking().AsQueryable();

        if (fromDate.HasValue)
        {
            salesQuery = salesQuery.Where(x => x.SaleDate >= fromDate.Value);
            purchaseQuery = purchaseQuery.Where(x => x.PurchaseDate >= fromDate.Value);
            expenseQuery = expenseQuery.Where(x => x.ExpenseDate >= fromDate.Value);
            paymentQuery = paymentQuery.Where(x => x.PaymentDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            salesQuery = salesQuery.Where(x => x.SaleDate <= toDate.Value);
            purchaseQuery = purchaseQuery.Where(x => x.PurchaseDate <= toDate.Value);
            expenseQuery = expenseQuery.Where(x => x.ExpenseDate <= toDate.Value);
            paymentQuery = paymentQuery.Where(x => x.PaymentDate <= toDate.Value);
        }

        var salesSummary = await salesQuery
            .GroupBy(_ => 1)
            .Select(x => new
            {
                Total = x.Sum(y => (decimal?)y.TotalAmount ?? 0m),
                Count = x.Count(),
                BoxCount = x.Sum(y => (int?)y.BoxQuantity ?? 0)
            })
            .FirstOrDefaultAsync() ?? new { Total = 0m, Count = 0, BoxCount = 0 };

        var purchaseSummary = await purchaseQuery
            .GroupBy(_ => 1)
            .Select(x => new
            {
                Total = x.Sum(y => (decimal?)y.TotalCost ?? 0m),
                Count = x.Count(),
                BoxCount = x.Sum(y => (int?)y.BoxQuantity ?? 0)
            })
            .FirstOrDefaultAsync() ?? new { Total = 0m, Count = 0, BoxCount = 0 };

        var expenseSummary = await expenseQuery
            .GroupBy(_ => 1)
            .Select(x => new
            {
                Total = x.Sum(y => (decimal?)y.Amount ?? 0m),
                Count = x.Count()
            })
            .FirstOrDefaultAsync() ?? new { Total = 0m, Count = 0 };

        var paymentSummary = await paymentQuery
            .GroupBy(_ => 1)
            .Select(x => new
            {
                Total = x.Sum(y => (decimal?)y.Amount ?? 0m),
                Count = x.Count()
            })
            .FirstOrDefaultAsync() ?? new { Total = 0m, Count = 0 };

        var comparisonValue = BusinessRules.CalculateBusinessResult(salesSummary.Total, purchaseSummary.Total, expenseSummary.Total);

        return new ReportDto
        {
            SalesTotal = salesSummary.Total,
            SalesCount = salesSummary.Count,
            SalesBoxCount = salesSummary.BoxCount,
            PurchaseTotal = purchaseSummary.Total,
            PurchaseCount = purchaseSummary.Count,
            PurchaseBoxCount = purchaseSummary.BoxCount,
            ExpenseTotal = expenseSummary.Total,
            ExpenseCount = expenseSummary.Count,
            PaymentTotal = paymentSummary.Total,
            PaymentCount = paymentSummary.Count,
            SalesMinusPurchaseCostMinusExpenses = comparisonValue,
            ComparisonNote = "Simple comparison only: Sales - Purchase cost - Expenses. This is not accounting profit."
        };
    }

    public async Task<DashboardDto> GetDashboardAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var todaySalesTotalTask = _db.Sales.AsNoTracking()
            .Where(x => x.SaleDate == today)
            .SumAsync(x => (decimal?)x.TotalAmount ?? 0m);

        var todayBoxesDistributedTask = _db.Sales.AsNoTracking()
            .Where(x => x.SaleDate == today)
            .SumAsync(x => (int?)x.BoxQuantity ?? 0);

        var todayPaymentCollectionsTask = _db.Payments.AsNoTracking()
            .Where(x => x.PaymentDate == today)
            .SumAsync(x => (decimal?)x.Amount ?? 0m);

        var outstandingByShop = await _db.Shops.AsNoTracking()
            .Select(shop => new
            {
                shop.OpeningBalance,
                SalesTotal = _db.Sales.AsNoTracking()
                    .Where(sale => sale.ShopId == shop.ShopId)
                    .Sum(sale => (decimal?)sale.TotalAmount ?? 0m),
                PaymentsTotal = _db.Payments.AsNoTracking()
                    .Where(payment => payment.ShopId == shop.ShopId)
                    .Sum(payment => (decimal?)payment.Amount ?? 0m)
            })
            .ToListAsync();

        var totalOutstandingBalance = outstandingByShop
            .Sum(x => BusinessRules.CalculateOutstanding(x.OpeningBalance, x.SalesTotal, x.PaymentsTotal));

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

        await Task.WhenAll(todaySalesTotalTask, todayBoxesDistributedTask, todayPaymentCollectionsTask);

        return new DashboardDto
        {
            TodaySalesTotal = await todaySalesTotalTask,
            TodayBoxesDistributed = await todayBoxesDistributedTask,
            TodayPaymentCollections = await todayPaymentCollectionsTask,
            TotalOutstandingBalance = totalOutstandingBalance,
            CurrentStockByFruit = stockByFruit
        };
    }
}
