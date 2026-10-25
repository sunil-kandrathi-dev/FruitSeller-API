namespace FruitSeller.API.Dtos;

public class PurchaseDto
{
    public int PurchaseId { get; set; }
    public int FruitId { get; set; }
    public string FruitName { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public int BoxQuantity { get; set; }
    public decimal TotalCost { get; set; }
    public DateOnly PurchaseDate { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreatePurchaseRequest
{
    public string FruitName { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public int BoxQuantity { get; set; }
    public decimal TotalCost { get; set; }
    public DateOnly PurchaseDate { get; set; }
}
