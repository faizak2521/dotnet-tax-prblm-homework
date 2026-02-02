// Faiza Khan
// 1 Feb. 2026

using System.Globalization; // for NumberStyles

// try for exceptions during file read and parsing
try
{
    // Load tax brackets from CSV
    var brackets = ReadTaxTable("tax_table.csv");

    // Prompt user for taxable income
    Console.Write("Enter taxable income: ");
    string? input = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(input))
        throw new ArgumentException("No income was entered.");

    // Use invariant culture so 17894.00 parses reliably (not locale-dependent **add to notes)
    if (!decimal.TryParse(input.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out decimal income))
        throw new FormatException("Income must be a valid number (example: 25000 or 25000.00).");

    if (income < 0)
        throw new ArgumentOutOfRangeException(nameof(income), "Income must be non-negative.");

    // Calculate tax
    decimal tax = TaxCalculator.Calculate(income, brackets);

    // Print result
    Console.WriteLine($"Tax owed: ${tax:F2}");
}
catch (FileNotFoundException)
{
    Console.WriteLine("Error: Could not find tax_table.csv. Make sure it is in the project root and set to copy to output.");
}
catch (UnauthorizedAccessException)
{
    Console.WriteLine("Error: Permission issue reading tax_table.csv.");
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}

// Reads tax bracket data from CSV file into a list of TaxBracket objects.
// Expected CSV columns (with a header row):
// minIncome,maxIncome,baseTax,rate,threshold
static List<TaxBracket> ReadTaxTable(string path)
{
    var brackets = new List<TaxBracket>();

    var lines = File.ReadAllLines(path);
    if (lines.Length <= 1)
        throw new InvalidDataException("tax_table.csv is empty or missing data rows.");

    for (int i = 1; i < lines.Length; i++) // skip header
    {
        string line = lines[i].Trim();
        if (string.IsNullOrWhiteSpace(line))
            continue; // ignore blank lines

        var parts = line.Split(',');
        if (parts.Length < 5)
            throw new InvalidDataException($"CSV row {i + 1} does not have 5 columns.");

        try // parse each column into a TaxBracket
        {
            brackets.Add(new TaxBracket
            {
                MinIncome = decimal.Parse(parts[0].Trim(), CultureInfo.InvariantCulture),
                MaxIncome = decimal.Parse(parts[1].Trim(), CultureInfo.InvariantCulture),
                BaseTax   = decimal.Parse(parts[2].Trim(), CultureInfo.InvariantCulture),
                Rate      = decimal.Parse(parts[3].Trim(), CultureInfo.InvariantCulture),
                Threshold = decimal.Parse(parts[4].Trim(), CultureInfo.InvariantCulture)
            });
        }
        catch (FormatException)
        {
            throw new InvalidDataException($"CSV row {i + 1} has an invalid number format.");
        }
    }
    // sorts by MinIncome so the calculator can assume correct order
    brackets.Sort((a, b) => a.MinIncome.CompareTo(b.MinIncome));

    return brackets;
}

public static class TaxCalculator
{
    // Calculates tax for a given income using the bracket that contains the income.
    // Formula per bracket: tax = baseTax + (income - threshold) * rate
    public static decimal Calculate(decimal income, List<TaxBracket> brackets)
    {
        foreach (var b in brackets)
        {
            if (income >= b.MinIncome && income <= b.MaxIncome)
            {
                decimal tax = b.BaseTax + (income - b.Threshold) * b.Rate;

                // Safety: tax should never be negative
                return tax < 0 ? 0 : tax;
            }
        }
        throw new InvalidOperationException("Income did not match any tax bracket. Check the CSV ranges.");
    }
}