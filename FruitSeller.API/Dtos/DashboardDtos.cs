namespace FruitSeller.API.Dtos;

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
