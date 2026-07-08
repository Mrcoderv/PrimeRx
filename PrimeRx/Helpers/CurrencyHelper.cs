using System.Globalization;

namespace PrimeRx.Helpers;

public static class CurrencyHelper
{
    private static readonly CultureInfo IndianCulture = new("hi-IN");

    public static string ToRs(this decimal amount)
    {
        return "Rs. " + amount.ToString("N2", IndianCulture);
    }

    public static string ToRs(this double amount)
    {
        return ((decimal)amount).ToRs();
    }

    public static string ToRs(this int amount)
    {
        return ((decimal)amount).ToRs();
    }

    public static string ToRs(this long amount)
    {
        return ((decimal)amount).ToRs();
    }

    public static string ToRs(this float amount)
    {
        return ((decimal)amount).ToRs();
    }
}
