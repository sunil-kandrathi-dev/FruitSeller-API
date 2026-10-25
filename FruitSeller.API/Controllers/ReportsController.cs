using FruitSeller.API.Dtos;
using FruitSeller.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FruitSeller.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly FruitSellerDbContext _db;

    public ReportsController(FruitSellerDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<ReportDto>> Get([FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate)
    {
        if (fromDate.HasValue && toDate.HasValue && toDate.Value < fromDate.Value)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["dateRange"] = ["The end date must be on or after the start date."]
            }));
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

        var comparisonValue = salesTotal - purchaseTotal - expenseTotal;

        return Ok(new ReportDto
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
        });
    }
}
