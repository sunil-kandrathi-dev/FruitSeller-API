using FruitSeller.Application.Dtos;

namespace FruitSeller.Application.Services;

public interface IFruitService
{
    Task<IEnumerable<FruitDto>> GetFruitsAsync();
    Task<FruitDto> CreateFruitAsync(CreateFruitRequest request);
}
