import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { useQuery } from "@tanstack/react-query";
import { Plus } from "lucide-react";
import { PageHeader } from "@/components/ui/page-header";
import { Button } from "@/components/ui/button";
import { DataTable, type Column } from "@/components/ui/data-table";
import { Pagination } from "@/components/ui/pagination";
import { FilterBar } from "@/components/ui/filter-bar";
import { Badge } from "@/components/ui/badge";
import { fetchUsers } from "@/features/users/api";
import type { UserSummary } from "@/lib/types";

export const Route = createFileRoute("/_app/users")({
  validateSearch: (search: Record<string, unknown>) => ({
    page: Number(search.page ?? 1),
    pageSize: Number(search.pageSize ?? 20),
    search: String(search.search ?? ""),
  }),
  component: UsersPage,
});

function UsersPage() {
  const navigate = useNavigate({ from: Route.fullPath });
  const { page, pageSize, search } = Route.useSearch();

  const { data, isLoading } = useQuery({
    queryKey: ["users", { page, pageSize, search }],
    queryFn: () => fetchUsers({ page, pageSize, search }),
    placeholderData: (prev) => prev,
  });

  const columns: Column<UserSummary>[] = [
    {
      key: "email",
      header: "Email",
      render: (u) => (
        <div className="flex items-center gap-3">
          <div className="flex h-8 w-8 items-center justify-center rounded-full bg-primary-container text-xs font-bold text-on-primary-container">
            {u.email.slice(0, 2).toUpperCase()}
          </div>
          <span className="font-medium">{u.email}</span>
        </div>
      ),
    },
    {
      key: "fullName",
      header: "Full Name",
      render: (u) => (
        <span className="text-on-surface-variant">{u.fullName}</span>
      ),
    },
    {
      key: "roles",
      header: "Roles",
      render: (u) => (
        <div className="flex flex-wrap gap-1">
          {u.roles.length === 0 ? (
            <span className="text-on-surface-variant">—</span>
          ) : (
            u.roles.map((role) => (
              <Badge key={role} variant="info">
                {role}
              </Badge>
            ))
          )}
        </div>
      ),
    },
    {
      key: "status",
      header: "Status",
      render: (u) =>
        u.isActive ? (
          <Badge variant="success">Active</Badge>
        ) : (
          <Badge variant="error">Inactive</Badge>
        ),
    },
    {
      key: "createdAt",
      header: "Created",
      render: (u) => (
        <span className="text-on-surface-variant">
          {new Date(u.createdAt).toLocaleDateString()}
        </span>
      ),
    },
  ];

  return (
    <>
      <PageHeader
        title="Users"
        subtitle="Manage user accounts and access."
        actions={
          <Button>
            <Plus className="h-4 w-4" />
            New User
          </Button>
        }
      />

      <FilterBar
        searchPlaceholder="Search by email or name..."
        searchValue={search}
        onSearchChange={(value) =>
          navigate({ search: { page: 1, pageSize, search: value } })
        }
      />

      <DataTable
        columns={columns}
        data={data?.items ?? []}
        emptyMessage={isLoading ? "Loading..." : "No users found"}
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
