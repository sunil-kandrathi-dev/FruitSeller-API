using FruitSeller.API.Dtos;
using FruitSeller.API.Utilities;
using FruitSeller.Domain.Entities;
using FruitSeller.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FruitSeller.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExpensesController : ControllerBase
{
    private readonly FruitSellerDbContext _db;

    public ExpensesController(FruitSellerDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ExpenseDto>>> Get([FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate)
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
        return Ok(expenses.Select(x => new ExpenseDto
        {
            ExpenseId = x.ExpenseId,
            Category = x.Category,
            Description = x.Description,
            ExpenseDate = x.ExpenseDate,
            Amount = x.Amount,
            CreatedAt = x.CreatedAt
        }));
    }

    [HttpPost]
    public async Task<ActionResult<ExpenseDto>> Post([FromBody] CreateExpenseRequest request)
    {
        if (request is null)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["request"] = [ValidationHelpers.DefaultApiError]
            }));
        }

        var category = ValidationHelpers.NormalizeCategory(request.Category);

        if (!ValidationHelpers.IsValidCategory(category))
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["category"] = ["Category is not valid. Allowed values are Transport, Packaging, Loading and unloading, Shop rent, Electricity, or Other expenses."]
            }));
        }

        if (request.Amount <= 0m)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["amount"] = ["Expense amount must be greater than zero."]
            }));
        }

        if (request.ExpenseDate > DateOnly.FromDateTime(DateTime.UtcNow))
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["expenseDate"] = ["Expense date cannot be in the future."]
            }));
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

        return CreatedAtAction(nameof(Get), new { id = expense.ExpenseId }, new ExpenseDto
        {
            ExpenseId = expense.ExpenseId,
            Category = expense.Category,
            Description = expense.Description,
            ExpenseDate = expense.ExpenseDate,
            Amount = expense.Amount,
            CreatedAt = expense.CreatedAt
        });
    }
}
