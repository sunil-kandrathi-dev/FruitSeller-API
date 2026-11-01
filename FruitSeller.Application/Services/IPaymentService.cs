using FruitSeller.Application.Dtos;

namespace FruitSeller.Application.Services;

public interface IPaymentService
{
    Task<IEnumerable<PaymentDto>> GetPaymentsAsync(int? shopId, string? method, DateOnly? fromDate, DateOnly? toDate);
    Task<PaymentDto> CreatePaymentAsync(CreatePaymentRequest request);
}
