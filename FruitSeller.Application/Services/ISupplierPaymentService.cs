using FruitSeller.Application.Dtos;

namespace FruitSeller.Application.Services;

public interface ISupplierPaymentService
{
    Task<IEnumerable<SupplierPaymentDto>> GetSupplierPaymentsAsync(int purchaseId);
    Task<SupplierPaymentDto> CreateSupplierPaymentAsync(int purchaseId, CreateSupplierPaymentRequest request);
}
