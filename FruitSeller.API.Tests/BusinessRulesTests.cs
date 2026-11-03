using FruitSeller.Application.Dtos;
using FruitSeller.Application.Validation;
using FruitSeller.Domain.BusinessRules;
using FruitSeller.Domain.Entities;
using FruitSeller.Infrastructure.Persistence;
using FruitSeller.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace FruitSeller.API.Tests;

public class BusinessRulesTests
{
    [Fact]
    public void CalculateOutstanding_ReturnsOpeningBalancePlusSalesMinusPayments()
    {
        var result = BusinessRules.CalculateOutstanding(1000m, 2500m, 500m);

        Assert.Equal(3000m, result);
    }

    [Fact]
    public void IsSaleAllowed_AllowsSaleWhenStockIsSufficient()
    {
        var result = BusinessRules.IsSaleAllowed(12, 7);

        Assert.True(result);
    }

    [Fact]
    public void IsSaleAllowed_RejectsSaleThatWouldMakeStockNegative()
    {
        var result = BusinessRules.IsSaleAllowed(4, 6);

        Assert.False(result);
    }

    [Fact]
    public void IsValidPaymentAmount_RejectsZeroOrNegativeAmounts()
    {
        Assert.True(BusinessRules.IsValidPaymentAmount(25m));
        Assert.False(BusinessRules.IsValidPaymentAmount(0m));
        Assert.False(BusinessRules.IsValidPaymentAmount(-5m));
    }

    [Fact]
    public void CalculateWeightedAverageCost_ReturnsCorrectBlendedRate()
    {
        var result = BusinessRules.CalculateWeightedAverageCost(10m, 100, 12m, 50);

        Assert.Equal(10.666666666666666666666666667m, result);
    }

    [Fact]
    public void CanAcceptPayment_UsesOutstandingBalanceGuard()
    {
        Assert.True(BusinessRules.CanAcceptPayment(500m, 500m));
        Assert.True(BusinessRules.CanAcceptPayment(500m, 400m));
        Assert.False(BusinessRules.CanAcceptPayment(500m, 600m));
        Assert.False(BusinessRules.CanAcceptPayment(500m, 0m));
    }

    [Fact]
    public void HasSufficientStock_RejectsNegativeStock()
    {
        Assert.True(BusinessRules.HasSufficientStock(12, 7));
        Assert.False(BusinessRules.HasSufficientStock(4, 6));
    }
}

public class ValidationHelpersTests
{
    [Theory]
    [InlineData("9876543210", true)]
    [InlineData("1234567890", false)]
    [InlineData("9988776655", true)]
    [InlineData("123", false)]
    public void IsValidIndianMobile_ValidatesExpectedPatterns(string phoneNumber, bool expected)
    {
        var result = ValidationHelpers.IsValidIndianMobile(phoneNumber);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Transport", true)]
    [InlineData("ELECTRICITY", true)]
    [InlineData("  Other expenses  ", true)]
    [InlineData("Loading and unloading", true)]
    [InlineData("Food", false)]
    public void IsValidCategory_ValidatesKnownCategoryValues(string category, bool expected)
    {
        var result = ValidationHelpers.IsValidCategory(category);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("  other expenses  ", "Other expenses")]
    [InlineData("PACKAGING", "Packaging")]
    public void NormalizeCategory_ReturnsCanonicalDatabaseValue(string category, string expected)
    {
        var result = ValidationHelpers.NormalizeCategory(category);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Cash", true)]
    [InlineData("UPI", true)]
    [InlineData("Card", false)]
    [InlineData(null, false)]
    public void IsValidPaymentMethod_ValidatesSupportedMethods(string? paymentMethod, bool expected)
    {
        var result = ValidationHelpers.IsValidPaymentMethod(paymentMethod);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Cash", true)]
    [InlineData("UPI", true)]
    [InlineData("Bank transfer", true)]
    [InlineData("Cheque", false)]
    public void IsValidSupplierPaymentMethod_ValidatesSupportedSupplierMethods(string? paymentMethod, bool expected)
    {
        var result = BusinessRules.IsValidSupplierPaymentMethod(paymentMethod);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void IsValidPaymentAmount_RejectsMoreThanTwoDecimalPlaces()
    {
        Assert.False(BusinessRules.IsValidPaymentAmount(10.001m));
        Assert.True(BusinessRules.IsValidPaymentAmount(10.00m));
    }
}

public class SupplierPaymentServiceTests
{
    private static FruitSellerDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<FruitSellerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new FruitSellerDbContext(options);
    }

    [Fact]
    public async Task CreateSupplierPaymentAsync_WhenPurchaseDoesNotExist_ThrowsKeyNotFoundException()
    {
        await using var db = CreateDbContext();
        var service = new SupplierPaymentService(db);

        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CreateSupplierPaymentAsync(999, new CreateSupplierPaymentRequest
        {
            Amount = 50m,
            PaymentDate = DateOnly.FromDateTime(DateTime.UtcNow),
            PaymentMethod = "Cash"
        }));

        Assert.Contains("Purchase not found", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateSupplierPaymentAsync_WhenPaymentExceedsBalance_ThrowsInvalidOperationException()
    {
        await using var db = CreateDbContext();
        db.Purchases.Add(new Purchase
        {
            FruitId = 10,
            SupplierName = "Alpha Supplier",
            BoxQuantity = 20,
            TotalCost = 100m,
            PurchaseDate = DateOnly.FromDateTime(DateTime.UtcNow),
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var service = new SupplierPaymentService(db);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateSupplierPaymentAsync(1, new CreateSupplierPaymentRequest
        {
            Amount = 101m,
            PaymentDate = DateOnly.FromDateTime(DateTime.UtcNow),
            PaymentMethod = "Cash"
        }));

        Assert.Contains("remaining balance", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetPurchasesAsync_ReportsPaidAndOutstandingBalance()
    {
        await using var db = CreateDbContext();

        var fruit = new Fruit
        {
            FruitName = "Mango",
            Variety = "Kesar",
            CreatedAt = DateTime.UtcNow
        };
        db.Fruits.Add(fruit);
        await db.SaveChangesAsync();

        var purchase = new Purchase
        {
            FruitId = fruit.FruitId,
            SupplierName = "Supplier A",
            BoxQuantity = 50,
            TotalCost = 500m,
            PurchaseDate = DateOnly.FromDateTime(DateTime.UtcNow),
            CreatedAt = DateTime.UtcNow
        };
        db.Purchases.Add(purchase);
        await db.SaveChangesAsync();

        db.SupplierPayments.AddRange(
            new SupplierPayment { PurchaseId = purchase.PurchaseId, Amount = 150m, PaymentDate = DateOnly.FromDateTime(DateTime.UtcNow), PaymentMethod = "Cash", CreatedAt = DateTime.UtcNow },
            new SupplierPayment { PurchaseId = purchase.PurchaseId, Amount = 100m, PaymentDate = DateOnly.FromDateTime(DateTime.UtcNow), PaymentMethod = "UPI", CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var service = new PurchaseService(db);
        var result = (await service.GetPurchasesAsync(null, null)).Single();

        Assert.Equal(250m, result.TotalPaid);
        Assert.Equal(250m, result.OutstandingBalance);
    }

    [Fact]
    public async Task CreatePurchaseAsync_WithInitialPayment_SavesPurchaseAndPaymentAtomically()
    {
        await using var db = CreateDbContext();

        db.Fruits.Add(new Fruit
        {
            FruitName = "Orange",
            Variety = "Nagpur",
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var service = new PurchaseService(db);
        var purchaseDate = DateOnly.FromDateTime(DateTime.UtcNow);

        var purchase = await service.CreatePurchaseAsync(new CreatePurchaseRequest
        {
            FruitName = "Orange",
            SupplierName = "Supplier B",
            BoxQuantity = 40,
            TotalCost = 300m,
            PurchaseDate = purchaseDate,
            InitialPaymentAmount = 120m,
            InitialPaymentMethod = "UPI",
            InitialPaymentDate = purchaseDate,
            InitialPaymentReferenceNumber = "REF-001",
            InitialPaymentNotes = "Advance payment"
        });

        Assert.Equal(120m, purchase.TotalPaid);
        Assert.Equal(180m, purchase.OutstandingBalance);
        Assert.Equal(1, await db.SupplierPayments.CountAsync());
        Assert.Equal(purchase.PurchaseId, (await db.SupplierPayments.SingleAsync()).PurchaseId);
    }
}
