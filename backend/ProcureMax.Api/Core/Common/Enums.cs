namespace ProcureMax.Core.Common;

// Status enums for procurement documents. Stored as TEXT in SQLite.
public static class RequisitionStatus
{
    public const string Draft = "DRAFT";
    public const string PendingApproval = "PENDING_APPROVAL";
    public const string Approved = "APPROVED";
    public const string Rejected = "REJECTED";
    public const string PartiallySourced = "PARTIALLY_SOURCED";
    public const string Closed = "CLOSED";
    public const string Cancelled = "CANCELLED";
}

public static class PurchaseOrderStatus
{
    public const string Draft = "DRAFT";
    public const string PendingApproval = "PENDING_APPROVAL";
    public const string Issued = "ISSUED";
    public const string PartiallyReceived = "PARTIALLY_RECEIVED";
    public const string Received = "RECEIVED";
    public const string Closed = "CLOSED";
    public const string Cancelled = "CANCELLED";
}

public static class GoodsReceiptStatus
{
    public const string Draft = "DRAFT";
    public const string Posted = "POSTED";
    public const string Cancelled = "CANCELLED";
}

public static class InvoiceStatus
{
    public const string New = "NEW";
    public const string PendingMatch = "PENDING_MATCH";
    public const string Matched = "MATCHED";
    public const string Exception = "EXCEPTION";
    public const string Approved = "APPROVED";
    public const string Paid = "PAID";
    public const string Void = "VOID";
    public const string Disputed = "DISPUTED";
}

public static class ApprovalStepStatus
{
    public const string Pending = "PENDING";
    public const string Approved = "APPROVED";
    public const string Rejected = "REJECTED";
    public const string Skipped = "SKIPPED";
}
