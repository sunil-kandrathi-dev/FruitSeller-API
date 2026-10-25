using System.ComponentModel.DataAnnotations;

namespace FruitSeller.API.Models;

public class Expense
{
    [Key]
    public int ExpenseId { get; set; }

    [Required]
    [StringLength(50)]
    public string Category { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    public DateOnly ExpenseDate { get; set; }

    public decimal Amount { get; set; }

    public DateTime CreatedAt { get; set; }
}
