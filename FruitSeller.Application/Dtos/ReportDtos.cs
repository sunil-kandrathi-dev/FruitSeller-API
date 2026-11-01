namespace FruitSeller.Application.Dtos;

public class ReportDto
{
    public decimal SalesTotal { get; set; }
    public int SalesCount { get; set; }
    public int SalesBoxCount { get; set; }
    public decimal PurchaseTotal { get; set; }
    public int PurchaseCount { get; set; }
    public int PurchaseBoxCount { get; set; }
    public decimal ExpenseTotal { get; set; }
    public int ExpenseCount { get; set; }
    public decimal PaymentTotal { get; set; }
    public int PaymentCount { get; set; }
    public decimal SalesMinusPurchaseCostMinusExpenses { get; set; }
    public string ComparisonNote { get; set; } = "Simple comparison only: Sales - Purchase cost - Expenses. This is not accounting profit.";
}

public class DashboardDto
{
    public decimal TodaySalesTotal { get; set; }
    public int TodayBoxesDistributed { get; set; }
    public decimal TodayPaymentCollections { get; set; }
    public decimal TotalOutstandingBalance { get; set; }
    public List<FruitStockSummaryDto> CurrentStockByFruit { get; set; } = [];
}

public class FruitStockSummaryDto
{
    public int FruitId { get; set; }
    public string FruitName { get; set; } = string.Empty;
    public int CurrentStock { get; set; }
}
