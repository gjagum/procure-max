import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { useQuery } from "@tanstack/react-query";
import { Plus, Package } from "lucide-react";
import { PageHeader } from "@/components/ui/page-header";
import { Button } from "@/components/ui/button";
import { DataTable, type Column } from "@/components/ui/data-table";
import { Pagination } from "@/components/ui/pagination";
import { FilterBar } from "@/components/ui/filter-bar";
import { Badge } from "@/components/ui/badge";
import { fetchItems } from "@/features/items/api";
import type { ItemSummary } from "@/lib/types";

export const Route = createFileRoute("/_app/items")({
  validateSearch: (search: Record<string, unknown>) => ({
    page: Number(search.page ?? 1),
    pageSize: Number(search.pageSize ?? 20),
    search: String(search.search ?? ""),
  }),
  component: ItemsPage,
});

function formatPrice(minor: number, currency: string) {
  return new Intl.NumberFormat("en-US", {
    style: "currency",
    currency,
  }).format(minor / 100);
}

function ItemsPage() {
  const navigate = useNavigate({ from: Route.fullPath });
  const { page, pageSize, search } = Route.useSearch();

  const { data, isLoading } = useQuery({
    queryKey: ["items", { page, pageSize, search }],
    queryFn: () => fetchItems({ page, pageSize, search }),
    placeholderData: (prev) => prev,
  });

  const columns: Column<ItemSummary>[] = [
    {
      key: "sku",
      header: "SKU",
      render: (i) => (
        <span className="font-mono text-xs text-on-surface-variant">
          {i.sku ?? "—"}
        </span>
      ),
    },
    {
      key: "name",
      header: "Name",
      render: (i) => (
        <div className="flex items-center gap-3">
          <div className="flex h-8 w-8 items-center justify-center rounded bg-primary/10">
            <Package className="h-4 w-4 text-primary" />
          </div>
          <span className="font-medium">{i.name}</span>
        </div>
      ),
    },
    {
      key: "category",
      header: "Category",
      render: (i) => (
        <span className="text-on-surface-variant">{i.category ?? "—"}</span>
      ),
    },
    {
      key: "price",
      header: "Default Price",
      render: (i) => (
        <span className="font-medium">
          {formatPrice(i.defaultPriceMinor, i.defaultCurrency)}
        </span>
      ),
    },
    {
      key: "status",
      header: "Status",
      render: (i) =>
        i.isActive ? (
          <Badge variant="success">Active</Badge>
        ) : (
          <Badge variant="neutral">Inactive</Badge>
        ),
    },
  ];

  return (
    <>
      <PageHeader
        title="Items & Services"
        subtitle="Manage the catalog of purchasable items and services."
        actions={
          <Button>
            <Plus className="h-4 w-4" />
            New Item
          </Button>
        }
      />

      <FilterBar
        searchPlaceholder="Search by SKU or name..."
        searchValue={search}
        onSearchChange={(value) =>
          navigate({ search: { page: 1, pageSize, search: value } })
        }
        filters={[
          {
            label: "Category",
            options: [
              { label: "All Categories", value: "" },
              { label: "IT & Telecom", value: "it" },
              { label: "Office Supplies", value: "office" },
              { label: "Raw Materials", value: "materials" },
            ],
          },
        ]}
      />

      <DataTable
        columns={columns}
        data={data?.items ?? []}
        emptyMessage={isLoading ? "Loading..." : "No items found"}
      />

      {data && (
        <Pagination
          page={data.page}
          totalPages={data.totalPages}
          total={data.total}
          pageSize={data.pageSize}
          onPageChange={(p) => navigate({ search: { page: p, pageSize, search } })}
        />
      )}
    </>
  );
}
