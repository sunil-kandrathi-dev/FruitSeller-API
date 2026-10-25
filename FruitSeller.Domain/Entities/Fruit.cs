namespace FruitSeller.Domain.Entities;

public class Fruit
{
    public int FruitId { get; set; }
    public string FruitName { get; set; } = string.Empty;
    public string Variety { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
