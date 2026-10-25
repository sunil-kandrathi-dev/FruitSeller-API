using System.ComponentModel.DataAnnotations;

namespace FruitSeller.API.Models;

public class Sale
{
    [Key]
    public int SaleId { get; set; }

    public int ShopId { get; set; }

    public int FruitId { get; set; }

    public int BoxQuantity { get; set; }

    public decimal PricePerBox { get; set; }

    public decimal TotalAmount { get; set; }

    public DateOnly SaleDate { get; set; }

    public DateTime CreatedAt { get; set; }
}
