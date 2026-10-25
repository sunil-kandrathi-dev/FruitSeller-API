using FruitSeller.Application.Dtos;

namespace FruitSeller.Application.Services;

public interface IShopService
{
    Task<IEnumerable<ShopDto>> GetShopsAsync();
    Task<ShopDto> CreateShopAsync(CreateShopRequest request);
}
