namespace PetShop.Domain.Members;

internal static class MoneyRules
{
    public static void EnsurePositive(decimal amount, string parameterName)
    {
        EnsurePrecision(amount, parameterName);
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, "金额必须大于零。");
        }
    }

    public static void EnsureNonNegative(decimal amount, string parameterName)
    {
        EnsurePrecision(amount, parameterName);
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, "金额不能小于零。");
        }
    }

    private static void EnsurePrecision(decimal amount, string parameterName)
    {
        if (decimal.Round(amount, 2, MidpointRounding.AwayFromZero) != amount)
        {
            throw new ArgumentException("金额最多只能保留两位小数。", parameterName);
        }
    }
}
