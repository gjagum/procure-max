import { ChevronLeft, ChevronRight } from "lucide-react";

interface PaginationProps {
  page: number;
  totalPages: number;
  total: number;
  pageSize: number;
  onPageChange: (page: number) => void;
}

export function Pagination({
  page,
  totalPages,
  total,
  pageSize,
  onPageChange,
}: PaginationProps) {
  const start = total === 0 ? 0 : (page - 1) * pageSize + 1;
  const end = Math.min(page * pageSize, total);

  const pages: (number | "...")[] = [];
  for (let i = 1; i <= totalPages; i++) {
    if (
      i === 1 ||
      i === totalPages ||
      (i >= page - 1 && i <= page + 1)
    ) {
      pages.push(i);
    } else if (pages[pages.length - 1] !== "...") {
      pages.push("...");
    }
  }

  return (
    <div className="flex items-center justify-between border-t border-outline-variant bg-surface-container-low px-6 py-4">
      <p className="text-sm text-on-surface-variant">
        Showing <span className="font-semibold text-on-surface">{start}</span>{" "}
        to <span className="font-semibold text-on-surface">{end}</span> of{" "}
        <span className="font-semibold text-on-surface">{total}</span> items
      </p>
      <div className="flex items-center gap-1">
        <button
          className="rounded p-1.5 text-on-surface-variant transition-colors hover:bg-surface-container-high disabled:opacity-50"
          onClick={() => onPageChange(page - 1)}
          disabled={page <= 1}
        >
          <ChevronLeft className="h-5 w-5" />
        </button>
        {pages.map((p, i) =>
          p === "..." ? (
            <span key={`e${i}`} className="px-2 text-on-surface-variant">
              ...
            </span>
          ) : (
            <button
              key={p}
              className={`rounded-lg px-3.5 py-1.5 text-sm font-semibold transition-colors ${
                p === page
                  ? "bg-primary text-white"
                  : "text-on-surface-variant hover:bg-surface-container-high"
              }`}
              onClick={() => onPageChange(p)}
            >
              {p}
            </button>
          ),
        )}
        <button
          className="rounded p-1.5 text-on-surface-variant transition-colors hover:bg-surface-container-high disabled:opacity-50"
          onClick={() => onPageChange(page + 1)}
          disabled={page >= totalPages}
        >
          <ChevronRight className="h-5 w-5" />
        </button>
      </div>
    </div>
  );
}
