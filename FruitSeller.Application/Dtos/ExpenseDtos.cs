namespace FruitSeller.Application.Dtos;

public class ExpenseDto
{
    public int ExpenseId { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateOnly ExpenseDate { get; set; }
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateExpenseRequest
{
    public string Category { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateOnly ExpenseDate { get; set; }
    public decimal Amount { get; set; }
}
