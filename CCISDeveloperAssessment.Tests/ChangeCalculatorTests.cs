using CCISDeveloperAssessment.Currency;

namespace CCISDeveloperAssessment.Tests;

public class ChangeCalculatorTests
{
    [Fact]
    public void Calculate_TwentyPoundsForFivePoundsFifty_ReturnsAssessmentExample()
    {
        ChangeResult result = ChangeCalculator.Calculate(amountPaidInPence: 2000, purchasePriceInPence: 550);

        Assert.Equal(1450, result.ChangeInPence);
        Assert.Equal(
            new[] { "1 x £10", "2 x £2", "1 x 50p" },
            result.Breakdown.Select(line => line.ToString()));
    }

    [Fact]
    public void Calculate_ExactPayment_ReturnsNoDenominations()
    {
        ChangeResult result = ChangeCalculator.Calculate(550, 550);

        Assert.Equal(0, result.ChangeInPence);
        Assert.False(result.HasChange);
        Assert.Empty(result.Breakdown);
        Assert.Equal(0, result.TotalItemCount);
    }

    [Fact]
    public void Calculate_ZeroPaidForFreeItem_ReturnsZeroChange()
    {
        ChangeResult result = ChangeCalculator.Calculate(0, 0);

        Assert.Equal(0, result.ChangeInPence);
        Assert.False(result.HasChange);
        Assert.Empty(result.Breakdown);
    }

    [Fact]
    public void Calculate_OnePennyChange_ReturnsSinglePenny()
    {
        ChangeResult result = ChangeCalculator.Calculate(100, 99);

        DenominationCount line = Assert.Single(result.Breakdown);
        Assert.Equal(1, line.Denomination.ValueInPence);
        Assert.Equal(1, line.Count);
        Assert.Equal("1 x 1p", line.ToString());
    }

    [Fact]
    public void Calculate_OneOfEveryDenomination_ReturnsOneOfEachLargestFirst()
    {
        // 5000 + 2000 + 1000 + 500 + 200 + 100 + 50 + 20 + 10 + 5 + 2 + 1 = 8888 pence.
        ChangeResult result = ChangeCalculator.Calculate(8888, 0);

        Assert.Equal(
            new[] { "1 x £50", "1 x £20", "1 x £10", "1 x £5", "1 x £2", "1 x £1", "1 x 50p", "1 x 20p", "1 x 10p", "1 x 5p", "1 x 2p", "1 x 1p" },
            result.Breakdown.Select(line => line.ToString()));
        Assert.Equal(12, result.TotalItemCount);
    }

    [Theory]
    [InlineData(10000, 0, "2 x £50")]
    [InlineData(5000, 4999, "1 x 1p")]
    [InlineData(5000, 4995, "1 x 5p")]
    [InlineData(2000, 1, "1 x £10|1 x £5|2 x £2|1 x 50p|2 x 20p|1 x 5p|2 x 2p")]
    [InlineData(100000, 1234, "19 x £50|1 x £20|1 x £10|1 x £5|1 x £2|1 x 50p|1 x 10p|1 x 5p|1 x 1p")]
    public void Calculate_MixedAmounts_ReturnsExpectedBreakdown(long paidInPence, long priceInPence, string expected)
    {
        ChangeResult result = ChangeCalculator.Calculate(paidInPence, priceInPence);

        Assert.Equal(expected.Split('|'), result.Breakdown.Select(line => line.ToString()));
    }

    [Fact]
    public void Calculate_Breakdown_IsOrderedByDescendingDenomination()
    {
        ChangeResult result = ChangeCalculator.Calculate(9999, 0);

        long[] values = result.Breakdown.Select(line => line.Denomination.ValueInPence).ToArray();
        Assert.Equal(values.OrderByDescending(value => value).ToArray(), values);
    }

    [Fact]
    public void Calculate_Breakdown_NeverContainsZeroCounts()
    {
        for (int change = 0; change <= 5000; change++)
        {
            ChangeResult result = ChangeCalculator.Calculate(change, 0);
            Assert.All(result.Breakdown, line => Assert.True(line.Count > 0));
        }
    }

    [Fact]
    public void Calculate_Breakdown_AlwaysSumsToTheChangeDue()
    {
        for (long change = 0; change <= 10_000; change++)
        {
            ChangeResult result = ChangeCalculator.Calculate(change + 1234, 1234);

            long total = result.Breakdown.Sum(line => line.TotalInPence);
            Assert.Equal(change, total);
            Assert.Equal(change, result.ChangeInPence);
        }
    }

    [Fact]
    public void Calculate_GreedyBreakdown_UsesFewestPossibleNotesAndCoins()
    {
        // Dynamic-programming reference: the minimum number of items for every amount up to £50.
        const int limit = 5000;
        long[] minimumItems = new long[limit + 1];
        for (int amount = 1; amount <= limit; amount++)
        {
            minimumItems[amount] = long.MaxValue;
            foreach (Denomination denomination in ChangeCalculator.Denominations)
            {
                if (denomination.ValueInPence <= amount
                    && minimumItems[amount - denomination.ValueInPence] != long.MaxValue)
                {
                    minimumItems[amount] = Math.Min(
                        minimumItems[amount],
                        minimumItems[amount - denomination.ValueInPence] + 1);
                }
            }
        }

        for (int amount = 0; amount <= limit; amount++)
        {
            ChangeResult result = ChangeCalculator.Calculate(amount, 0);
            Assert.Equal(minimumItems[amount], result.TotalItemCount);
        }
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    [InlineData(-100, -100)]
    [InlineData(long.MinValue, 0)]
    public void Calculate_NegativeAmounts_Throws(long paidInPence, long priceInPence)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ChangeCalculator.Calculate(paidInPence, priceInPence));
    }

    [Theory]
    [InlineData(549, 550)]
    [InlineData(0, 1)]
    [InlineData(1999, 2000)]
    [InlineData(0, ChangeCalculator.MaxAmountInPence)]
    public void Calculate_PaymentBelowPurchasePrice_Throws(long paidInPence, long priceInPence)
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => ChangeCalculator.Calculate(paidInPence, priceInPence));

        Assert.Contains("less than the purchase price", exception.Message);
    }

    [Fact]
    public void Calculate_MaximumSupportedAmount_DoesNotOverflow()
    {
        ChangeResult result = ChangeCalculator.Calculate(ChangeCalculator.MaxAmountInPence, 0);

        Assert.Equal(ChangeCalculator.MaxAmountInPence, result.ChangeInPence);
        Assert.Equal(ChangeCalculator.MaxAmountInPence, result.Breakdown.Sum(line => line.TotalInPence));

        // £1,000,000,000,000.00 / £50 = 20,000,000,000 notes, a count that does not fit in int.
        Assert.Equal("20000000000 x £50", result.Breakdown.Single().ToString());
        Assert.Equal(20_000_000_000L, result.TotalItemCount);
    }

    [Fact]
    public void Calculate_MaximumPaidAgainstMaximumPrice_ReturnsZeroChange()
    {
        ChangeResult result = ChangeCalculator.Calculate(
            ChangeCalculator.MaxAmountInPence,
            ChangeCalculator.MaxAmountInPence);

        Assert.Equal(0, result.ChangeInPence);
        Assert.Empty(result.Breakdown);
    }

    [Fact]
    public void Calculate_AmountsAboveIntRange_AreHandledExactly()
    {
        // 50,000,000,000 pence = £500,000,000.00, well beyond int.MaxValue pence.
        const long paid = 50_000_000_000L;
        const long price = 1234L;

        ChangeResult result = ChangeCalculator.Calculate(paid, price);

        Assert.Equal(paid - price, result.ChangeInPence);
        Assert.Equal(paid - price, result.Breakdown.Sum(line => line.TotalInPence));
    }

    [Theory]
    [InlineData(ChangeCalculator.MaxAmountInPence + 1, 0)]
    [InlineData(long.MaxValue, 0)]
    [InlineData(ChangeCalculator.MaxAmountInPence, ChangeCalculator.MaxAmountInPence + 1)]
    public void Calculate_AmountsAboveTheSupportedMaximum_Throws(long paidInPence, long priceInPence)
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => ChangeCalculator.Calculate(paidInPence, priceInPence));

        Assert.Contains("supported maximum", exception.Message);
    }

    [Fact]
    public void MaxAmountInPence_IsFarBelowTheLongCeiling()
    {
        Assert.Equal(100_000_000_000_000L, ChangeCalculator.MaxAmountInPence);
        Assert.Equal(ChangeCalculator.MaxAmountInPence, CurrencyInputParser.MaxAmountInPence);

        // Headroom check: the whole supported range can be summed thousands of times over
        // without approaching long.MaxValue.
        Assert.True(ChangeCalculator.MaxAmountInPence < long.MaxValue / 1000);
    }

    [Fact]
    public void Denominations_AreTheTwelveUkNotesAndCoinsInDescendingOrder()
    {
        Assert.Equal(
            new long[] { 5000, 2000, 1000, 500, 200, 100, 50, 20, 10, 5, 2, 1 },
            ChangeCalculator.Denominations.Select(denomination => denomination.ValueInPence));
    }

    [Fact]
    public void ChangeResult_BreakdownThatDoesNotSumToTheChange_Throws()
    {
        Denomination fivePounds = ChangeCalculator.Denominations.Single(d => d.ValueInPence == 500);

        Assert.Throws<InvalidOperationException>(
            () => new ChangeResult(1000, 0, new[] { new DenominationCount(fivePounds, 1) }));
    }

    [Fact]
    public void ChangeResult_MutatingTheCallersListAfterwards_DoesNotAffectTheResult()
    {
        Denomination fivePounds = ChangeCalculator.Denominations.Single(d => d.ValueInPence == 500);
        Denomination onePenny = ChangeCalculator.Denominations.Single(d => d.ValueInPence == 1);

        // The caller keeps a reference to the concrete list it passed in.
        List<DenominationCount> callerList = new() { new DenominationCount(fivePounds, 1) };
        ChangeResult result = new(500, 0, callerList);

        callerList.Add(new DenominationCount(onePenny, 7));
        callerList.Clear();

        // The result kept its own copy, so the invariant still holds.
        Assert.Equal("1 x £5", Assert.Single(result.Breakdown).ToString());
        Assert.Equal(500, result.ChangeInPence);
        Assert.Equal(500, result.Breakdown.Sum(line => line.TotalInPence));
    }

    [Fact]
    public void ChangeResult_BreakdownCollection_IsNotCastableBackToAMutableList()
    {
        ChangeResult result = ChangeCalculator.Calculate(2000, 550);

        Assert.IsNotType<List<DenominationCount>>(result.Breakdown);
        Assert.IsNotType<DenominationCount[]>(result.Breakdown);
    }

    [Fact]
    public void ChangeResult_NullBreakdownOrNullEntry_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new ChangeResult(0, 0, null!));
        Assert.Throws<ArgumentNullException>(() => new ChangeResult(500, 0, new DenominationCount[] { null! }));
    }

    [Fact]
    public void DenominationCount_ZeroOrNegativeCount_Throws()
    {
        Denomination onePenny = ChangeCalculator.Denominations.Single(d => d.ValueInPence == 1);

        Assert.Throws<ArgumentOutOfRangeException>(() => new DenominationCount(onePenny, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new DenominationCount(onePenny, -1));
    }
}
