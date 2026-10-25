namespace FruitSeller.Domain.Entities;

public class Purchase
{
    public int PurchaseId { get; set; }
    public int FruitId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public int BoxQuantity { get; set; }
    public decimal TotalCost { get; set; }
    public DateOnly PurchaseDate { get; set; }
    public DateTime CreatedAt { get; set; }
}
