using CCISDeveloperAssessment.Dates;

namespace CCISDeveloperAssessment.Tests;

/// <summary>
/// Tests for the manual date parser, which accepts <c>d/M/yyyy</c> and <c>dd/MM/yyyy</c> (one or
/// two digits for the day and month, exactly four for the year) and normalises every result to
/// <c>dd/MM/yyyy</c>. No built-in date/time API is used to build expectations - every expected
/// value is a literal.
/// </summary>
public class DateParserTests
{
    [Theory]
    // Two digit day and month.
    [InlineData("31/01/2016", 31, 1, 2016)]
    [InlineData("01/02/2016", 1, 2, 2016)]
    [InlineData("02/02/2026", 2, 2, 2026)]
    [InlineData("29/02/2024", 29, 2, 2024)]
    [InlineData("29/02/2000", 29, 2, 2000)]
    [InlineData("31/12/9999", 31, 12, 9999)]
    [InlineData("01/01/0001", 1, 1, 1)]
    // Single digit day and month.
    [InlineData("2/2/2026", 2, 2, 2026)]
    [InlineData("1/1/2016", 1, 1, 2016)]
    [InlineData("9/9/0999", 9, 9, 999)]
    [InlineData("29/2/2024", 29, 2, 2024)]
    // Mixed widths.
    [InlineData("2/02/2026", 2, 2, 2026)]
    [InlineData("02/2/2026", 2, 2, 2026)]
    [InlineData("31/1/2016", 31, 1, 2016)]
    [InlineData("1/12/2016", 1, 12, 2016)]
    // Surrounding whitespace is ignored.
    [InlineData("  31/01/2016  ", 31, 1, 2016)]
    [InlineData(" 2/2/2026 ", 2, 2, 2026)]
    public void TryParse_ValidDates_ReturnsComponents(string input, int expectedDay, int expectedMonth, int expectedYear)
    {
        Assert.True(DateParser.TryParse(input, out ManualDate? date, out string? error));

        Assert.NotNull(date);
        Assert.Equal(expectedDay, date.Day);
        Assert.Equal(expectedMonth, date.Month);
        Assert.Equal(expectedYear, date.Year);
        Assert.Null(error);
    }

    [Theory]
    [InlineData("2/2/2026", "02/02/2026")]
    [InlineData("02/2/2026", "02/02/2026")]
    [InlineData("2/02/2026", "02/02/2026")]
    [InlineData("02/02/2026", "02/02/2026")]
    [InlineData("31/1/2016", "31/01/2016")]
    [InlineData("1/12/2016", "01/12/2016")]
    [InlineData("31/01/2016", "31/01/2016")]
    [InlineData("1/1/0001", "01/01/0001")]
    [InlineData("9/9/0999", "09/09/0999")]
    [InlineData("31/12/9999", "31/12/9999")]
    public void TryParse_NormalisesTheResultToDdMmYyyy(string input, string expected)
    {
        ManualDate date = DateParser.Parse(input);

        Assert.Equal(expected, DateCalculator.Format(date));
        Assert.Equal(expected, date.ToString());
        Assert.Equal(10, DateCalculator.Format(date).Length);
    }

    [Fact]
    public void TryParse_AllFourSpellingsOfTheSameDate_ProduceEqualDates()
    {
        ManualDate shortForm = DateParser.Parse("2/2/2026");
        ManualDate paddedDay = DateParser.Parse("02/2/2026");
        ManualDate paddedMonth = DateParser.Parse("2/02/2026");
        ManualDate longForm = DateParser.Parse("02/02/2026");

        Assert.Equal(shortForm, paddedDay);
        Assert.Equal(shortForm, paddedMonth);
        Assert.Equal(shortForm, longForm);
    }

    [Fact]
    public void TryParse_SingleDigitDateThenOneDayAdded_IsFormattedWithLeadingZeroes()
    {
        ManualDate start = DateParser.Parse("2/2/2026");

        ManualDate result = DateCalculator.AddDays(start, 1);

        Assert.Equal("03/02/2026", DateCalculator.Format(result));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("    ")]
    public void TryParse_MissingInput_IsRejected(string? input)
    {
        Assert.False(DateParser.TryParse(input, out ManualDate? date, out string? error));

        Assert.Null(date);
        Assert.Contains("dd/MM/yyyy", error);
    }

    [Theory]
    // Wrong number of year digits.
    [InlineData("31/01/16")]
    [InlineData("2/2/26")]
    [InlineData("2/2/226")]
    [InlineData("2/2/20266")]
    [InlineData("31/01/20166")]
    // Too many digits in the day or month.
    [InlineData("002/02/2026")]
    [InlineData("2/002/2026")]
    [InlineData("311/01/2016")]
    // Wrong number of components or extra separators.
    [InlineData("2/2")]
    [InlineData("2026")]
    [InlineData("2/2/2026/1")]
    [InlineData("31//01/2016")]
    [InlineData("31/01/2016/")]
    [InlineData("/2/2026")]
    [InlineData("2//2026")]
    [InlineData("2/2/")]
    [InlineData("//")]
    // Wrong separators.
    [InlineData("2016-01-31")]
    [InlineData("31-01-2016")]
    [InlineData("31.01.2016")]
    [InlineData("2 2 2026")]
    // Non-numeric or signed components.
    [InlineData("today")]
    [InlineData("aa/bb/cccc")]
    [InlineData("2/Feb/2026")]
    [InlineData("+2/2/2026")]
    [InlineData("2/-2/2026")]
    [InlineData("2/2/-026")]
    [InlineData("2 /2/2026")]
    [InlineData("2/ 2/2026")]
    [InlineData("2.5/2/2026")]
    [InlineData("3１/01/2016")]        // full-width digit must not be accepted
    public void TryParse_MalformedInput_IsRejected(string input)
    {
        Assert.False(DateParser.TryParse(input, out ManualDate? date, out string? error));

        Assert.Null(date);
        Assert.NotNull(error);
    }

    [Theory]
    [InlineData("31/00/2016")]
    [InlineData("1/0/2026")]
    [InlineData("01/13/2016")]
    [InlineData("1/13/2026")]
    [InlineData("01/99/2016")]
    public void TryParse_InvalidMonth_IsRejected(string input)
    {
        Assert.False(DateParser.TryParse(input, out _, out string? error));

        Assert.Contains("month", error);
    }

    [Theory]
    [InlineData("00/01/2016")]
    [InlineData("0/1/2026")]
    [InlineData("32/01/2016")]
    [InlineData("31/04/2024")]        // April has 30 days
    [InlineData("31/4/2026")]
    [InlineData("31/06/2024")]
    [InlineData("31/9/2024")]
    [InlineData("31/11/2024")]
    [InlineData("30/02/2024")]
    [InlineData("30/2/2024")]
    public void TryParse_DayThatDoesNotExistInTheMonth_IsRejected(string input)
    {
        Assert.False(DateParser.TryParse(input, out _, out string? error));

        Assert.NotNull(error);
    }

    [Theory]
    [InlineData("29/02/2023")]        // not divisible by 4
    [InlineData("29/2/2023")]
    [InlineData("29/02/1900")]        // divisible by 100 but not 400
    [InlineData("29/2/1900")]
    [InlineData("29/02/2100")]
    public void TryParse_LeapDayInNonLeapYear_IsRejected(string input)
    {
        Assert.False(DateParser.TryParse(input, out _, out string? error));

        Assert.Contains("28 days", error);
    }

    [Theory]
    [InlineData("29/02/2024")]
    [InlineData("29/2/2024")]
    [InlineData("29/02/2000")]
    [InlineData("29/2/2000")]
    [InlineData("29/02/1600")]
    public void TryParse_LeapDayInLeapYear_IsAccepted(string input)
    {
        Assert.True(DateParser.TryParse(input, out ManualDate? date, out _));

        Assert.NotNull(date);
        Assert.Equal(29, date.Day);
        Assert.Equal(2, date.Month);
    }

    [Theory]
    [InlineData("01/01/0000")]
    [InlineData("1/1/0000")]
    public void TryParse_YearBelowSupportedRange_IsRejected(string input)
    {
        Assert.False(DateParser.TryParse(input, out _, out string? error));

        Assert.Contains("year", error);
    }

    [Theory]
    [InlineData("01/01/0001", "01/01/0001")]
    [InlineData("1/1/0001", "01/01/0001")]
    [InlineData("31/12/9999", "31/12/9999")]
    public void TryParse_SupportedYearBoundaries_AreAccepted(string input, string expected)
    {
        Assert.True(DateParser.TryParse(input, out ManualDate? date, out _));

        Assert.NotNull(date);
        Assert.Equal(expected, DateCalculator.Format(date));
    }

    [Fact]
    public void Parse_ValidDate_ReturnsDate()
    {
        ManualDate date = DateParser.Parse("31/01/2016");

        Assert.Equal("31/01/2016", date.ToString());
    }

    [Theory]
    [InlineData("31/02/2016")]
    [InlineData("31/2/2016")]
    [InlineData("2/2/16")]
    public void Parse_InvalidDate_ThrowsFormatException(string input)
    {
        Assert.Throws<FormatException>(() => DateParser.Parse(input));
    }

    [Theory]
    [InlineData("0", 0)]
    [InlineData("1", 1)]
    [InlineData("45", 45)]
    [InlineData("000045", 45)]
    [InlineData(" 7 ", 7)]
    [InlineData("3652058", 3_652_058)]
    public void TryParseDaysToAdd_ValidCounts_AreAccepted(string input, int expected)
    {
        Assert.True(DateParser.TryParseDaysToAdd(input, out int days, out string? error));

        Assert.Equal(expected, days);
        Assert.Null(error);
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("-100")]
    public void TryParseDaysToAdd_NegativeCounts_AreRejected(string input)
    {
        Assert.False(DateParser.TryParseDaysToAdd(input, out int days, out string? error));

        Assert.Equal(0, days);
        Assert.Contains("cannot be negative", error);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("one")]
    [InlineData("1.5")]
    [InlineData("1,000")]
    [InlineData("+5")]
    [InlineData("5 days")]
    public void TryParseDaysToAdd_MalformedCounts_AreRejected(string? input)
    {
        Assert.False(DateParser.TryParseDaysToAdd(input, out int days, out string? error));

        Assert.Equal(0, days);
        Assert.NotNull(error);
    }

    [Theory]
    [InlineData("3652059")]
    [InlineData("999999999")]
    [InlineData("99999999999999999999")]
    public void TryParseDaysToAdd_CountsBeyondTheSupportedCalendar_AreRejected(string input)
    {
        Assert.False(DateParser.TryParseDaysToAdd(input, out _, out string? error));

        Assert.Contains("At most", error);
    }
}
