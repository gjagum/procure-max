namespace ProcureMax.Core;

// Procurement domain-wide options. Bound from "Procurement" configuration section.
public class ProcurementOptions
{
    public int MatchPriceTolerancePercent { get; set; } = 2;
    public int MatchQtyTolerancePercent { get; set; } = 5;
}
