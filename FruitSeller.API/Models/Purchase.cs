using System.ComponentModel.DataAnnotations;

namespace FruitSeller.API.Models;

public class Purchase
{
    [Key]
    public int PurchaseId { get; set; }

    public int FruitId { get; set; }

    [Required]
    [StringLength(100)]
    public string SupplierName { get; set; } = string.Empty;

    public int BoxQuantity { get; set; }

    public decimal TotalCost { get; set; }

    public DateOnly PurchaseDate { get; set; }

    public DateTime CreatedAt { get; set; }
}
