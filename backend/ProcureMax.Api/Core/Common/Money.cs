namespace ProcureMax.Core.Common;

// Money is stored as a 64-bit integer of minor units (cents) to avoid float drift.
// Format on display via Money.Format(). Conversions handled at boundaries.
public readonly record struct Money(long MinorUnits, string Currency = "USD")
{
    public decimal AsDecimal() => MinorUnits / 100m;

    public override string ToString() => $"{AsDecimal():F2} {Currency}";

    public static Money FromDecimal(decimal value, string currency = "USD")
        => new((long)Math.Round(value * 100m), currency);

    public static Money operator +(Money a, Money b)
    {
        if (a.Currency != b.Currency)
            throw new InvalidOperationException($"Cannot add mismatched currencies {a.Currency}/{b.Currency}");
        return new Money(a.MinorUnits + b.MinorUnits, a.Currency);
    }

    public static Money operator -(Money a, Money b)
    {
        if (a.Currency != b.Currency)
            throw new InvalidOperationException($"Cannot subtract mismatched currencies {a.Currency}/{b.Currency}");
        return new Money(a.MinorUnits - b.MinorUnits, a.Currency);
    }

    public static bool operator >(Money a, Money b) => a.MinorUnits > b.MinorUnits;
    public static bool operator <(Money a, Money b) => a.MinorUnits < b.MinorUnits;
    public static bool operator >=(Money a, Money b) => a.MinorUnits >= b.MinorUnits;
    public static bool operator <=(Money a, Money b) => a.MinorUnits <= b.MinorUnits;

    // Scalars multiply the minor-unit count directly — no precision drift.
    // Negative multiplier throws because procurement amounts are non-negative by definition.
    public static Money operator *(Money a, int multiplier)
    {
        if (multiplier < 0)
            throw new InvalidOperationException("Money multiplier cannot be negative.");
        return new Money(a.MinorUnits * multiplier, a.Currency);
    }
    public static Money operator *(int multiplier, Money a) => a * multiplier;

    public static Money Zero(string currency = "USD") => new(0, currency);
}
