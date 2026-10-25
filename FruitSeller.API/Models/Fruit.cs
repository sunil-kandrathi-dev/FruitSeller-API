using System.ComponentModel.DataAnnotations;

namespace FruitSeller.API.Models;

public class Fruit
{
    [Key]
    public int FruitId { get; set; }

    [Required]
    [StringLength(100)]
    public string FruitName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Variety { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
