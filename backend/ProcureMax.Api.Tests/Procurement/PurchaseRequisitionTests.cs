using FluentAssertions;
using ProcureMax.Core.Common;
using ProcureMax.Features.Procurement;
using Xunit;

namespace ProcureMax.Tests.Procurement;

// PurchaseRequisition enforces the in-memory domain workflow rules that keep a PR
// in a valid state: line invariants, status transitions, currency consistency
// and self-approval guard. No database is touched — these tests are pure and fast.
public class PurchaseRequisitionTests
{
    // ---- Construction ----

    [Fact]
    public void Constructor_assigns_identity_and_defaults()
    {
        var pr = new PurchaseRequisition("user:1");

        pr.Id.Should().NotBeNullOrWhiteSpace();
        pr.RequestorId.Should().Be("user:1");
        pr.Status.Should().Be(RequisitionStatus.Draft);
        pr.Currency.Should().Be("USD");
        pr.Lines.Should().BeEmpty();
        pr.Total.AsDecimal().Should().Be(0m);
        pr.SubmittedAt.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_requires_requestor(string? requestorId)
    {
        var act = () => new PurchaseRequisition(requestorId!);

        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(requestorId));
    }

    [Fact]
    public void Constructor_honours_cost_center_and_currency()
    {
        var pr = new PurchaseRequisition("user:1", "cc:dev", "EUR", "Branch laptops");

        pr.CostCenterId.Should().Be("cc:dev");
        pr.Currency.Should().Be("EUR");
        pr.Title.Should().Be("Branch laptops");
    }

    // ---- Lines ----

    [Fact]
    public void AddLine_appends_line_and_recomputes_total()
    {
        var pr = new PurchaseRequisition("user:1");

        pr.AddLine("item:laptop", 3, Money.FromDecimal(1200m));

        pr.Lines.Should().HaveCount(1);
        pr.Total.AsDecimal().Should().Be(3600m);
    }

    [Fact]
    public void AddLine_sums_multiple_lines()
    {
        var pr = new PurchaseRequisition("user:1", currency: "USD");

        pr.AddLine("item:a", 2, Money.FromDecimal(50m, "USD"));
        pr.AddLine("item:b", 1, Money.FromDecimal(10.50m, "USD"));

        pr.Total.AsDecimal().Should().Be(110.50m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void AddLine_rejects_non_positive_quantity(int qty)
    {
        var pr = new PurchaseRequisition("user:1");

        var act = () => pr.AddLine("item:a", qty, Money.FromDecimal(10m));

        act.Should().Throw<Exception>();
    }

    [Fact]
    public void AddLine_rejects_foreign_currency()
    {
        var pr = new PurchaseRequisition("user:1", currency: "USD");

        var act = () => pr.AddLine("item:a", 1, Money.FromDecimal(10m, "EUR"));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*does not match*");
    }

    [Fact]
    public void RemoveLine_removes_by_id_and_recomputes()
    {
        var pr = new PurchaseRequisition("user:1");
        var keep = pr.AddLine("item:a", 1, Money.FromDecimal(10m));
        var drop = pr.AddLine("item:b", 1, Money.FromDecimal(20m));

        pr.RemoveLine(drop.Id);

        pr.Lines.Should().ContainSingle().Which.Id.Should().Be(keep.Id);
        pr.Total.AsDecimal().Should().Be(10m);
    }

    [Fact]
    public void RemoveLine_unknown_id_throws()
    {
        var pr = new PurchaseRequisition("user:1");

        var act = () => pr.RemoveLine("nope");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Line*not found*");
    }

    // ---- Submit ----

    [Fact]
    public void Submit_moves_to_pending_approval_and_stamps_time()
    {
        var pr = NewWithOneLine();

        var before = DateTime.UtcNow;
        pr.Submit();
        var after = DateTime.UtcNow;

        pr.Status.Should().Be(RequisitionStatus.PendingApproval);
        pr.SubmittedAt.Should().NotBeNull();
        pr.SubmittedAt!.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void Submit_with_no_lines_throws()
    {
        var pr = new PurchaseRequisition("user:1");

        var act = () => pr.Submit();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*no lines*");
    }

    [Fact]
    public void Cannot_mutate_lines_after_submission()
    {
        var pr = NewWithOneLine();
        pr.Submit();

        var add = () => pr.AddLine("item:b", 1, Money.FromDecimal(1m));
        var remove = () => pr.RemoveLine(pr.Lines[0].Id);

        add.Should().Throw<InvalidOperationException>().WithMessage("*not mutable*");
        remove.Should().Throw<InvalidOperationException>().WithMessage("*not mutable*");
    }

    // ---- Approve / Reject ----

    [Fact]
    public void Approve_transitions_to_approved_and_records_actor()
    {
        var pr = SubmittedPr();

        pr.Approve("user:approver1", "Within budget");

        pr.Status.Should().Be(RequisitionStatus.Approved);
        pr.DecidedBy.Should().Be("user:approver1");
        pr.DecisionReason.Should().Be("Within budget");
        pr.DecidedAt.Should().NotBeNull();
    }

    [Fact]
    public void Reject_transitions_to_rejected()
    {
        var pr = SubmittedPr();

        pr.Reject("user:approver1", "Duplicate request");

        pr.Status.Should().Be(RequisitionStatus.Rejected);
        pr.DecisionReason.Should().Be("Duplicate request");
    }

    [Fact]
    public void Cannot_approve_when_not_pending()
    {
        var pr = NewWithOneLine(); // DRAFT

        var act = () => pr.Approve("user:approver1");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot approve*");
    }

    [Fact]
    public void Cannot_approve_own_requisition()
    {
        var pr = SubmittedPr(requestorId: "user:same");

        var act = () => pr.Approve("user:same");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*self-approve*");
    }

    [Fact]
    public void Cannot_reject_own_requisition()
    {
        var pr = SubmittedPr(requestorId: "user:same");

        var act = () => pr.Reject("user:same");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*self-reject*");
    }

    // ---- Cancel / Reopen ----

    [Fact]
    public void Cancel_from_draft_transitions_to_cancelled()
    {
        var pr = NewWithOneLine();

        pr.Cancel("user:1");

        pr.Status.Should().Be(RequisitionStatus.Cancelled);
        pr.DecidedBy.Should().Be("user:1");
    }

    [Fact]
    public void Cancel_from_pending_approval_succeeds()
    {
        var pr = SubmittedPr();

        var act = () => pr.Cancel("user:1");

        act.Should().NotThrow();
        pr.Status.Should().Be(RequisitionStatus.Cancelled);
    }

    [Fact]
    public void Cancel_on_already_closed_throws()
    {
        var pr = new PurchaseRequisition("user:1");
        pr.Cancel("user:1");

        var act = () => pr.Cancel("user:1");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot cancel*");
    }

    [Fact]
    public void Reopen_only_works_from_rejected()
    {
        var pr = SubmittedPr();
        pr.Reject("user:approver");

        pr.Reopen();

        pr.Status.Should().Be(RequisitionStatus.Draft);
        pr.SubmittedAt.Should().BeNull();
        pr.DecidedAt.Should().BeNull();
        pr.DecidedBy.Should().BeNull();
    }

    [Fact]
    public void Reopen_from_approved_is_invalid()
    {
        var pr = SubmittedPr();
        pr.Approve("user:approver");

        var act = () => pr.Reopen();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Only rejected*");
    }

    // ---- Helpers ----

    private static PurchaseRequisition NewWithOneLine()
    {
        var pr = new PurchaseRequisition("user:1");
        pr.AddLine("item:demo", 1, Money.FromDecimal(10m));
        return pr;
    }

    private static PurchaseRequisition SubmittedPr(string requestorId = "user:1")
    {
        var pr = new PurchaseRequisition(requestorId);
        pr.AddLine("item:demo", 1, Money.FromDecimal(10m));
        pr.Submit();
        return pr;
    }
}
