import { useState } from "react";
import { createFileRoute } from "@tanstack/react-router";
import { Plus } from "lucide-react";
import { PageHeader } from "@/components/ui/page-header";
import { Button } from "@/components/ui/button";
import { DataTable, type Column } from "@/components/ui/data-table";
import { FilterBar } from "@/components/ui/filter-bar";
import { Badge } from "@/components/ui/badge";
import type { RequisitionSummary } from "@/lib/types";

export const Route = createFileRoute("/_app/purchase-requisitions")({
  component: RequisitionsPage,
});

const mockData: RequisitionSummary[] = [
  {
    id: "1",
    number: "PR-2026-001",
    requestor: "Jane Doe",
    costCenter: "CC-1000",
    totalMinor: 120000,
    currency: "USD",
    status: "PENDING_APPROVAL",
    createdAt: "2026-05-20",
  },
  {
    id: "2",
    number: "PR-2026-002",
    requestor: "John Smith",
    costCenter: "CC-2000",
    totalMinor: 85000,
    currency: "USD",
    status: "APPROVED",
    createdAt: "2026-05-19",
  },
  {
    id: "3",
    number: "PR-2026-003",
    requestor: "Alice Brown",
    costCenter: "CC-1000",
    totalMinor: 35000,
    currency: "USD",
    status: "DRAFT",
    createdAt: "2026-05-18",
  },
  {
    id: "4",
    number: "PR-2026-004",
    requestor: "Bob Wilson",
    costCenter: "CC-3000",
    totalMinor: 450000,
    currency: "USD",
    status: "REJECTED",
    createdAt: "2026-05-17",
  },
];

const statusConfig: Record<
  RequisitionSummary["status"],
  { label: string; variant: "warning" | "success" | "neutral" | "error" }
> = {
  DRAFT: { label: "Draft", variant: "neutral" },
  PENDING_APPROVAL: { label: "Pending", variant: "warning" },
  APPROVED: { label: "Approved", variant: "success" },
  REJECTED: { label: "Rejected", variant: "error" },
};

function RequisitionsPage() {
  const [search, setSearch] = useState("");
  const [statusFilter, setStatusFilter] = useState("");

  const filtered = mockData.filter((r) => {
    const matchesSearch =
      r.number.toLowerCase().includes(search.toLowerCase()) ||
      r.requestor.toLowerCase().includes(search.toLowerCase());
    const matchesStatus = !statusFilter || r.status === statusFilter;
    return matchesSearch && matchesStatus;
  });

  const columns: Column<RequisitionSummary>[] = [
    {
      key: "number",
      header: "PR #",
      render: (r) => (
        <span className="font-semibold text-primary">{r.number}</span>
      ),
    },
    {
      key: "requestor",
      header: "Requestor",
      render: (r) => <span className="font-medium">{r.requestor}</span>,
    },
    {
      key: "costCenter",
      header: "Cost Center",
      render: (r) => (
        <span className="text-on-surface-variant">{r.costCenter ?? "—"}</span>
      ),
    },
    {
      key: "total",
      header: "Total",
      render: (r) => (
        <span className="font-medium">
          {new Intl.NumberFormat("en-US", {
            style: "currency",
            currency: r.currency,
          }).format(r.totalMinor / 100)}
        </span>
      ),
    },
    {
      key: "status",
      header: "Status",
      render: (r) => {
        const cfg = statusConfig[r.status];
        return (
          <Badge variant={cfg.variant} dot>
            {cfg.label}
          </Badge>
        );
      },
    },
    {
      key: "createdAt",
      header: "Created",
      render: (r) => (
        <span className="text-on-surface-variant">
          {new Date(r.createdAt).toLocaleDateString()}
        </span>
      ),
    },
  ];

  return (
    <>
      <PageHeader
        title="Purchase Requisitions"
        subtitle="Create and track purchase requests through the approval workflow."
        actions={
          <Button>
            <Plus className="h-4 w-4" />
            New PR
          </Button>
        }
      />

      <FilterBar
        searchPlaceholder="Search by PR number or requestor..."
        searchValue={search}
        onSearchChange={setSearch}
        filters={[
          {
            label: "Status",
            value: statusFilter,
            options: [
              { label: "All Status", value: "" },
              { label: "Draft", value: "DRAFT" },
              { label: "Pending Approval", value: "PENDING_APPROVAL" },
              { label: "Approved", value: "APPROVED" },
              { label: "Rejected", value: "REJECTED" },
            ],
            onChange: setStatusFilter,
          },
        ]}
      />

      <DataTable
        columns={columns}
        data={filtered}
        emptyMessage="No requisitions found"
      />
    </>
  );
}
