using System.Globalization;
using System.Text.RegularExpressions;

namespace FruitSeller.Application.Validation;

public static partial class ValidationHelpers
{
    public const string DefaultApiError = "The request contains invalid data.";

    public static bool IsBlank(string? value) => string.IsNullOrWhiteSpace(value);
    public static string NormalizeText(string? value) => value?.Trim() ?? string.Empty;

    public static bool IsValidIndianMobile(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber)) return false;
        return Regex.IsMatch(phoneNumber.Trim(), "^[6-9]\\d{9}$");
    }

    public static bool IsValidCategory(string? category)
    {
        var normalized = NormalizeText(category);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return false;
        }

        var allowed = new[]
        {
            "Transport",
            "Packaging",
            "Loading and unloading",
            "Shop rent",
            "Electricity",
            "Other expenses"
        };

        return allowed.Contains(normalized, StringComparer.OrdinalIgnoreCase);
    }

    public static string NormalizeCategory(string? category)
    {
        var normalized = NormalizeText(category);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return string.Empty;
        }

        var allowed = new[]
        {
            "Transport",
            "Packaging",
            "Loading and unloading",
            "Shop rent",
            "Electricity",
            "Other expenses"
        };

        var match = allowed.FirstOrDefault(x => string.Equals(x, normalized, StringComparison.OrdinalIgnoreCase));
        return match ?? normalized;
    }

    public static bool IsValidPaymentMethod(string? paymentMethod)
    {
        return string.Equals(paymentMethod, "Cash", StringComparison.OrdinalIgnoreCase)
            || string.Equals(paymentMethod, "UPI", StringComparison.OrdinalIgnoreCase);
    }

    public static DateOnly? ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var parsed = DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result);
        return parsed ? result : null;
    }
}
