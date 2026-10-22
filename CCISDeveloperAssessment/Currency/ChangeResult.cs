namespace CCISDeveloperAssessment.Currency;

/// <summary>
/// A single UK currency denomination (note or coin), expressed in whole pence.
/// </summary>
public sealed class Denomination
{
    /// <param name="valueInPence">Face value in whole pence, for example 2000 for a £20 note.</param>
    /// <param name="label">Human readable label, for example "£20" or "50p".</param>
    public Denomination(long valueInPence, string label)
    {
        if (valueInPence <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(valueInPence), valueInPence, "A denomination must be worth at least one penny.");
        }

        if (string.IsNullOrWhiteSpace(label))
        {
            throw new ArgumentException("A denomination must have a label.", nameof(label));
        }

        ValueInPence = valueInPence;
        Label = label;
    }

    /// <summary>Face value in whole pence.</summary>
    public long ValueInPence { get; }

    /// <summary>Display label such as "£20" or "50p".</summary>
    public string Label { get; }

    public override string ToString() => Label;
}

/// <summary>
/// How many of one denomination make up part of the change. Immutable.
/// </summary>
public sealed class DenominationCount
{
    public DenominationCount(Denomination denomination, long count)
    {
        ArgumentNullException.ThrowIfNull(denomination);

        if (count <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), count, "Only denominations that are actually handed over are reported.");
        }

        Denomination = denomination;
        Count = count;
    }

    public Denomination Denomination { get; }

    public long Count { get; }

    public long TotalInPence => Count * Denomination.ValueInPence;

    public override string ToString() => $"{Count} x {Denomination.Label}";
}

/// <summary>
/// Immutable result of a change calculation: the amounts involved and the
/// notes/coins to hand back, largest denomination first.
/// </summary>
public sealed class ChangeResult
{
   
    public ChangeResult(long amountPaidInPence, long purchasePriceInPence, IReadOnlyList<DenominationCount> breakdown)
    {
        ArgumentNullException.ThrowIfNull(breakdown);
        DenominationCount[] lines = breakdown.ToArray();

        long total = 0;
        foreach (DenominationCount line in lines)
        {
            ArgumentNullException.ThrowIfNull(line, nameof(breakdown));
            total += line.TotalInPence;
        }

        // Invariant: the denominations handed back must add up to exactly the change due.
        long changeInPence = amountPaidInPence - purchasePriceInPence;
        if (total != changeInPence)
        {
            throw new InvalidOperationException(
                $"Breakdown totals {total} pence but the change due is {changeInPence} pence.");
        }

        // Validated, so the instance can now be populated with the private copy.
        AmountPaidInPence = amountPaidInPence;
        PurchasePriceInPence = purchasePriceInPence;
        Breakdown = Array.AsReadOnly(lines);
    }
    
    public long AmountPaidInPence { get; }
    
    public long PurchasePriceInPence { get; }
    
    public long ChangeInPence => AmountPaidInPence - PurchasePriceInPence;
    
    public IReadOnlyList<DenominationCount> Breakdown { get; }
    
    public bool HasChange => ChangeInPence > 0;
    
    public long TotalItemCount
    {
        get
        {
            long count = 0;
            foreach (DenominationCount line in Breakdown)
            {
                count += line.Count;
            }

            return count;
        }
    }
}
