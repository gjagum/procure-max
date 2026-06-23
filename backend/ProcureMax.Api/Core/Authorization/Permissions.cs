namespace ProcureMax.Core.Authorization;

// Central permission catalog. Seed all entries at startup.
// Convention: '<area>.<action>' read top-down as 'can <action> <area>'.
// Slices apply [HasPermission("...")] from this list.
public static class Permissions
{
    public const string UsersManage      = "users.manage";
    public const string RolesManage      = "roles.manage";
    public const string DashboardView    = "dashboard.view";

    public const string SupplierManage   = "supplier.manage";
    public const string SupplierView     = "supplier.view";

    public const string ItemManage       = "item.manage";
    public const string ItemView         = "item.view";

    public const string CostCenterManage = "costcenter.manage";
    public const string GlAccountManage  = "glaccount.manage";
    public const string UnitManage       = "unit.manage";

    public const string PrCreate         = "pr.create";
    public const string PrView           = "pr.view";
    public const string PrApproveOwn     = "pr.approve.own_cost_center";
    public const string PrApproveAll     = "pr.approve.all";

    public const string PoCreate         = "po.create";
    public const string PoView           = "po.view";
    public const string PoIssue          = "po.issue";
    public const string PoApprove        = "po.approve";

    public const string GrCreate         = "gr.create";
    public const string GrView           = "gr.view";

    public const string InvoiceManage    = "invoice.manage";
    public const string InvoiceView      = "invoice.view";
    public const string InvoiceApprove   = "invoice.approve";

    public const string ApprovalRulesManage = "approval-rules.manage";

    public static readonly (string area, string action)[] All =
    [
        ("users", "manage"),
        ("roles", "manage"),
        ("dashboard", "view"),
        ("supplier", "manage"),
        ("supplier", "view"),
        ("item", "manage"),
        ("item", "view"),
        ("costcenter", "manage"),
        ("glaccount", "manage"),
        ("unit", "manage"),
        ("pr", "create"),
        ("pr", "view"),
        ("pr", "approve.own_cost_center"),
        ("pr", "approve.all"),
        ("po", "create"),
        ("po", "view"),
        ("po", "issue"),
        ("po", "approve"),
        ("gr", "create"),
        ("gr", "view"),
        ("invoice", "manage"),
        ("invoice", "view"),
        ("invoice", "approve"),
        ("approval-rules", "manage"),
    ];

    public static string FullName(string area, string action) => $"{area}.{action}";
}

// Role templates the system seeds. Custom roles can be added via UI.
public static class SystemRoles
{
    public const string Admin    = "Admin";
    public const string Requestor = "Requestor";
    public const string Approver = "Approver";
    public const string Buyer    = "Buyer";
    public const string ApClerk  = "AP Clerk";
}
