using System.Data;
using FruitSeller.Application.Dtos;
using FruitSeller.Application.Services;
using FruitSeller.Application.Validation;
using FruitSeller.Domain.BusinessRules;
using FruitSeller.Domain.Entities;
using FruitSeller.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FruitSeller.Infrastructure.Services;

public class PaymentService : IPaymentService
{
    private readonly FruitSellerDbContext _db;

    public PaymentService(FruitSellerDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<PaymentDto>> GetPaymentsAsync(int? shopId, string? method, DateOnly? fromDate, DateOnly? toDate)
    {
        var query = _db.Payments.AsNoTracking().AsQueryable();

        if (shopId.HasValue)
        {
            query = query.Where(x => x.ShopId == shopId.Value);
        }

        if (!string.IsNullOrWhiteSpace(method))
        {
            query = query.Where(x => x.PaymentMethod.ToLower() == method.Trim().ToLower());
        }

        if (fromDate.HasValue)
        {
            query = query.Where(x => x.PaymentDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(x => x.PaymentDate <= toDate.Value);
        }

        var payments = await query.OrderByDescending(x => x.PaymentDate).ThenByDescending(x => x.PaymentId).ToListAsync();
        var shops = await _db.Shops.AsNoTracking().ToDictionaryAsync(x => x.ShopId, x => x.ShopName);

        return payments.Select(x => new PaymentDto
        {
            PaymentId = x.PaymentId,
            ShopId = x.ShopId,
            ShopName = shops.TryGetValue(x.ShopId, out var shopName) ? shopName : string.Empty,
            Amount = x.Amount,
            PaymentMethod = x.PaymentMethod,
            PaymentDate = x.PaymentDate,
            CreatedAt = x.CreatedAt
        }).ToList();
    }

    public async Task<PaymentDto> CreatePaymentAsync(CreatePaymentRequest request)
    {
        if (request is null)
        {
            throw new InvalidOperationException(ValidationHelpers.DefaultApiError);
        }

        var shopName = ValidationHelpers.NormalizeText(request.ShopName);
        if (string.IsNullOrWhiteSpace(shopName) || shopName.Length > 100)
        {
            throw new InvalidOperationException("Shop name is required and must be 100 characters or less.");
        }

        if (!BusinessRules.IsValidPaymentAmount(request.Amount))
        {
            throw new InvalidOperationException("Payment amount must be greater than zero.");
        }

        if (!BusinessRules.IsValidPaymentMethod(request.PaymentMethod))
        {
            throw new InvalidOperationException("Payment method must be Cash or UPI.");
        }

        if (request.PaymentDate > DateOnly.FromDateTime(DateTime.UtcNow))
        {
            throw new InvalidOperationException("Payment date cannot be in the future.");
        }

        var shop = await _db.Shops.AsNoTracking().SingleOrDefaultAsync(x => x.ShopName.ToLower() == shopName.ToLower());
        if (shop is null)
        {
            throw new InvalidOperationException("Shop name does not match an existing shop record.");
        }

        using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        var lockedShop = await _db.Shops
            .FromSqlInterpolated($"SELECT * FROM dbo.Shops WITH (UPDLOCK, HOLDLOCK) WHERE ShopId = {shop.ShopId}")
            .SingleAsync();

        var salesTotal = await _db.Sales.AsNoTracking()
            .Where(x => x.ShopId == lockedShop.ShopId)
            .SumAsync(x => (decimal?)x.TotalAmount ?? 0m);

        var paymentsTotal = await _db.Payments.AsNoTracking()
            .Where(x => x.ShopId == lockedShop.ShopId)
            .SumAsync(x => (decimal?)x.Amount ?? 0m);

        var outstanding = lockedShop.OpeningBalance + salesTotal - paymentsTotal;

        if (!BusinessRules.CanAcceptPayment(outstanding, request.Amount))
        {
            throw new InvalidOperationException("Payment amount exceeds the current outstanding balance.");
        }

        var payment = new Payment
        {
            ShopId = lockedShop.ShopId,
            Amount = request.Amount,
            PaymentMethod = request.PaymentMethod,
            PaymentDate = request.PaymentDate,
            CreatedAt = DateTime.UtcNow
        };

        _db.Payments.Add(payment);
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return new PaymentDto
        {
            PaymentId = payment.PaymentId,
            ShopId = lockedShop.ShopId,
            ShopName = lockedShop.ShopName,
            Amount = payment.Amount,
            PaymentMethod = payment.PaymentMethod,
            PaymentDate = payment.PaymentDate,
            CreatedAt = payment.CreatedAt
        };
    }
}
