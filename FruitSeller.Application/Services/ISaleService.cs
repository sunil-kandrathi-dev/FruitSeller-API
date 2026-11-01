using FruitSeller.Application.Dtos;

namespace FruitSeller.Application.Services;

public interface ISaleService
{
    Task<IEnumerable<SaleDto>> GetSalesAsync(DateOnly? fromDate, DateOnly? toDate);
    Task<SaleDto> CreateSaleAsync(CreateSaleRequest request);
}
