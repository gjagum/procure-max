namespace ProcureMax.Features.Procurement;

// In-memory domain aggregate for a Purchase Requisition (PR).
//
// This is the central procurement document a Requestor raises to ask the buying
// team to source goods/services. Lines are mutable while Status == Draft; once
// submitted the aggregate transition rules enforce workflow integrity.
//
// Persistence (Dapper/SQLite) will be added in a later slice; for now this model
// exists so domain rules can be unit-tested in isolation without a database.
public class PurchaseRequisition
{
    public string Id { get; private set; } = Guid.NewGuid().ToString("N");
    public string RequestorId { get; private set; }
    public string? CostCenterId { get; private set; }
    public string Status { get; private set; } = RequisitionStatus.Draft;
    public string Currency { get; private set; } = "USD";
    public string? Title { get; private set; }
    public string? Justification { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? SubmittedAt { get; private set; }
    public DateTime? DecidedAt { get; private set; }
    public string? DecidedBy { get; private set; }
    public string? DecisionReason { get; private set; }

    private readonly List<RequisitionLine> _lines = new();
    public IReadOnlyList<RequisitionLine> Lines => _lines.AsReadOnly();

    public Money Total => _lines.Count == 0
        ? Money.Zero(Currency)
        : _lines.Select(l => l.LineTotal).Aggregate((a, b) => a + b);

    public PurchaseRequisition(string requestorId, string? costCenterId = null, string currency = "USD", string? title = null)
    {
        if (string.IsNullOrWhiteSpace(requestorId))
            throw new ArgumentException("RequestorId is required.", nameof(requestorId));
        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency is required.", nameof(currency));

        RequestorId = requestorId;
        CostCenterId = costCenterId;
        Currency = currency;
        Title = title;
    }

    // --- Line management ---

    public RequisitionLine AddLine(string itemId, int quantity, Money unitPrice, string uom = "EA")
    {
        EnsureMutable();
        if (quantity <= 0)
            throw new InvalidOperationException("Quantity must be greater than zero.");
        EnsureSameCurrency(unitPrice);
        var line = new RequisitionLine(itemId, quantity, unitPrice, uom);
        _lines.Add(line);
        return line;
    }

    public void RemoveLine(string lineId)
    {
        EnsureMutable();
        if (_lines.RemoveAll(l => l.Id == lineId) == 0)
            throw new InvalidOperationException($"Line '{lineId}' not found on requisition.");
    }

    // --- Workflow transitions ---

    public void Submit()
    {
        EnsureMutable();
        if (_lines.Count == 0)
            throw new InvalidOperationException("Cannot submit a requisition with no lines.");
        Status = RequisitionStatus.PendingApproval;
        SubmittedAt = DateTime.UtcNow;
    }

    public void Approve(string approverId, string? reason = null)
    {
        if (Status != RequisitionStatus.PendingApproval)
            throw new InvalidOperationException($"Cannot approve a requisition in status '{Status}'.");
        if (string.IsNullOrWhiteSpace(approverId))
            throw new ArgumentException("ApproverId is required.", nameof(approverId));
        if (approverId == RequestorId)
            throw new InvalidOperationException("Cannot self-approve a requisition.");
        Status = RequisitionStatus.Approved;
        DecidedAt = DateTime.UtcNow;
        DecidedBy = approverId;
        DecisionReason = reason;
    }

    public void Reject(string approverId, string? reason = null)
    {
        if (Status != RequisitionStatus.PendingApproval)
            throw new InvalidOperationException($"Cannot reject a requisition in status '{Status}'.");
        if (string.IsNullOrWhiteSpace(approverId))
            throw new ArgumentException("ApproverId is required.", nameof(approverId));
        if (approverId == RequestorId)
            throw new InvalidOperationException("Cannot self-reject a requisition.");
        Status = RequisitionStatus.Rejected;
        DecidedAt = DateTime.UtcNow;
        DecidedBy = approverId;
        DecisionReason = reason;
    }

    public void Cancel(string actorId)
    {
        if (Status is RequisitionStatus.Closed or RequisitionStatus.Cancelled)
            throw new InvalidOperationException($"Cannot cancel a requisition in status '{Status}'.");
        Status = RequisitionStatus.Cancelled;
        DecidedAt = DateTime.UtcNow;
        DecidedBy = actorId;
    }

    public void Reopen()
    {
        if (Status != RequisitionStatus.Rejected)
            throw new InvalidOperationException("Only rejected requisitions can be reopened.");
        Status = RequisitionStatus.Draft;
        SubmittedAt = null;
        DecidedAt = null;
        DecidedBy = null;
        DecisionReason = null;
    }

    private void EnsureMutable()
    {
        if (Status != RequisitionStatus.Draft)
            throw new InvalidOperationException($"Requisition is not mutable in status '{Status}'.");
    }

    private void EnsureSameCurrency(Money amount)
    {
        if (amount.Currency != Currency)
            throw new InvalidOperationException(
                $"Line currency '{amount.Currency}' does not match requisition currency '{Currency}'.");
    }
}

// A single line item on a requisition. Immutable value type; replace instead of mutating.
public class RequisitionLine
{
    public string Id { get; } = Guid.NewGuid().ToString("N");
    public string ItemId { get; }
    public int Quantity { get; }
    public Money UnitPrice { get; }
    public string Uom { get; }
    public Money LineTotal => UnitPrice * Quantity;

    public RequisitionLine(string itemId, int quantity, Money unitPrice, string uom = "EA")
    {
        if (string.IsNullOrWhiteSpace(itemId))
            throw new ArgumentException("ItemId is required.", nameof(itemId));
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");
        if (string.IsNullOrWhiteSpace(uom))
            throw new ArgumentException("Uom is required.", nameof(uom));
        ItemId = itemId;
        Quantity = quantity;
        UnitPrice = unitPrice;
        Uom = uom;
    }
}
