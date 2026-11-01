using FruitSeller.Application.Dtos;

namespace FruitSeller.Application.Services;

public interface IPurchaseService
{
    Task<IEnumerable<PurchaseDto>> GetPurchasesAsync(DateOnly? fromDate, DateOnly? toDate);
    Task<PurchaseDto> CreatePurchaseAsync(CreatePurchaseRequest request);
}
