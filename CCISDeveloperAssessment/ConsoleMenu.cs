using System.Diagnostics.CodeAnalysis;
using CCISDeveloperAssessment.Currency;
using CCISDeveloperAssessment.Dates;

namespace CCISDeveloperAssessment;

/// <summary>
/// Drives all console interaction for the assessment: shows the menu, collects input and
/// prints results. It contains no calculation logic of its own - every rule lives in the
/// <see cref="CCISDeveloperAssessment.Currency"/> and <see cref="CCISDeveloperAssessment.Dates"/>
/// namespaces.
/// </summary>
public sealed class ConsoleMenu
{
    /// <summary>Zero-width byte-order mark, stripped from input lines when present.</summary>
    private const char ByteOrderMark = (char)0xFEFF;

    private readonly TextReader _input;
    private readonly TextWriter _output;

    /// <param name="input">Where menu selections and answers are read from.</param>
    /// <param name="output">Where the menu and results are written to.</param>
    public ConsoleMenu(TextReader input, TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(output);

        _input = input;
        _output = output;
    }

    /// <summary>
    /// Shows the menu until the user chooses to exit or the input stream ends.
    /// </summary>
    public void Run()
    {
        while (true)
        {
            WriteMenu();

            if (!TryRead(out string? choice))
            {
                // End of input (for example piped input or Ctrl+Z): stop rather than loop forever.
                _output.WriteLine();
                return;
            }

            switch (choice.Trim())
            {
                case "1":
                    if (!RunChangeCalculator())
                    {
                        return;
                    }

                    break;

                case "2":
                    if (!RunDateAddition())
                    {
                        return;
                    }

                    break;

                case "0":
                    _output.WriteLine("Goodbye.");
                    return;

                default:
                    WriteError($"'{choice.Trim()}' is not a valid option. Please enter 1, 2 or 0.");
                    break;
            }
        }
    }

    private void WriteMenu()
    {
        _output.WriteLine();
        _output.WriteLine("CCIS Developer Test 2026");
        _output.WriteLine();
        _output.WriteLine("1. UK Currency Change Calculator");
        _output.WriteLine("2. Date Addition");
        _output.WriteLine("0. Exit");
        _output.WriteLine();
        _output.Write("Select an option: ");
    }

    /// <summary>
    /// Task A: asks for the amount paid and the purchase price, then prints the change.
    /// </summary>
    /// <returns>False when the input stream has ended and the application should stop.</returns>
    private bool RunChangeCalculator()
    {
        _output.WriteLine();
        _output.WriteLine("UK Currency Change Calculator");

        if (!TryPrompt("Amount paid (GBP): ", out string? paidText))
        {
            return false;
        }

        if (!CurrencyInputParser.TryParsePence(paidText, out long amountPaidInPence, out string? paidError))
        {
            WriteError(paidError);
            return true;
        }

        if (!TryPrompt("Purchase price (GBP): ", out string? priceText))
        {
            return false;
        }

        if (!CurrencyInputParser.TryParsePence(priceText, out long purchasePriceInPence, out string? priceError))
        {
            WriteError(priceError);
            return true;
        }

        try
        {
            ChangeResult result = ChangeCalculator.Calculate(amountPaidInPence, purchasePriceInPence);
            WriteChange(result);
        }
        catch (ArgumentException)
        {
            WriteError(
                $"The amount paid ({CurrencyInputParser.FormatPence(amountPaidInPence)}) is less than the " +
                $"purchase price ({CurrencyInputParser.FormatPence(purchasePriceInPence)}), so no change can be given.");
        }

        return true;
    }

    private void WriteChange(ChangeResult result)
    {
        _output.WriteLine();

        if (!result.HasChange)
        {
            _output.WriteLine("No change is due.");
            return;
        }

        _output.WriteLine("Your change is:");
        foreach (DenominationCount line in result.Breakdown)
        {
            _output.WriteLine(line.ToString());
        }
    }

    /// <summary>
    /// Task B: asks for a start date and a number of days, then prints the resulting date.
    /// </summary>
    /// <returns>False when the input stream has ended and the application should stop.</returns>
    private bool RunDateAddition()
    {
        _output.WriteLine();
        _output.WriteLine("Date Addition");

        if (!TryPrompt($"Input date ({DateParser.AcceptedFormats}): ", out string? dateText))
        {
            return false;
        }

        if (!DateParser.TryParse(dateText, out ManualDate? startDate, out string? dateError))
        {
            WriteError(dateError);
            return true;
        }

        if (!TryPrompt("Days to add: ", out string? daysText))
        {
            return false;
        }

        if (!DateParser.TryParseDaysToAdd(daysText, out int daysToAdd, out string? daysError))
        {
            WriteError(daysError);
            return true;
        }

        try
        {
            ManualDate result = DateCalculator.AddDays(startDate, daysToAdd);
            _output.WriteLine();
            _output.WriteLine($"New Date: {DateCalculator.Format(result)}");
        }
        catch (ArgumentOutOfRangeException)
        {
            WriteError($"The resulting date is outside the supported range (01/01/{ManualDate.MinYear:0000} to 31/12/{ManualDate.MaxYear}).");
        }

        return true;
    }

    private bool TryPrompt(string prompt, [NotNullWhen(true)] out string? answer)
    {
        _output.Write(prompt);
        return TryRead(out answer);
    }

    private bool TryRead([NotNullWhen(true)] out string? line)
    {
        line = _input.ReadLine();

        if (line is null)
        {
            return false;
        }

        // Some shells prepend a byte-order mark when input is piped in; it is not whitespace,
        // so Trim() would leave it in place and every answer would look invalid.
        line = line.TrimStart(ByteOrderMark);
        return true;
    }

    private void WriteError(string message)
    {
        _output.WriteLine();
        _output.WriteLine($"Error: {message}");
    }
}
