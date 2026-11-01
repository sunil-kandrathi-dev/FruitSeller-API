using FruitSeller.Application.Dtos;

namespace FruitSeller.Application.Services;

public interface IExpenseService
{
    Task<IEnumerable<ExpenseDto>> GetExpensesAsync(DateOnly? fromDate, DateOnly? toDate);
    Task<ExpenseDto> CreateExpenseAsync(CreateExpenseRequest request);
}
