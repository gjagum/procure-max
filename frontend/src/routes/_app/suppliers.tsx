import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { useQuery } from "@tanstack/react-query";
import { Plus, Building2 } from "lucide-react";
import { PageHeader } from "@/components/ui/page-header";
import { Button } from "@/components/ui/button";
import { DataTable, type Column } from "@/components/ui/data-table";
import { Pagination } from "@/components/ui/pagination";
import { FilterBar } from "@/components/ui/filter-bar";
import { Badge } from "@/components/ui/badge";
import { fetchSuppliers } from "@/features/suppliers/api";
import type { SupplierSummary } from "@/lib/types";

export const Route = createFileRoute("/_app/suppliers")({
  validateSearch: (search: Record<string, unknown>) => ({
    page: Number(search.page ?? 1),
    pageSize: Number(search.pageSize ?? 20),
    search: String(search.search ?? ""),
  }),
  component: SuppliersPage,
});

function SuppliersPage() {
  const navigate = useNavigate({ from: Route.fullPath });
  const { page, pageSize, search } = Route.useSearch();

  const { data, isLoading } = useQuery({
    queryKey: ["suppliers", { page, pageSize, search }],
    queryFn: () => fetchSuppliers({ page, pageSize, search }),
    placeholderData: (prev) => prev,
  });

  const columns: Column<SupplierSummary>[] = [
    {
      key: "code",
      header: "Code",
      render: (s) => <span className="font-semibold text-primary">{s.code}</span>,
    },
    {
      key: "name",
      header: "Name",
      render: (s) => (
        <div className="flex items-center gap-3">
          <div className="flex h-8 w-8 items-center justify-center rounded bg-primary/10">
            <Building2 className="h-4 w-4 text-primary" />
          </div>
          <span className="font-medium">{s.name}</span>
        </div>
      ),
    },
    {
      key: "legalName",
      header: "Legal Name",
      render: (s) => (
        <span className="text-on-surface-variant">{s.legalName ?? "—"}</span>
      ),
    },
    {
      key: "status",
      header: "Status",
      render: (s) =>
        s.isBlocked ? (
          <Badge variant="error">Blocked</Badge>
        ) : s.isActive ? (
          <Badge variant="success">Active</Badge>
        ) : (
          <Badge variant="neutral">Inactive</Badge>
        ),
    },
    {
      key: "currency",
      header: "Currency",
      render: (s) => <span className="font-medium">{s.currency}</span>,
    },
  ];

  return (
    <>
      <PageHeader
        title="Suppliers"
        subtitle="Manage and monitor vendor relationships across your organization."
        actions={
          <Button>
            <Plus className="h-4 w-4" />
            New Supplier
          </Button>
        }
      />

      <FilterBar
        searchPlaceholder="By code, name or tax ID..."
        searchValue={search}
        onSearchChange={(value) =>
          navigate({ search: { page: 1, pageSize, search: value } })
        }
        filters={[
          {
            label: "Status",
            options: [
              { label: "All Status", value: "" },
              { label: "Active", value: "active" },
              { label: "Blocked", value: "blocked" },
            ],
          },
        ]}
      />

      <DataTable
        columns={columns}
        data={data?.items ?? []}
        emptyMessage={isLoading ? "Loading..." : "No suppliers found"}
      />

      {data && (
        <div className="mt-0 -mt-px">
          <Pagination
            page={data.page}
            totalPages={data.totalPages}
            total={data.total}
            pageSize={data.pageSize}
            onPageChange={(p) =>
              navigate({ search: { page: p, pageSize, search } })
            }
          />
        </div>
      )}
    </>
  );
}
