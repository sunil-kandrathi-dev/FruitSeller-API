namespace FruitSeller.Application.Dtos;

public class FruitDto
{
    public int FruitId { get; set; }
    public string FruitName { get; set; } = string.Empty;
    public string Variety { get; set; } = string.Empty;
    public int CurrentStock { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateFruitRequest
{
    public string FruitName { get; set; } = string.Empty;
    public string Variety { get; set; } = string.Empty;
    public int OpeningQuantity { get; set; }
}
