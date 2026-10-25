using System.ComponentModel.DataAnnotations;

namespace FruitSeller.API.Models;

public class Payment
{
    [Key]
    public int PaymentId { get; set; }

    public int ShopId { get; set; }

    public decimal Amount { get; set; }

    [Required]
    [StringLength(10)]
    public string PaymentMethod { get; set; } = "Cash";

    public DateOnly PaymentDate { get; set; }

    public DateTime CreatedAt { get; set; }
}
