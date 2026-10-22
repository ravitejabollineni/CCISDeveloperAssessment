using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;

namespace CCISDeveloperAssessment.Currency;

/// <summary>
/// Converts free-text GBP amounts typed by a user into whole pence.
/// </summary>
public static class CurrencyInputParser
{
    /// <summary>
    /// Largest accepted amount, in whole pence - the same limit the calculator supports,
    /// see <see cref="ChangeCalculator.MaxAmountInPence"/> (£1,000,000,000,000.00).
    /// </summary>
    public const long MaxAmountInPence = ChangeCalculator.MaxAmountInPence;

    private const int MaxDecimalPlaces = 2;

    private static readonly decimal MaxAmountInPounds = MaxAmountInPence / 100m;

    private static readonly Regex AmountPattern = new(
        @"^(?<whole>\d{1,3}(?:,\d{3})*|\d+)(?:\.(?<fraction>\d{1,2}))?$",
        RegexOptions.CultureInvariant);

    /// <summary>Matches an otherwise well formed amount that carries too many decimal places.</summary>
    private static readonly Regex TooPreciseAmountPattern = new(
        @"^(?:\d{1,3}(?:,\d{3})*|\d+)\.\d{3,}$",
        RegexOptions.CultureInvariant);

    /// <summary>
    /// Attempts to convert a typed GBP amount into whole pence.
    /// </summary>
    /// <param name="input">Raw text entered by the user.</param>
    /// <param name="pence">The parsed amount in whole pence; zero when parsing fails.</param>
    /// <param name="error">A user-facing explanation when parsing fails; otherwise null.</param>
    /// <returns>True when <paramref name="input"/> is a valid GBP amount.</returns>
    public static bool TryParsePence(
        string? input,
        out long pence,
        [NotNullWhen(false)] out string? error)
    {
        pence = 0;

        if (string.IsNullOrWhiteSpace(input))
        {
            error = "Please enter an amount in pounds, for example 20 or 5.50.";
            return false;
        }

        string text = input.Trim();

        if (text.StartsWith('£'))
        {
            text = text[1..].TrimStart();
        }

        if (text.StartsWith('-'))
        {
            error = "The amount cannot be negative.";
            return false;
        }

        Match match = AmountPattern.Match(text);
        if (!match.Success)
        {
            error = TooPreciseAmountPattern.IsMatch(text)
                ? "The amount cannot have more than two decimal places (whole pence only)."
                : $"'{text}' is not a valid amount. Enter pounds and pence, for example 20 or 5.50.";
            return false;
        }

        string whole = match.Groups["whole"].Value.Replace(",", string.Empty);
        string fraction = match.Groups["fraction"].Success
            ? match.Groups["fraction"].Value.PadRight(MaxDecimalPlaces, '0')
            : "00";
        
        if (!decimal.TryParse(
                $"{whole}.{fraction}",
                NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture,
                out decimal pounds))
        {
            error = TooLargeMessage;
            return false;
        }
       
        if (pounds > MaxAmountInPounds)
        {
            error = TooLargeMessage;
            return false;
        }
        
        pence = (long)(pounds * 100m);
        error = null;
        return true;
    }

    /// <summary>
    /// Converts a typed GBP amount into whole pence.
    /// </summary>
    /// <exception cref="FormatException">The input is not a valid GBP amount.</exception>
    public static long ParsePence(string? input)
    {
        if (!TryParsePence(input, out long pence, out string? error))
        {
            throw new FormatException(error);
        }

        return pence;
    }

    /// <summary>
    /// Formats whole pence as a GBP amount, for example 550 becomes "£5.50".
    /// </summary>
    public static string FormatPence(long pence)
    {
        if (pence < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pence), pence, "Negative amounts are not supported.");
        }

        long pounds = pence / 100;
        long remainder = pence % 100;
        return $"£{pounds.ToString(CultureInfo.InvariantCulture)}.{remainder.ToString(CultureInfo.InvariantCulture).PadLeft(2, '0')}";
    }

    private static string TooLargeMessage =>
        $"The amount is too large. The maximum accepted amount is {FormatPence(MaxAmountInPence)}.";
}
