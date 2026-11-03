namespace FruitSeller.Domain.Entities;

public class SupplierPayment
{
    public long SupplierPaymentId { get; set; }
    public int PurchaseId { get; set; }
    public decimal Amount { get; set; }
    public DateOnly PaymentDate { get; set; }
    public string PaymentMethod { get; set; } = "Cash";
    public string? ReferenceNumber { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }

    public Purchase Purchase { get; set; } = null!;
}
