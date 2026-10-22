using System.Diagnostics.CodeAnalysis;

namespace CCISDeveloperAssessment.Dates;

/// <summary>
/// Manual parsing and validation of user input for the date task.
/// </summary>
public static class DateParser
{
    /// <summary>
    /// The format every parsed date is normalised to on output. Input may use one or two digits
    /// for the day and month - see <see cref="AcceptedFormats"/>.
    /// </summary>
    public const string ExpectedFormat = "dd/MM/yyyy";

    /// <summary>The accepted input formats, for use in prompts and error messages.</summary>
    public const string AcceptedFormats = "d/M/yyyy or dd/MM/yyyy";

    private const char Separator = '/';
    private const int ExpectedComponentCount = 3;
    private const int YearDigits = 4;

    /// <param name="input">Raw text entered by the user.</param>
    /// <param name="date">The parsed date, or null when parsing fails.</param>
    /// <param name="error">A user-facing explanation when parsing fails; otherwise null.</param>
    /// <returns>True when the input is a real date in the supported range.</returns>
    public static bool TryParse(
        string? input,
        [NotNullWhen(true)] out ManualDate? date,
        [NotNullWhen(false)] out string? error)
    {
        date = null;

        if (string.IsNullOrWhiteSpace(input))
        {
            error = $"Please enter a date in {AcceptedFormats} format, for example 2/2/2026 or 31/01/2016.";
            return false;
        }

        string text = input.Trim();

        // Exactly three components: any missing, empty or extra component fails here, which also
        // rules out additional separators such as "31//01/2016" and "31/01/2016/".
        string[] components = text.Split(Separator);

        if (components.Length != ExpectedComponentCount
            || !IsNumericComponent(components[0], 1, 2)
            || !IsNumericComponent(components[1], 1, 2)
            || !IsNumericComponent(components[2], YearDigits, YearDigits))
        {
            error = $"'{text}' is not in {AcceptedFormats} format. Use one or two digits for the day and the month and exactly four digits for the year, for example 2/2/2026 or 31/01/2016.";
            return false;
        }

        int day = ReadNumber(components[0]);
        int month = ReadNumber(components[1]);
        int year = ReadNumber(components[2]);

        if (year < ManualDate.MinYear)
        {
            error = $"The year must be between {ManualDate.MinYear:0000} and {ManualDate.MaxYear}.";
            return false;
        }

        if (month is < 1 or > 12)
        {
            error = $"'{text}' has an invalid month. The month must be between 1 and 12.";
            return false;
        }

        int daysInMonth = DateCalculator.DaysInMonth(year, month);
        if (day < 1 || day > daysInMonth)
        {
            error = $"'{text}' is not a real date. Month {month:00} of {year:0000} has {daysInMonth} days.";
            return false;
        }

        date = ManualDate.Create(year, month, day);
        error = null;
        return true;
    }

    /// <summary>
    /// Parses a date written in <c>d/M/yyyy</c> or <c>dd/MM/yyyy</c> form.
    /// </summary>
    /// <exception cref="FormatException">The input is not a real date in an accepted format.</exception>
    public static ManualDate Parse(string? input)
    {
        if (!TryParse(input, out ManualDate? date, out string? error))
        {
            throw new FormatException(error);
        }

        return date;
    }

    /// <summary>
    /// Attempts to parse the number of days to add: a non-negative whole number with no sign,
    /// separators or decimal point.
    /// </summary>
    /// <param name="input">Raw text entered by the user.</param>
    /// <param name="days">The parsed day count; zero when parsing fails.</param>
    /// <param name="error">A user-facing explanation when parsing fails; otherwise null.</param>
    /// <returns>True when the input is a valid day count.</returns>
    public static bool TryParseDaysToAdd(
        string? input,
        out int days,
        [NotNullWhen(false)] out string? error)
    {
        days = 0;

        if (string.IsNullOrWhiteSpace(input))
        {
            error = "Please enter the number of days to add, for example 1.";
            return false;
        }

        string text = input.Trim();

        if (text.StartsWith('-'))
        {
            error = "The number of days to add cannot be negative.";
            return false;
        }

        if (!AreDigits(text, 0, text.Length))
        {
            error = $"'{text}' is not a whole number of days. Enter digits only, for example 45.";
            return false;
        }

        // Accumulate manually so an oversized value is reported rather than overflowing.
        int value = 0;
        foreach (char digit in text)
        {
            value = (value * 10) + (digit - '0');

            if (value > DateCalculator.MaxDaysToAdd)
            {
                error = $"At most {DateCalculator.MaxDaysToAdd} days can be added, which is the span of the supported calendar.";
                return false;
            }
        }

        days = value;
        error = null;
        return true;
    }

    /// <summary>
    /// True when a date component is between <paramref name="minDigits"/> and
    /// <paramref name="maxDigits"/> characters long and made up only of ASCII digits. Empty
    /// components, signs, spaces and Unicode digits are all rejected.
    /// </summary>
    private static bool IsNumericComponent(string component, int minDigits, int maxDigits) =>
        component.Length >= minDigits
        && component.Length <= maxDigits
        && AreDigits(component, 0, component.Length);

    private static bool AreDigits(string text, int start, int length)
    {
        if (length <= 0)
        {
            return false;
        }

        for (int index = start; index < start + length; index++)
        {
            if (text[index] is < '0' or > '9')
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Builds a number from a string of ASCII digits, one digit at a time.</summary>
    private static int ReadNumber(string text)
    {
        int value = 0;
        foreach (char digit in text)
        {
            value = (value * 10) + (digit - '0');
        }

        return value;
    }
}
