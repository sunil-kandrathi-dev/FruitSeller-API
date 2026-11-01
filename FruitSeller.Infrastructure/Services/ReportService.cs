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

        var salesTotal = await salesQuery.SumAsync(x => (decimal?)x.TotalAmount ?? 0m);
        var salesCount = await salesQuery.CountAsync();
        var salesBoxCount = await salesQuery.SumAsync(x => (int?)x.BoxQuantity ?? 0);

        var purchaseTotal = await purchaseQuery.SumAsync(x => (decimal?)x.TotalCost ?? 0m);
        var purchaseCount = await purchaseQuery.CountAsync();
        var purchaseBoxCount = await purchaseQuery.SumAsync(x => (int?)x.BoxQuantity ?? 0);

        var expenseTotal = await expenseQuery.SumAsync(x => (decimal?)x.Amount ?? 0m);
        var expenseCount = await expenseQuery.CountAsync();

        var paymentTotal = await paymentQuery.SumAsync(x => (decimal?)x.Amount ?? 0m);
        var paymentCount = await paymentQuery.CountAsync();

        var comparisonValue = BusinessRules.CalculateBusinessResult(salesTotal, purchaseTotal, expenseTotal);

        return new ReportDto
        {
            SalesTotal = salesTotal,
            SalesCount = salesCount,
            SalesBoxCount = salesBoxCount,
            PurchaseTotal = purchaseTotal,
            PurchaseCount = purchaseCount,
            PurchaseBoxCount = purchaseBoxCount,
            ExpenseTotal = expenseTotal,
            ExpenseCount = expenseCount,
            PaymentTotal = paymentTotal,
            PaymentCount = paymentCount,
            SalesMinusPurchaseCostMinusExpenses = comparisonValue,
            ComparisonNote = "Simple comparison only: Sales - Purchase cost - Expenses. This is not accounting profit."
        };
    }

    public async Task<DashboardDto> GetDashboardAsync()
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
        decimal totalOutstandingBalance = 0m;

        foreach (var shop in shops)
        {
            var salesTotal = await _db.Sales.AsNoTracking()
                .Where(x => x.ShopId == shop.ShopId)
                .SumAsync(x => (decimal?)x.TotalAmount ?? 0m);

            var paymentsTotal = await _db.Payments.AsNoTracking()
                .Where(x => x.ShopId == shop.ShopId)
                .SumAsync(x => (decimal?)x.Amount ?? 0m);

            totalOutstandingBalance += BusinessRules.CalculateOutstanding(shop.OpeningBalance, salesTotal, paymentsTotal);
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

        return new DashboardDto
        {
            TodaySalesTotal = todaySalesTotal,
            TodayBoxesDistributed = todayBoxesDistributed,
            TodayPaymentCollections = todayPaymentCollections,
            TotalOutstandingBalance = totalOutstandingBalance,
            CurrentStockByFruit = stockByFruit
        };
    }
}
