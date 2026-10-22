using CCISDeveloperAssessment.Dates;

namespace CCISDeveloperAssessment.Tests;

/// <summary>
/// Tests for the manual calendar arithmetic. Every expected result is a hand-written literal:
/// no <c>DateTime</c>, <c>DateOnly</c>, <c>TimeSpan</c> or other date/time API is used to
/// produce expectations.
/// </summary>
public class DateCalculatorTests
{
    [Theory]
    // The assessment example.
    [InlineData("31/01/2016", 1, "01/02/2016")]
    // Adding zero days.
    [InlineData("31/01/2016", 0, "31/01/2016")]
    [InlineData("01/01/0001", 0, "01/01/0001")]
    [InlineData("31/12/9999", 0, "31/12/9999")]
    // Single digit input is accepted and the result is normalised to dd/MM/yyyy.
    [InlineData("2/2/2026", 1, "03/02/2026")]
    [InlineData("1/1/2024", 1, "02/01/2024")]
    [InlineData("28/2/2023", 1, "01/03/2023")]
    [InlineData("31/1/2016", 1, "01/02/2016")]
    // Ordinary increments inside a month.
    [InlineData("01/01/2024", 1, "02/01/2024")]
    [InlineData("05/06/2024", 10, "15/06/2024")]
    [InlineData("01/03/2024", 30, "31/03/2024")]
    // Month-end transitions.
    [InlineData("30/04/2024", 1, "01/05/2024")]
    [InlineData("31/03/2024", 1, "01/04/2024")]
    [InlineData("30/11/2024", 1, "01/12/2024")]
    [InlineData("31/01/2016", 29, "29/02/2016")]
    [InlineData("31/01/2016", 30, "01/03/2016")]
    // Year transitions.
    [InlineData("31/12/2024", 1, "01/01/2025")]
    [InlineData("31/12/2024", 366, "01/01/2026")]
    [InlineData("01/01/2023", 365, "01/01/2024")]
    // Leap years: 2024 and 2000 are leap years, 2023 and 1900 are not.
    [InlineData("28/02/2024", 1, "29/02/2024")]
    [InlineData("29/02/2024", 1, "01/03/2024")]
    [InlineData("28/02/2023", 1, "01/03/2023")]
    [InlineData("28/02/1900", 1, "01/03/1900")]
    [InlineData("28/02/2000", 1, "29/02/2000")]
    [InlineData("28/02/2100", 1, "01/03/2100")]
    // A leap year is 366 days, a common year 365.
    [InlineData("01/01/2024", 366, "01/01/2025")]
    [InlineData("01/01/2025", 365, "01/01/2026")]
    // Larger spans.
    [InlineData("01/01/2024", 1000, "27/09/2026")]
    [InlineData("31/01/2016", 10_000, "18/06/2043")]
    [InlineData("01/01/0001", 3_652_058, "31/12/9999")]
    public void AddDays_ReturnsExpectedDate(string startDate, int daysToAdd, string expected)
    {
        ManualDate start = DateParser.Parse(startDate);

        ManualDate result = DateCalculator.AddDays(start, daysToAdd);

        Assert.Equal(expected, DateCalculator.Format(result));
    }

    [Fact]
    public void AddDays_OneDayAtATime_WalksTheWholeOfTwoThousandTwentyFour()
    {
        // 2024 is a leap year, so 01/01/2024 plus 366 single-day steps is 01/01/2025.
        ManualDate date = DateParser.Parse("01/01/2024");

        for (int step = 0; step < 366; step++)
        {
            date = DateCalculator.AddDays(date, 1);
        }

        Assert.Equal("01/01/2025", DateCalculator.Format(date));
    }

    [Fact]
    public void AddDays_AddingInOneStepMatchesAddingDayByDay()
    {
        ManualDate start = DateParser.Parse("15/11/2023");
        ManualDate stepped = start;

        for (int step = 0; step < 500; step++)
        {
            stepped = DateCalculator.AddDays(stepped, 1);
        }

        ManualDate inOneGo = DateCalculator.AddDays(start, 500);

        Assert.Equal(DateCalculator.Format(stepped), DateCalculator.Format(inOneGo));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-365)]
    [InlineData(int.MinValue)]
    public void AddDays_NegativeDayCount_Throws(int daysToAdd)
    {
        ManualDate start = DateParser.Parse("31/01/2016");

        Assert.Throws<ArgumentOutOfRangeException>(() => DateCalculator.AddDays(start, daysToAdd));
    }

    [Theory]
    [InlineData("31/12/9999", 1)]
    [InlineData("01/01/9999", 365)]
    [InlineData("01/01/0001", 3_652_059)]
    [InlineData("31/01/2016", int.MaxValue)]
    public void AddDays_ResultBeyondTheSupportedRange_Throws(string startDate, int daysToAdd)
    {
        ManualDate start = DateParser.Parse(startDate);

        Assert.Throws<ArgumentOutOfRangeException>(() => DateCalculator.AddDays(start, daysToAdd));
    }

    [Fact]
    public void AddDays_NullStartDate_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => DateCalculator.AddDays(null!, 1));
    }

    [Theory]
    [InlineData(2024, true)]
    [InlineData(2016, true)]
    [InlineData(2000, true)]
    [InlineData(1600, true)]
    [InlineData(4, true)]
    [InlineData(2023, false)]
    [InlineData(2025, false)]
    [InlineData(1900, false)]
    [InlineData(2100, false)]
    [InlineData(1800, false)]
    [InlineData(1, false)]
    [InlineData(9999, false)]
    public void IsLeapYear_AppliesTheGregorianRule(int year, bool expected)
    {
        Assert.Equal(expected, DateCalculator.IsLeapYear(year));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-4)]
    [InlineData(10_000)]
    public void IsLeapYear_YearOutsideSupportedRange_Throws(int year)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => DateCalculator.IsLeapYear(year));
    }

    [Theory]
    [InlineData(2023, 1, 31)]
    [InlineData(2023, 2, 28)]
    [InlineData(2024, 2, 29)]
    [InlineData(1900, 2, 28)]
    [InlineData(2000, 2, 29)]
    [InlineData(2024, 3, 31)]
    [InlineData(2024, 4, 30)]
    [InlineData(2024, 5, 31)]
    [InlineData(2024, 6, 30)]
    [InlineData(2024, 7, 31)]
    [InlineData(2024, 8, 31)]
    [InlineData(2024, 9, 30)]
    [InlineData(2024, 10, 31)]
    [InlineData(2024, 11, 30)]
    [InlineData(2024, 12, 31)]
    public void DaysInMonth_ReturnsManualMonthLengths(int year, int month, int expected)
    {
        Assert.Equal(expected, DateCalculator.DaysInMonth(year, month));
    }

    [Theory]
    [InlineData(2024, 0)]
    [InlineData(2024, 13)]
    [InlineData(2024, -1)]
    public void DaysInMonth_InvalidMonth_Throws(int year, int month)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => DateCalculator.DaysInMonth(year, month));
    }

    [Fact]
    public void DaysInMonth_SummedOverAYear_Is365Or366()
    {
        int common = 0;
        int leap = 0;
        for (int month = 1; month <= 12; month++)
        {
            common += DateCalculator.DaysInMonth(2023, month);
            leap += DateCalculator.DaysInMonth(2024, month);
        }

        Assert.Equal(365, common);
        Assert.Equal(366, leap);
    }

    [Theory]
    [InlineData(2016, 1, 31, "31/01/2016")]
    [InlineData(2016, 2, 1, "01/02/2016")]
    [InlineData(1, 1, 1, "01/01/0001")]
    [InlineData(9999, 12, 31, "31/12/9999")]
    [InlineData(999, 9, 9, "09/09/0999")]
    [InlineData(2024, 12, 5, "05/12/2024")]
    public void Format_PadsDayMonthAndYearWithLeadingZeroes(int year, int month, int day, string expected)
    {
        ManualDate date = ManualDate.Create(year, month, day);

        Assert.Equal(expected, DateCalculator.Format(date));
        Assert.Equal(expected, date.ToString());
        Assert.Equal(10, expected.Length);
    }

    [Fact]
    public void Format_NullDate_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => DateCalculator.Format(null!));
    }

    [Theory]
    [InlineData(2024, 4, 31)]
    [InlineData(2023, 2, 29)]
    [InlineData(2024, 2, 30)]
    [InlineData(2024, 1, 0)]
    [InlineData(2024, 13, 1)]
    [InlineData(0, 1, 1)]
    [InlineData(10_000, 1, 1)]
    public void ManualDate_Create_InvalidComponents_Throws(int year, int month, int day)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ManualDate.Create(year, month, day));
        Assert.False(ManualDate.TryCreate(year, month, day, out ManualDate? date));
        Assert.Null(date);
    }

    [Fact]
    public void ManualDate_Equality_ComparesComponents()
    {
        ManualDate first = ManualDate.Create(2016, 1, 31);
        ManualDate second = ManualDate.Create(2016, 1, 31);
        ManualDate different = ManualDate.Create(2016, 2, 1);

        Assert.Equal(first, second);
        Assert.True(first == second);
        Assert.False(first != second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.NotEqual(first, different);
        Assert.True(first != different);
    }

    [Fact]
    public void MaxDaysToAdd_IsTheSpanOfTheSupportedCalendar()
    {
        // 9999 years: 9999 * 365 ordinary days plus 2424 leap days, minus one because the
        // start date itself is not added.
        int leapYears = 0;
        for (int year = ManualDate.MinYear; year <= ManualDate.MaxYear; year++)
        {
            if (DateCalculator.IsLeapYear(year))
            {
                leapYears++;
            }
        }

        Assert.Equal(2424, leapYears);
        Assert.Equal(DateCalculator.MaxDaysToAdd, (9999 * 365) + leapYears - 1);
    }
}
