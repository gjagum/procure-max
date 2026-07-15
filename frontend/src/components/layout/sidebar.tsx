import { Link, useRouterState } from "@tanstack/react-router";
import {
  LayoutDashboard,
  FileText,
  ShoppingCart,
  PackageCheck,
  Receipt,
  Building2,
  Package,
  Wallet,
  Landmark,
  Ruler,
  Users,
  ShieldCheck,
  Settings,
  LogOut,
  Plus,
} from "lucide-react";

const navSections = [
  {
    label: undefined,
    items: [
      { to: "/dashboard", label: "Dashboard", icon: LayoutDashboard },
    ],
  },
  {
    label: "Procurement",
    items: [
      { to: "/purchase-requisitions", label: "Purchase Requisitions", icon: FileText },
      { to: "/purchase-orders", label: "Purchase Orders", icon: ShoppingCart },
      { to: "/goods-receipt", label: "Goods Receipt", icon: PackageCheck },
      { to: "/invoices", label: "Invoices", icon: Receipt },
    ],
  },
  {
    label: "Master Data",
    items: [
      { to: "/suppliers", label: "Suppliers", icon: Building2 },
      { to: "/items", label: "Items", icon: Package },
    ],
  },
  {
    label: "Financial Setup",
    items: [
      { to: "/cost-centers", label: "Cost Centers", icon: Wallet },
      { to: "/gl-accounts", label: "GL Accounts", icon: Landmark },
      { to: "/units", label: "Units", icon: Ruler },
    ],
  },
  {
    label: "Administration",
    items: [
      { to: "/users", label: "Users", icon: Users },
      { to: "/roles", label: "Roles", icon: ShieldCheck },
    ],
  },
];

export function Sidebar() {
  const pathname = useRouterState({ select: (s) => s.location.pathname });

  return (
    <nav className="z-50 flex h-full w-64 flex-col overflow-y-auto border-r border-outline-variant bg-surface-container-low px-3 pb-8 pt-4">
      {/* Logo */}
      <div className="mb-8 flex items-center gap-3 px-4">
        <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-primary shadow-lg">
          <Package className="h-5 w-5 text-on-primary" />
        </div>
        <div>
          <h1 className="text-lg font-bold text-primary">ProcureMax</h1>
          <p className="text-xs text-on-surface-variant">Enterprise ERP</p>
        </div>
      </div>

      {/* New Requisition Button */}
      <Link
        to="/purchase-requisitions"
        className="mb-6 flex w-full items-center justify-center gap-2 rounded-lg bg-primary py-2 px-4 text-sm font-medium text-on-primary transition-opacity hover:opacity-90"
      >
        <Plus className="h-4 w-4" />
        New Requisition
      </Link>

      {/* Nav Sections */}
      <div className="flex flex-1 flex-col gap-1">
        {navSections.map((section, si) => (
          <div key={si}>
            {section.label && (
              <p className="mt-4 mb-1 px-4 text-[11px] font-bold uppercase tracking-wider text-on-surface-variant/60">
                {section.label}
              </p>
            )}
            {section.items.map((item) => {
              const Icon = item.icon;
              const active = pathname.startsWith(item.to);
              return (
                <Link
                  key={item.to}
                  to={item.to}
                  className={`flex items-center gap-3 rounded-lg px-4 py-2 text-sm transition-colors ${
                    active
                      ? "bg-primary-container font-medium text-on-primary-container"
                      : "text-on-surface-variant hover:bg-surface-container-high"
                  }`}
                >
                  <Icon className="h-4 w-4" />
                  {item.label}
                </Link>
              );
            })}
          </div>
        ))}
      </div>

      {/* Footer */}
      <div className="mt-auto flex flex-col gap-1 border-t border-outline pt-4">
        <Link
          to="/settings"
          className="flex items-center gap-3 rounded-lg px-4 py-2 text-sm text-on-surface-variant transition-colors hover:bg-surface-container-high"
        >
          <Settings className="h-4 w-4" />
          Settings
        </Link>
        <button className="flex items-center gap-3 rounded-lg px-4 py-2 text-sm text-on-surface-variant transition-colors hover:bg-surface-container-high">
          <LogOut className="h-4 w-4" />
          Logout
        </button>
      </div>
    </nav>
  );
}
