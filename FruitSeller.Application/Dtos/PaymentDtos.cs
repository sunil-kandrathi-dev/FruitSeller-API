namespace FruitSeller.Application.Dtos;

public class PaymentDto
{
    public int PaymentId { get; set; }
    public int ShopId { get; set; }
    public string ShopName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = "Cash";
    public DateOnly PaymentDate { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreatePaymentRequest
{
    public string ShopName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = "Cash";
    public DateOnly PaymentDate { get; set; }
}

public class SupplierPaymentDto
{
    public long SupplierPaymentId { get; set; }
    public int PurchaseId { get; set; }
    public decimal Amount { get; set; }
    public DateOnly PaymentDate { get; set; }
    public string PaymentMethod { get; set; } = "Cash";
    public string? ReferenceNumber { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateSupplierPaymentRequest
{
    public decimal Amount { get; set; }
    public DateOnly PaymentDate { get; set; }
    public string PaymentMethod { get; set; } = "Cash";
    public string? ReferenceNumber { get; set; }
    public string? Notes { get; set; }
}
