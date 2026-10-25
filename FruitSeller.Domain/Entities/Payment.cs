namespace FruitSeller.Domain.Entities;

public class Payment
{
    public int PaymentId { get; set; }
    public int ShopId { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = "Cash";
    public DateOnly PaymentDate { get; set; }
    public DateTime CreatedAt { get; set; }
}
