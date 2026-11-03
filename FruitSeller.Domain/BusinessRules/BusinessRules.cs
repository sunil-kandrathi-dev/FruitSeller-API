namespace FruitSeller.Domain.BusinessRules;

public static class BusinessRules
{
    public static decimal CalculateOutstanding(decimal openingBalance, decimal salesTotal, decimal paymentsTotal)
    {
        return openingBalance + salesTotal - paymentsTotal;
    }

    public static decimal CalculateBusinessResult(decimal salesTotal, decimal purchaseTotal, decimal expenseTotal)
    {
        return salesTotal - purchaseTotal - expenseTotal;
    }

    public static bool IsSaleAllowed(int currentStock, int requestedBoxes)
    {
        return currentStock - requestedBoxes >= 0;
    }

    public static bool IsStockAvailable(int currentStock, int requestedQuantity)
    {
        return requestedQuantity <= currentStock;
    }

    public static bool HasAtMostTwoDecimalPlaces(decimal amount)
    {
        return decimal.Round(amount, 2, MidpointRounding.AwayFromZero) == amount;
    }

    public static bool IsValidPaymentAmount(decimal amount)
    {
        return amount > 0m && HasAtMostTwoDecimalPlaces(amount);
    }

    public static bool IsValidSupplierPaymentMethod(string? paymentMethod)
    {
        return string.Equals(paymentMethod, "Cash", StringComparison.OrdinalIgnoreCase)
            || string.Equals(paymentMethod, "UPI", StringComparison.OrdinalIgnoreCase)
            || string.Equals(paymentMethod, "Bank transfer", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsValidPaymentMethod(string? paymentMethod)
    {
        return IsValidSupplierPaymentMethod(paymentMethod);
    }

    public static bool CanAcceptPayment(decimal outstandingBalance, decimal paymentAmount)
    {
        return paymentAmount <= outstandingBalance && IsValidPaymentAmount(paymentAmount);
    }

    public static decimal CalculateRemainingBalance(decimal totalCost, decimal totalPaid)
    {
        return totalCost - totalPaid;
    }

    public static bool CanApplySupplierPayment(decimal totalCost, decimal totalPaid, decimal candidateAmount)
    {
        return IsValidPaymentAmount(candidateAmount)
            && totalPaid + candidateAmount <= totalCost;
    }

    public static bool HasSufficientStock(int currentStock, int requestedQuantity)
    {
        return IsStockAvailable(currentStock, requestedQuantity);
    }

    public static decimal CalculateWeightedAverageCost(decimal previousAverageCost, int previousQuantity, decimal newPurchaseCost, int newPurchaseQuantity)
    {
        if (previousQuantity == 0 && newPurchaseQuantity == 0)
        {
            return 0m;
        }

        if (previousQuantity == 0)
        {
            return newPurchaseCost;
        }

        var previousTotalCost = previousAverageCost * previousQuantity;
        var newTotalCost = newPurchaseCost * newPurchaseQuantity;
        var totalQuantity = previousQuantity + newPurchaseQuantity;

        if (totalQuantity == 0)
        {
            return 0m;
        }

        return (previousTotalCost + newTotalCost) / totalQuantity;
    }
}
