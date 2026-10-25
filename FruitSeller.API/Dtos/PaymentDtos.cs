namespace FruitSeller.API.Dtos;

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
