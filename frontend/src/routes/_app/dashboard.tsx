import { createFileRoute, Link } from "@tanstack/react-router";
import {
  FileText,
  ShoppingCart,
  Building2,
  Package,
} from "lucide-react";
import { PageHeader } from "@/components/ui/page-header";
import { Badge } from "@/components/ui/badge";

export const Route = createFileRoute("/_app/dashboard")({
  component: DashboardPage,
});

const metrics = [
  {
    label: "Pending PRs",
    value: "12",
    icon: FileText,
    iconBg: "bg-orange-50 text-orange-600",
  },
  {
    label: "Active POs",
    value: "8",
    icon: ShoppingCart,
    iconBg: "bg-primary-container text-primary",
  },
  {
    label: "Suppliers",
    value: "45",
    icon: Building2,
    iconBg: "bg-emerald-50 text-emerald-600",
  },
  {
    label: "Items",
    value: "230",
    icon: Package,
    iconBg: "bg-purple-50 text-purple-600",
  },
];

const recentActivity = [
  {
    item: "Laptop Pro 14",
    pr: "PR-2024-001",
    status: "Pending",
    variant: "warning" as const,
    amount: "$1,200.00",
    date: "2026-05-20",
  },
  {
    item: "Office Printer X",
    pr: "PR-2024-002",
    status: "Approved",
    variant: "success" as const,
    amount: "$850.00",
    date: "2026-05-19",
  },
  {
    item: "Ergonomic Chair",
    pr: "PR-2024-003",
    status: "Processing",
    variant: "info" as const,
    amount: "$350.00",
    date: "2026-05-18",
  },
  {
    item: "Server Rack Units",
    pr: "PR-2024-004",
    status: "Pending",
    variant: "warning" as const,
    amount: "$4,500.00",
    date: "2026-05-17",
  },
];

function DashboardPage() {
  return (
    <>
      <PageHeader
        title="Dashboard Overview"
        subtitle="Key procurement metrics for the current period."
      />

      {/* Metrics Grid */}
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
        {metrics.map((m) => {
          const Icon = m.icon;
          return (
            <div
              key={m.label}
              className="flex items-start gap-4 rounded-xl border border-outline-variant bg-surface p-5 shadow-sm"
            >
              <div
                className={`flex h-10 w-10 shrink-0 items-center justify-center rounded-lg ${m.iconBg}`}
              >
                <Icon className="h-5 w-5" />
              </div>
              <div>
                <p className="text-sm font-medium text-on-surface-variant">
                  {m.label}
                </p>
                <p className="mt-1 text-2xl font-bold text-on-surface">
                  {m.value}
                </p>
              </div>
            </div>
          );
        })}
      </div>

      {/* Recent Activity */}
      <div className="mt-8 overflow-hidden rounded-xl border border-outline-variant bg-surface shadow-sm">
        <div className="flex items-center justify-between border-b border-outline px-6 py-5">
          <h3 className="text-lg font-semibold text-on-surface">
            Recent Activity
          </h3>
          <Link
            to="/purchase-requisitions"
            className="text-sm font-medium text-primary hover:underline"
          >
            View All
          </Link>
        </div>
        <div className="overflow-x-auto">
          <table className="w-full whitespace-nowrap text-left text-sm">
            <thead className="bg-surface-container-low font-medium text-on-surface-variant">
              <tr>
                <th className="px-6 py-3">Item</th>
                <th className="px-6 py-3">Requisition #</th>
                <th className="px-6 py-3">Status</th>
                <th className="px-6 py-3">Amount</th>
                <th className="px-6 py-3">Date</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-outline">
              {recentActivity.map((row) => (
                <tr
                  key={row.pr}
                  className="transition-colors hover:bg-surface-container-low"
                >
                  <td className="px-6 py-4 font-medium text-on-surface">
                    {row.item}
                  </td>
                  <td className="px-6 py-4 text-on-surface-variant">{row.pr}</td>
                  <td className="px-6 py-4">
                    <Badge variant={row.variant} dot>
                      {row.status}
                    </Badge>
                  </td>
                  <td className="px-6 py-4 font-medium text-on-surface">
                    {row.amount}
                  </td>
                  <td className="px-6 py-4 text-on-surface-variant">
                    {row.date}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </>
  );
}
