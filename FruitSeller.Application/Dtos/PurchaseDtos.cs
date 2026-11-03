namespace FruitSeller.Application.Dtos;

public class PurchaseDto
{
    public int PurchaseId { get; set; }
    public int FruitId { get; set; }
    public string FruitName { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public int BoxQuantity { get; set; }
    public decimal TotalCost { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal OutstandingBalance { get; set; }
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
    public decimal? InitialPaymentAmount { get; set; }
    public string? InitialPaymentMethod { get; set; }
    public DateOnly? InitialPaymentDate { get; set; }
    public string? InitialPaymentReferenceNumber { get; set; }
    public string? InitialPaymentNotes { get; set; }
}
