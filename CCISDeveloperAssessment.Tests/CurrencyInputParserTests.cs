using CCISDeveloperAssessment.Currency;

namespace CCISDeveloperAssessment.Tests;

public class CurrencyInputParserTests
{
    [Theory]
    [InlineData("0", 0)]
    [InlineData("20", 2000)]
    [InlineData("5.50", 550)]
    [InlineData("5.5", 550)]
    [InlineData("0.01", 1)]
    [InlineData("0.99", 99)]
    [InlineData("£20", 2000)]
    [InlineData("£5.50", 550)]
    [InlineData("£ 5.50", 550)]
    [InlineData("  5.50  ", 550)]
    [InlineData("1,000.00", 100_000)]
    [InlineData("1,000,000.00", 100_000_000)]
    [InlineData("1000000", 100_000_000)]
    // Amounts beyond int.MaxValue pence (£21,474,836.47) must survive as long pence.
    [InlineData("21474836.48", 2_147_483_648L)]
    [InlineData("500000000.00", 50_000_000_000L)]
    [InlineData("999999999999.99", 99_999_999_999_999L)]
    [InlineData("1000000000000.00", 100_000_000_000_000L)]
    [InlineData("1,000,000,000,000.00", 100_000_000_000_000L)]
    public void TryParsePence_ValidAmounts_ReturnsWholePence(string input, long expectedPence)
    {
        Assert.True(CurrencyInputParser.TryParsePence(input, out long pence, out string? error));

        Assert.Equal(expectedPence, pence);
        Assert.Null(error);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryParsePence_MissingInput_IsRejected(string? input)
    {
        Assert.False(CurrencyInputParser.TryParsePence(input, out long pence, out string? error));

        Assert.Equal(0, pence);
        Assert.Contains("Please enter an amount", error);
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("-5.50")]
    [InlineData("£-5.50")]
    public void TryParsePence_NegativeAmounts_IsRejected(string input)
    {
        Assert.False(CurrencyInputParser.TryParsePence(input, out _, out string? error));

        Assert.Contains("cannot be negative", error);
    }

    [Theory]
    [InlineData("5.555")]
    [InlineData("5.500")]
    [InlineData("0.001")]
    [InlineData("20.123456")]
    public void TryParsePence_MoreThanTwoDecimalPlaces_IsRejected(string input)
    {
        Assert.False(CurrencyInputParser.TryParsePence(input, out _, out string? error));

        Assert.Contains("more than two decimal places", error);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("five pounds")]
    [InlineData("5.5.5")]
    [InlineData("5,")]
    [InlineData("5.")]
    [InlineData(".5")]
    [InlineData("£")]
    [InlineData("$5.00")]
    [InlineData("5 pounds")]
    [InlineData("1e3")]
    [InlineData("5p")]
    [InlineData("+5.00")]
    [InlineData("(5.00)")]
    public void TryParsePence_MalformedInput_IsRejected(string input)
    {
        Assert.False(CurrencyInputParser.TryParsePence(input, out long pence, out string? error));

        Assert.Equal(0, pence);
        Assert.NotNull(error);
    }

    [Theory]
    // Just past the supported maximum of £1,000,000,000,000.00.
    [InlineData("1000000000000.01")]
    [InlineData("1,000,000,000,000.01")]
    [InlineData("1000000000001")]
    [InlineData("9999999999999999")]
    // Above long pence range (long.MaxValue is 9,223,372,036,854,775,807 pence) but still a
    // perfectly valid decimal: the parser must reject it rather than overflow when scaling.
    [InlineData("92233720368547758.07")]
    [InlineData("92233720368547758.08")]
    [InlineData("99999999999999999999.99")]
    // Beyond decimal's own range, so decimal.TryParse itself fails.
    [InlineData("99999999999999999999999999999999999999999")]
    [InlineData("792281625142643375935439503356.00")]
    public void TryParsePence_AmountsAboveTheSupportedMaximum_IsRejected(string input)
    {
        Assert.False(CurrencyInputParser.TryParsePence(input, out long pence, out string? error));

        Assert.Equal(0, pence);
        Assert.Contains("too large", error);
    }

    [Fact]
    public void TryParsePence_MaximumSupportedAmount_IsAccepted()
    {
        Assert.True(CurrencyInputParser.TryParsePence("1000000000000.00", out long pence, out _));

        Assert.Equal(CurrencyInputParser.MaxAmountInPence, pence);
        Assert.Equal(ChangeCalculator.MaxAmountInPence, pence);
    }

    [Fact]
    public void TryParsePence_MaximumSupportedAmount_IsAcceptedByTheCalculatorToo()
    {
        long paid = CurrencyInputParser.ParsePence("1000000000000.00");
        long price = CurrencyInputParser.ParsePence("0.01");

        ChangeResult result = ChangeCalculator.Calculate(paid, price);

        Assert.Equal(99_999_999_999_999L, result.ChangeInPence);
        Assert.Equal(result.ChangeInPence, result.Breakdown.Sum(line => line.TotalInPence));
    }

    [Fact]
    public void ParsePence_ValidAmount_ReturnsPence()
    {
        Assert.Equal(550, CurrencyInputParser.ParsePence("£5.50"));
    }

    [Fact]
    public void ParsePence_InvalidAmount_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => CurrencyInputParser.ParsePence("not money"));
    }

    [Theory]
    [InlineData(0, "£0.00")]
    [InlineData(1, "£0.01")]
    [InlineData(550, "£5.50")]
    [InlineData(2000, "£20.00")]
    [InlineData(100_000_000, "£1000000.00")]
    [InlineData(2_147_483_648L, "£21474836.48")]
    [InlineData(100_000_000_000_000L, "£1000000000000.00")]
    [InlineData(long.MaxValue, "£92233720368547758.07")]
    public void FormatPence_RendersPoundsAndPence(long pence, string expected)
    {
        Assert.Equal(expected, CurrencyInputParser.FormatPence(pence));
    }

    [Fact]
    public void FormatPence_NegativePence_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CurrencyInputParser.FormatPence(-1));
    }
}
