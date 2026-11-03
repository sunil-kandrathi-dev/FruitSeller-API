using System.Data;
using FruitSeller.Application.Dtos;
using FruitSeller.Application.Services;
using FruitSeller.Application.Validation;
using FruitSeller.Domain.BusinessRules;
using FruitSeller.Domain.Entities;
using FruitSeller.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FruitSeller.Infrastructure.Services;

public class PurchaseService : IPurchaseService
{
    private readonly FruitSellerDbContext _db;

    public PurchaseService(FruitSellerDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<PurchaseDto>> GetPurchasesAsync(DateOnly? fromDate, DateOnly? toDate)
    {
        var query = _db.Purchases.AsNoTracking().AsQueryable();

        if (fromDate.HasValue)
        {
            query = query.Where(x => x.PurchaseDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(x => x.PurchaseDate <= toDate.Value);
        }

        var purchases = await query.OrderByDescending(x => x.PurchaseDate).ThenByDescending(x => x.PurchaseId).ToListAsync();
        var fruitNames = await _db.Fruits.AsNoTracking().ToDictionaryAsync(x => x.FruitId, x => x.FruitName);
        var paymentTotals = await _db.SupplierPayments
            .AsNoTracking()
            .GroupBy(x => x.PurchaseId)
            .Select(x => new { PurchaseId = x.Key, TotalPaid = x.Sum(y => y.Amount) })
            .ToDictionaryAsync(x => x.PurchaseId, x => x.TotalPaid);

        return purchases.Select(x =>
        {
            var totalPaid = paymentTotals.TryGetValue(x.PurchaseId, out var amount) ? amount : 0m;
            var outstandingBalance = BusinessRules.CalculateRemainingBalance(x.TotalCost, totalPaid);

            return new PurchaseDto
            {
                PurchaseId = x.PurchaseId,
                FruitId = x.FruitId,
                FruitName = fruitNames.TryGetValue(x.FruitId, out var fruitName) ? fruitName : string.Empty,
                SupplierName = x.SupplierName,
                BoxQuantity = x.BoxQuantity,
                TotalCost = x.TotalCost,
                TotalPaid = totalPaid,
                OutstandingBalance = outstandingBalance,
                PurchaseDate = x.PurchaseDate,
                CreatedAt = x.CreatedAt
            };
        }).ToList();
    }

    public async Task<PurchaseDto> CreatePurchaseAsync(CreatePurchaseRequest request)
    {
        if (request is null)
        {
            throw new InvalidOperationException(ValidationHelpers.DefaultApiError);
        }

        var fruitName = ValidationHelpers.NormalizeText(request.FruitName);
        var supplierName = ValidationHelpers.NormalizeText(request.SupplierName);

        if (string.IsNullOrWhiteSpace(fruitName) || fruitName.Length > 100)
        {
            throw new InvalidOperationException("Fruit name is required and must be 100 characters or less.");
        }

        if (string.IsNullOrWhiteSpace(supplierName) || supplierName.Length > 100)
        {
            throw new InvalidOperationException("Supplier name is required and must be 100 characters or less.");
        }

        if (request.BoxQuantity <= 0)
        {
            throw new InvalidOperationException("Box quantity must be greater than zero.");
        }

        if (request.TotalCost <= 0m || !BusinessRules.IsValidPaymentAmount(request.TotalCost))
        {
            throw new InvalidOperationException("Total cost must be greater than zero and have at most two decimal places.");
        }

        if (request.PurchaseDate > DateOnly.FromDateTime(DateTime.UtcNow))
        {
            throw new InvalidOperationException("Purchase date cannot be in the future.");
        }

        if (request.InitialPaymentAmount.HasValue)
        {
            var initialPayment = request.InitialPaymentAmount.Value;
            if (initialPayment <= 0m || !BusinessRules.IsValidPaymentAmount(initialPayment))
            {
                throw new InvalidOperationException("Initial payment amount must be greater than zero and have at most two decimal places.");
            }

            if (initialPayment > request.TotalCost)
            {
                throw new InvalidOperationException("Initial payment cannot exceed the total purchase cost.");
            }

            var initialPaymentMethod = ValidationHelpers.NormalizeText(request.InitialPaymentMethod);
            if (string.IsNullOrWhiteSpace(initialPaymentMethod))
            {
                initialPaymentMethod = "Cash";
            }

            if (!BusinessRules.IsValidSupplierPaymentMethod(initialPaymentMethod))
            {
                throw new InvalidOperationException("Initial payment method must be Cash, UPI, or Bank transfer.");
            }

            var initialPaymentDate = request.InitialPaymentDate ?? request.PurchaseDate;
            if (initialPaymentDate > DateOnly.FromDateTime(DateTime.UtcNow))
            {
                throw new InvalidOperationException("Initial payment date cannot be in the future.");
            }
        }

        var fruit = await _db.Fruits.AsNoTracking().SingleOrDefaultAsync(x => x.FruitName.ToLower() == fruitName.ToLower());
        if (fruit is null)
        {
            throw new InvalidOperationException("Fruit name does not match an existing fruit record.");
        }

        using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        var lockedFruit = _db.Database.IsSqlServer()
            ? await _db.Fruits
                .FromSqlInterpolated($"SELECT * FROM dbo.Fruits WITH (UPDLOCK, HOLDLOCK) WHERE FruitId = {fruit.FruitId}")
                .SingleAsync()
            : await _db.Fruits.SingleAsync(x => x.FruitId == fruit.FruitId);

        var purchase = new Purchase
        {
            FruitId = lockedFruit.FruitId,
            SupplierName = supplierName,
            BoxQuantity = request.BoxQuantity,
            TotalCost = request.TotalCost,
            PurchaseDate = request.PurchaseDate,
            CreatedAt = DateTime.UtcNow
        };

        _db.Purchases.Add(purchase);
        await _db.SaveChangesAsync();

        if (request.InitialPaymentAmount.HasValue)
        {
            var initialPaymentAmount = request.InitialPaymentAmount.Value;
            if (initialPaymentAmount > 0m)
            {
                var initialPaymentMethod = ValidationHelpers.NormalizeText(request.InitialPaymentMethod);
                if (string.IsNullOrWhiteSpace(initialPaymentMethod))
                {
                    initialPaymentMethod = "Cash";
                }

                var payment = new SupplierPayment
                {
                    PurchaseId = purchase.PurchaseId,
                    Amount = initialPaymentAmount,
                    PaymentDate = request.InitialPaymentDate ?? request.PurchaseDate,
                    PaymentMethod = initialPaymentMethod,
                    ReferenceNumber = string.IsNullOrWhiteSpace(request.InitialPaymentReferenceNumber) ? null : ValidationHelpers.NormalizeText(request.InitialPaymentReferenceNumber),
                    Notes = string.IsNullOrWhiteSpace(request.InitialPaymentNotes) ? null : ValidationHelpers.NormalizeText(request.InitialPaymentNotes),
                    CreatedAt = DateTime.UtcNow
                };

                _db.SupplierPayments.Add(payment);
                await _db.SaveChangesAsync();
            }
        }

        var movement = new InventoryMovement
        {
            FruitId = fruit.FruitId,
            MovementType = "Purchase",
            QuantityChange = request.BoxQuantity,
            MovementDate = request.PurchaseDate,
            PurchaseId = purchase.PurchaseId,
            CreatedAt = DateTime.UtcNow
        };

        _db.InventoryMovements.Add(movement);
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        var totalPaid = await _db.SupplierPayments.AsNoTracking().Where(x => x.PurchaseId == purchase.PurchaseId).SumAsync(x => (decimal?)x.Amount ?? 0m);

        return new PurchaseDto
        {
            PurchaseId = purchase.PurchaseId,
            FruitId = fruit.FruitId,
            FruitName = fruit.FruitName,
            SupplierName = purchase.SupplierName,
            BoxQuantity = purchase.BoxQuantity,
            TotalCost = purchase.TotalCost,
            TotalPaid = totalPaid,
            OutstandingBalance = BusinessRules.CalculateRemainingBalance(purchase.TotalCost, totalPaid),
            PurchaseDate = purchase.PurchaseDate,
            CreatedAt = purchase.CreatedAt
        };
    }
}
