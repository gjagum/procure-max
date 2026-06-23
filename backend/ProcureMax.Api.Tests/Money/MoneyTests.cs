using FluentAssertions;
using ProcureMax.Core.Common;
using Xunit;

// 'Money' resolves to the type ProcureMax.Core.Common.Money from the using above;
// this file's namespace ends in .Money but does not shadow the type lookup.
namespace ProcureMax.Tests.MoneyDomain;

// Money is the most important value object in the procurement domain — every
// invoice, PO and requisition total depends on it. Cent-based arithmetic + strict
// currency checks must hold under all callers, so these rules are pinned first.
public class MoneyTests
{
    [Fact]
    public void FromDecimal_rounds_to_minor_units()
    {
        var m = Money.FromDecimal(12.345m);

        m.MinorUnits.Should().Be(1235); // round of 12.345 -> 12.35
        m.AsDecimal().Should().Be(12.35m);
        m.Currency.Should().Be("USD");
    }

    [Fact]
    public void ToString_formats_with_currency()
    {
        var m = Money.FromDecimal(1500.5m, "EUR");

        m.ToString().Should().Be("1500.50 EUR");
    }

    [Theory]
    [InlineData("USD", "EUR")]
    [InlineData("USD", "usd")] // currency codes are case-sensitive by design
    public void Addition_of_mismatched_currencies_throws(string a, string b)
    {
        var lhs = Money.FromDecimal(10m, a);
        var rhs = Money.FromDecimal(10m, b);

        var act = () => (lhs + rhs);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*mismatched currencies*");
    }

    [Fact]
    public void Subtraction_of_same_currency_preserves_currency()
    {
        var lhs = Money.FromDecimal(30m, "GBP");
        var rhs = Money.FromDecimal(12m, "GBP");

        var result = lhs - rhs;

        result.AsDecimal().Should().Be(18m);
        result.Currency.Should().Be("GBP");
    }

    [Theory]
    [InlineData(100, 50, true, false)]
    [InlineData(50, 100, false, true)]
    [InlineData(100, 100, false, true)] // strict greater than fails on equal
    public void Comparison_operators_compare_minor_units(long aCents, long bCents,
        bool aGtB, bool aLtB)
    {
        var a = new Money(aCents, "USD");
        var b = new Money(bCents, "USD");

        (a > b).Should().Be(aGtB);
        (a < b).Should().Be(aLtB);
        (a >= b).Should().Be(aGtB || a.MinorUnits == b.MinorUnits);
        (a <= b).Should().Be(aLtB || a.MinorUnits == b.MinorUnits);
    }

    [Fact]
    public void Scalar_multiplication_preserves_currency_and_does_not_drift()
    {
        var unit = Money.FromDecimal(19.99m, "USD");

        var lineTotal = unit * 3;

        lineTotal.AsDecimal().Should().Be(59.97m);
        lineTotal.Currency.Should().Be("USD");
    }

    [Theory]
    [InlineData(-1)]
    public void Scalar_multiplication_by_negative_throws(int multiplier)
    {
        var m = Money.FromDecimal(10m);

        var act = () => m * multiplier;

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*negative*");
    }

    [Fact]
    public void Multiplication_is_commutative_with_scalar()
    {
        var m = Money.FromDecimal(1.25m, "JPY");

        (m * 4).Should().Be(4 * m);
    }

    [Fact]
    public void Zero_has_explicit_currency()
    {
        Money.Zero("CAD").Should().Be(new Money(0, "CAD"));
    }
}
