namespace FruitSeller.Domain.Entities;

public class InventoryMovement
{
    public long InventoryMovementId { get; set; }
    public int FruitId { get; set; }
    public string MovementType { get; set; } = string.Empty;
    public int QuantityChange { get; set; }
    public DateOnly MovementDate { get; set; }
    public int? PurchaseId { get; set; }
    public int? SaleId { get; set; }
    public DateTime CreatedAt { get; set; }
}
