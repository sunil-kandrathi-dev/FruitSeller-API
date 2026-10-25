using System.ComponentModel.DataAnnotations;

namespace FruitSeller.API.Models;

public class Shop
{
    [Key]
    public int ShopId { get; set; }

    [Required]
    [StringLength(100)]
    public string ShopName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string OwnerName { get; set; } = string.Empty;

    [Required]
    [StringLength(10)]
    public string PhoneNumber { get; set; } = string.Empty;

    public decimal OpeningBalance { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
