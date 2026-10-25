using FruitSeller.API.Utilities;

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
}
