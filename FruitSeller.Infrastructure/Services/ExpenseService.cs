using FruitSeller.Application.Dtos;
using FruitSeller.Application.Services;
using FruitSeller.Application.Validation;
using FruitSeller.Domain.Entities;
using FruitSeller.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FruitSeller.Infrastructure.Services;

public class ExpenseService : IExpenseService
{
    private readonly FruitSellerDbContext _db;

    public ExpenseService(FruitSellerDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<ExpenseDto>> GetExpensesAsync(DateOnly? fromDate, DateOnly? toDate)
    {
        var query = _db.Expenses.AsNoTracking().AsQueryable();

        if (fromDate.HasValue)
        {
            query = query.Where(x => x.ExpenseDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(x => x.ExpenseDate <= toDate.Value);
        }

        var expenses = await query.OrderByDescending(x => x.ExpenseDate).ThenByDescending(x => x.ExpenseId).ToListAsync();
        return expenses.Select(x => new ExpenseDto
        {
            ExpenseId = x.ExpenseId,
            Category = x.Category,
            Description = x.Description,
            ExpenseDate = x.ExpenseDate,
            Amount = x.Amount,
            CreatedAt = x.CreatedAt
        }).ToList();
    }

    public async Task<ExpenseDto> CreateExpenseAsync(CreateExpenseRequest request)
    {
        if (request is null)
        {
            throw new InvalidOperationException(ValidationHelpers.DefaultApiError);
        }

        var category = ValidationHelpers.NormalizeCategory(request.Category);

        if (!ValidationHelpers.IsValidCategory(category))
        {
            throw new InvalidOperationException("Category is not valid. Allowed values are Transport, Packaging, Loading and unloading, Shop rent, Electricity, or Other expenses.");
        }

        if (request.Amount <= 0m)
        {
            throw new InvalidOperationException("Expense amount must be greater than zero.");
        }

        if (request.ExpenseDate > DateOnly.FromDateTime(DateTime.UtcNow))
        {
            throw new InvalidOperationException("Expense date cannot be in the future.");
        }

        var expense = new Expense
        {
            Category = category,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : ValidationHelpers.NormalizeText(request.Description),
            ExpenseDate = request.ExpenseDate,
            Amount = request.Amount,
            CreatedAt = DateTime.UtcNow
        };

        _db.Expenses.Add(expense);
        await _db.SaveChangesAsync();

        return new ExpenseDto
        {
            ExpenseId = expense.ExpenseId,
            Category = expense.Category,
            Description = expense.Description,
            ExpenseDate = expense.ExpenseDate,
            Amount = expense.Amount,
            CreatedAt = expense.CreatedAt
        };
    }
}
