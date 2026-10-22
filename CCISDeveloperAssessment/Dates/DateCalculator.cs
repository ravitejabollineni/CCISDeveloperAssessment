using System.Globalization;

namespace CCISDeveloperAssessment.Dates;

/// <summary>
/// Manual calendar arithmetic: leap years, month lengths, day addition and formatting.
/// </summary>
public static class DateCalculator
{
    /// <summary>
    /// Days in each month of a common (non-leap) year, indexed 1-12. Index 0 is unused so the
    /// month number can be used directly.
    /// </summary>
    private static readonly int[] DaysPerMonthCommonYear =
    {
        0, 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31,
    };

    /// <summary>
    /// Total number of days between 01/01/0001 and 31/12/9999 inclusive, which is the largest
    /// number of days that could ever be added to a supported date without leaving the
    /// supported range.
    /// </summary>
    public const int MaxDaysToAdd = 3_652_058;
    
    /// <exception cref="ArgumentOutOfRangeException">The year is outside the supported range.</exception>
    public static bool IsLeapYear(int year)
    {
        if (year < ManualDate.MinYear || year > ManualDate.MaxYear)
        {
            throw new ArgumentOutOfRangeException(
                nameof(year), year, $"The year must be between {ManualDate.MinYear} and {ManualDate.MaxYear}.");
        }

        if (year % 400 == 0)
        {
            return true;
        }

        if (year % 100 == 0)
        {
            return false;
        }

        return year % 4 == 0;
    }

    /// <summary>
    /// Number of days in the given month, taking leap years into account for February.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The year or month is out of range.</exception>
    public static int DaysInMonth(int year, int month)
    {
        if (month is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(month), month, "The month must be between 1 and 12.");
        }

        // Validates the year as a side effect.
        bool isLeapYear = IsLeapYear(year);

        if (month == 2 && isLeapYear)
        {
            return 29;
        }

        return DaysPerMonthCommonYear[month];
    }

    /// <summary>
    /// Adds a non-negative number of days to a date.
    /// </summary>
    /// <param name="start">The starting date.</param>
    /// <param name="daysToAdd">Number of days to add. Zero returns an equal date.</param>
    /// <exception cref="ArgumentNullException"><paramref name="start"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="daysToAdd"/> is negative or greater than <see cref="MaxDaysToAdd"/>, or the
    /// result would fall after 31/12/<see cref="ManualDate.MaxYear"/>.
    /// </exception>
    public static ManualDate AddDays(ManualDate start, int daysToAdd)
    {
        ArgumentNullException.ThrowIfNull(start);

        if (daysToAdd < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(daysToAdd), daysToAdd, "The number of days to add cannot be negative.");
        }

        if (daysToAdd > MaxDaysToAdd)
        {
            throw new ArgumentOutOfRangeException(
                nameof(daysToAdd),
                daysToAdd,
                $"At most {MaxDaysToAdd} days can be added, which is the span of the supported calendar.");
        }

        int year = start.Year;
        int month = start.Month;
        int day = start.Day;
        int remaining = daysToAdd;

        while (remaining > 0)
        {
            int daysLeftInMonth = DaysInMonth(year, month) - day;

            if (remaining <= daysLeftInMonth)
            {
                day += remaining;
                break;
            }

            // Consume the rest of this month plus one day to land on the 1st of the next month.
            remaining -= daysLeftInMonth + 1;
            day = 1;
            month++;

            if (month > 12)
            {
                month = 1;
                year++;

                if (year > ManualDate.MaxYear)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(daysToAdd),
                        daysToAdd,
                        $"The resulting date would be after 31/12/{ManualDate.MaxYear}, which is outside the supported range.");
                }
            }
        }

        return ManualDate.Create(year, month, day);
    }

    /// <summary>
    /// Formats a date as <c>dd/MM/yyyy</c> with leading zeroes, built manually from the
    /// integer components.
    /// </summary>
    public static string Format(ManualDate date)
    {
        ArgumentNullException.ThrowIfNull(date);

        return string.Concat(
            PadWithZeroes(date.Day, 2),
            "/",
            PadWithZeroes(date.Month, 2),
            "/",
            PadWithZeroes(date.Year, 4));
    }

    private static string PadWithZeroes(int value, int width) =>
        value.ToString(CultureInfo.InvariantCulture).PadLeft(width, '0');
}
