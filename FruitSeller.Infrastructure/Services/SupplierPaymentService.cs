using System.Data;
using FruitSeller.Application.Dtos;
using FruitSeller.Application.Services;
using FruitSeller.Application.Validation;
using FruitSeller.Domain.BusinessRules;
using FruitSeller.Domain.Entities;
using FruitSeller.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FruitSeller.Infrastructure.Services;

public class SupplierPaymentService : ISupplierPaymentService
{
    private readonly FruitSellerDbContext _db;

    public SupplierPaymentService(FruitSellerDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<SupplierPaymentDto>> GetSupplierPaymentsAsync(int purchaseId)
    {
        var purchaseExists = await _db.Purchases.AsNoTracking().AnyAsync(x => x.PurchaseId == purchaseId);
        if (!purchaseExists)
        {
            throw new KeyNotFoundException("Purchase not found.");
        }

        var payments = await _db.SupplierPayments
            .AsNoTracking()
            .Where(x => x.PurchaseId == purchaseId)
            .OrderByDescending(x => x.PaymentDate)
            .ThenByDescending(x => x.SupplierPaymentId)
            .ToListAsync();

        return payments.Select(x => new SupplierPaymentDto
        {
            SupplierPaymentId = x.SupplierPaymentId,
            PurchaseId = x.PurchaseId,
            Amount = x.Amount,
            PaymentDate = x.PaymentDate,
            PaymentMethod = x.PaymentMethod,
            ReferenceNumber = x.ReferenceNumber,
            Notes = x.Notes,
            CreatedAt = x.CreatedAt
        }).ToList();
    }

    public async Task<SupplierPaymentDto> CreateSupplierPaymentAsync(int purchaseId, CreateSupplierPaymentRequest request)
    {
        if (request is null)
        {
            throw new InvalidOperationException(ValidationHelpers.DefaultApiError);
        }

        var normalizedMethod = ValidationHelpers.NormalizeText(request.PaymentMethod);
        if (string.IsNullOrWhiteSpace(normalizedMethod))
        {
            normalizedMethod = "Cash";
        }

        if (!BusinessRules.IsValidPaymentAmount(request.Amount))
        {
            throw new InvalidOperationException("Supplier payment amount must be greater than zero and have at most two decimal places.");
        }

        if (!BusinessRules.IsValidSupplierPaymentMethod(normalizedMethod))
        {
            throw new InvalidOperationException("Payment method must be Cash, UPI, or Bank transfer.");
        }

        if (request.PaymentDate > DateOnly.FromDateTime(DateTime.UtcNow))
        {
            throw new InvalidOperationException("Payment date cannot be in the future.");
        }

        using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        var purchase = _db.Database.IsSqlServer()
            ? await _db.Purchases
                .FromSqlInterpolated($"SELECT * FROM dbo.Purchases WITH (UPDLOCK, HOLDLOCK) WHERE PurchaseId = {purchaseId}")
                .SingleOrDefaultAsync()
            : await _db.Purchases.SingleOrDefaultAsync(x => x.PurchaseId == purchaseId);

        if (purchase is null)
        {
            throw new KeyNotFoundException("Purchase not found.");
        }

        var totalPaidToDate = await _db.SupplierPayments
            .AsNoTracking()
            .Where(x => x.PurchaseId == purchaseId)
            .SumAsync(x => (decimal?)x.Amount ?? 0m);

        if (!BusinessRules.CanApplySupplierPayment(purchase.TotalCost, totalPaidToDate, request.Amount))
        {
            throw new InvalidOperationException("Supplier payment exceeds the remaining balance for this purchase.");
        }

        var payment = new SupplierPayment
        {
            PurchaseId = purchase.PurchaseId,
            Amount = request.Amount,
            PaymentDate = request.PaymentDate,
            PaymentMethod = normalizedMethod,
            ReferenceNumber = string.IsNullOrWhiteSpace(request.ReferenceNumber) ? null : ValidationHelpers.NormalizeText(request.ReferenceNumber),
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : ValidationHelpers.NormalizeText(request.Notes),
            CreatedAt = DateTime.UtcNow
        };

        _db.SupplierPayments.Add(payment);
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return new SupplierPaymentDto
        {
            SupplierPaymentId = payment.SupplierPaymentId,
            PurchaseId = purchase.PurchaseId,
            Amount = payment.Amount,
            PaymentDate = payment.PaymentDate,
            PaymentMethod = payment.PaymentMethod,
            ReferenceNumber = payment.ReferenceNumber,
            Notes = payment.Notes,
            CreatedAt = payment.CreatedAt
        };
    }
}
