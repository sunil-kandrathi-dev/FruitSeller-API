namespace FruitSeller.API.Utilities;

public static class BusinessRules
{
    public static decimal CalculateOutstanding(decimal openingBalance, decimal salesTotal, decimal paymentsTotal)
    {
        return openingBalance + salesTotal - paymentsTotal;
    }

    public static bool IsSaleAllowed(int currentStock, int requestedBoxes)
    {
        return currentStock - requestedBoxes >= 0;
    }
}
