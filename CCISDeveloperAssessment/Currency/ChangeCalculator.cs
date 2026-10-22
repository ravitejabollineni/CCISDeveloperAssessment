namespace CCISDeveloperAssessment.Currency;

/// <summary>
/// Works out the change due for a purchase using UK notes and coins.
/// </summary>
// Why the greedy algorithm is valid here
// ---------------------------------------
// Taking as many of the largest denomination that fits as possible only guarantees the fewest
// notes and coins when the denomination set is *canonical*. The UK set is the 1-2-5 decimal
// series (1, 2, 5, 10, 20, 50, 100, 200, 500, 1000, 2000, 5000 pence), which is canonical:
// within each decade, what is left after taking a 5 is at most 4 (covered optimally by 2+2, 2+1
// or 1), and what is left after taking a 2 is at most 1. A non-canonical set such as {1, 3, 4}
// would defeat greedy - 6 is 3+3, but greedy gives 4+1+1 - which is why Denominations is fixed
// here rather than supplied by the caller. Including 1p means any amount can be made exactly,
// and each step leaves a remainder smaller than the denomination just used, so the loop always
// terminates with nothing left over. ChangeCalculatorTests verifies minimality against a
// dynamic-programming reference for every amount up to £50.
//
// All arithmetic is integer long pence. double/float are never used for money: 0.1 has no exact
// binary representation, so repeated subtraction would drift.
public static class ChangeCalculator
{
    /// <summary>
    /// Largest amount this calculator supports, in whole pence: 100,000,000,000,000 pence,
    /// which is £1,000,000,000,000.00 (one trillion pounds).
    /// </summary>
    /// <remarks>
    /// Money is stored as <see cref="long"/> pence, so the hard technical ceiling is
    /// <see cref="long.MaxValue"/> (9,223,372,036,854,775,807 pence). The supported maximum is
    /// deliberately set far below that, for two reasons: it is a round figure that can be
    /// reported to the user sensibly, and it leaves roughly five orders of magnitude of headroom
    /// so that sums and products of supported amounts still cannot overflow.
    /// </remarks>
    public const long MaxAmountInPence = 100_000_000_000_000L;

    /// <summary>
    /// UK notes and coins in descending order of value. Order matters: the greedy
    /// algorithm and the reported breakdown both rely on it being largest first.
    /// </summary>
    public static readonly IReadOnlyList<Denomination> Denominations = new[]
    {
        new Denomination(5000, "£50"),
        new Denomination(2000, "£20"),
        new Denomination(1000, "£10"),
        new Denomination(500, "£5"),
        new Denomination(200, "£2"),
        new Denomination(100, "£1"),
        new Denomination(50, "50p"),
        new Denomination(20, "20p"),
        new Denomination(10, "10p"),
        new Denomination(5, "5p"),
        new Denomination(2, "2p"),
        new Denomination(1, "1p"),
    };

    /// <summary>
    /// Calculates the change due for a purchase.
    /// </summary>
    /// <param name="amountPaidInPence">
    /// Amount tendered, in whole pence. Must be between 0 and <see cref="MaxAmountInPence"/>.
    /// </param>
    /// <param name="purchasePriceInPence">
    /// Purchase price, in whole pence. Must be between 0 and <see cref="MaxAmountInPence"/>.
    /// </param>
    /// <returns>The change due, broken down into denominations largest first.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Either amount is negative or above <see cref="MaxAmountInPence"/>.
    /// </exception>
    /// <exception cref="ArgumentException">The amount paid is less than the purchase price.</exception>
    public static ChangeResult Calculate(long amountPaidInPence, long purchasePriceInPence)
    {
        if (amountPaidInPence < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(amountPaidInPence), amountPaidInPence, "The amount paid cannot be negative.");
        }

        if (purchasePriceInPence < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(purchasePriceInPence), purchasePriceInPence, "The purchase price cannot be negative.");
        }
        
        if (amountPaidInPence > MaxAmountInPence)
        {
            throw new ArgumentOutOfRangeException(
                nameof(amountPaidInPence),
                amountPaidInPence,
                $"The amount paid is above the supported maximum of {MaxAmountInPence} pence.");
        }

        if (purchasePriceInPence > MaxAmountInPence)
        {
            throw new ArgumentOutOfRangeException(
                nameof(purchasePriceInPence),
                purchasePriceInPence,
                $"The purchase price is above the supported maximum of {MaxAmountInPence} pence.");
        }
       
        if (amountPaidInPence < purchasePriceInPence)
        {
            throw new ArgumentException(
                "The amount paid is less than the purchase price, so no change can be given.",
                nameof(amountPaidInPence));
        }

        // Both operands are non-negative and paid >= price, so the result is in [0, Max].
        long remaining = amountPaidInPence - purchasePriceInPence;

        List<DenominationCount> breakdown = new(Denominations.Count);
        foreach (Denomination denomination in Denominations)
        {
            long count = remaining / denomination.ValueInPence;
            if (count > 0)
            {
                breakdown.Add(new DenominationCount(denomination, count));
                remaining -= count * denomination.ValueInPence;
            }
        }

        // The set includes 1p, so the remainder is always driven to zero.
        return new ChangeResult(amountPaidInPence, purchasePriceInPence, breakdown);
    }
}
