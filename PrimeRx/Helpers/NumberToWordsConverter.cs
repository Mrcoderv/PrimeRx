using System;

namespace PrimeRx.Helpers;

public static class NumberToWordsConverter
{
    private static readonly string[] Units = { "", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten", 
        "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen", "Seventeen", "Eighteen", "Nineteen" };

    private static readonly string[] Tens = { "", "", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety" };

    public static string ToWords(long number)
    {
        if (number == 0)
            return "Zero";

        if (number < 0)
            return "Minus " + ToWords(Math.Abs(number));

        string words = "";

        // Crore
        if (number >= 10000000)
        {
            words += ConvertToWords(number / 10000000) + " Crore ";
            number %= 10000000;
        }

        // Lakh
        if (number >= 100000)
        {
            words += ConvertToWords(number / 100000) + " Lakh ";
            number %= 100000;
        }

        // Thousand
        if (number >= 1000)
        {
            words += ConvertToWords(number / 1000) + " Thousand ";
            number %= 1000;
        }

        // Remaining
        words += ConvertToWords(number);

        return words.Trim();
    }

    public static string ToWords(decimal amount)
    {
        long rupees = (long)Math.Floor(amount);
        int paise = (int)Math.Round((amount - rupees) * 100);

        string result = rupees == 0 ? "Zero" : ToWords(rupees) + " Rupees";

        if (paise > 0)
            result += " and " + ToWords(paise) + " Paise";
        else
            result += " Only";

        return result;
    }

    private static string ConvertToWords(long number)
    {
        if (number < 20)
            return Units[number];

        if (number < 100)
        {
            return Tens[number / 10] + (number % 10 > 0 ? " " + Units[number % 10] : "");
        }

        if (number < 1000)
        {
            string hundred = Units[number / 100] + " Hundred";
            long remainder = number % 100;
            if (remainder > 0)
                hundred += " " + ConvertToWords(remainder);
            return hundred;
        }

        return "";
    }
}
