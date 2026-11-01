namespace FruitSeller.Application.Dtos;

public class SaleDto
{
    public int SaleId { get; set; }
    public int ShopId { get; set; }
    public string ShopName { get; set; } = string.Empty;
    public int FruitId { get; set; }
    public string FruitName { get; set; } = string.Empty;
    public int BoxQuantity { get; set; }
    public decimal PricePerBox { get; set; }
    public decimal TotalAmount { get; set; }
    public DateOnly SaleDate { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateSaleRequest
{
    public string ShopName { get; set; } = string.Empty;
    public string FruitName { get; set; } = string.Empty;
    public int BoxQuantity { get; set; }
    public decimal PricePerBox { get; set; }
    public DateOnly SaleDate { get; set; }
}
