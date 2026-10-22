namespace CCISDeveloperAssessment.Dates;

/// <summary>
/// An immutable calendar date held as three integers.
/// </summary>
public sealed class ManualDate : IEquatable<ManualDate>
{
    /// <summary>Earliest supported year (proleptic Gregorian calendar).</summary>
    public const int MinYear = 1;

    /// <summary>Latest supported year.</summary>
    public const int MaxYear = 9999;

    private ManualDate(int year, int month, int day)
    {
        Year = year;
        Month = month;
        Day = day;
    }

    /// <summary>Year, between <see cref="MinYear"/> and <see cref="MaxYear"/>.</summary>
    public int Year { get; }

    /// <summary>Month, 1 to 12.</summary>
    public int Month { get; }

    /// <summary>Day of month, 1 to the number of days in <see cref="Month"/>.</summary>
    public int Day { get; }

    /// <summary>
    /// Creates a validated date.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The year is outside the supported range, the month is not 1-12, or the day does not
    /// exist in that month (for example 31/04 or 29/02 in a non-leap year).
    /// </exception>
    public static ManualDate Create(int year, int month, int day)
    {
        if (year < MinYear || year > MaxYear)
        {
            throw new ArgumentOutOfRangeException(
                nameof(year), year, $"The year must be between {MinYear} and {MaxYear}.");
        }

        if (month is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(month), month, "The month must be between 1 and 12.");
        }

        int daysInMonth = DateCalculator.DaysInMonth(year, month);
        if (day < 1 || day > daysInMonth)
        {
            throw new ArgumentOutOfRangeException(
                nameof(day), day, $"Month {month} of {year} has {daysInMonth} days.");
        }

        return new ManualDate(year, month, day);
    }

    /// <summary>
    /// Attempts to create a validated date.
    /// </summary>
    /// <returns>True when the three components form a real date in the supported range.</returns>
    public static bool TryCreate(int year, int month, int day, out ManualDate? date)
    {
        if (year < MinYear || year > MaxYear || month is < 1 or > 12
            || day < 1 || day > DateCalculator.DaysInMonth(year, month))
        {
            date = null;
            return false;
        }

        date = new ManualDate(year, month, day);
        return true;
    }

    /// <summary>Renders the date as <c>dd/MM/yyyy</c> using manual zero padding.</summary>
    public override string ToString() => DateCalculator.Format(this);

    public bool Equals(ManualDate? other) =>
        other is not null && other.Year == Year && other.Month == Month && other.Day == Day;

    public override bool Equals(object? obj) => Equals(obj as ManualDate);

    public override int GetHashCode() => (Year * 100 + Month) * 100 + Day;

    public static bool operator ==(ManualDate? left, ManualDate? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(ManualDate? left, ManualDate? right) => !(left == right);
}
