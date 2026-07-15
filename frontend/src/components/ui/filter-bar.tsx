import type { ReactNode } from "react";
import { Search, Filter } from "lucide-react";

export interface FilterOption {
  label: string;
  options: string[];
}

interface FilterBarProps {
  searchPlaceholder?: string;
  searchValue?: string;
  onSearchChange?: (value: string) => void;
  filters?: {
    label: string;
    value?: string;
    options: { label: string; value: string }[];
    onChange?: (value: string) => void;
  }[];
  children?: ReactNode;
}

export function FilterBar({
  searchPlaceholder = "Search...",
  searchValue,
  onSearchChange,
  filters = [],
  children,
}: FilterBarProps) {
  return (
    <div className="mb-6 flex flex-wrap items-end gap-4 rounded-xl border border-outline-variant bg-surface p-4 shadow-sm">
      {onSearchChange && (
        <div className="min-w-[240px] flex-1">
          <label className="mb-1 ml-1 block text-[11px] font-bold uppercase text-on-surface-variant">
            Search
          </label>
          <div className="relative">
            <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-on-surface-variant" />
            <input
              className="w-full rounded-lg border border-outline-variant bg-surface-container-low py-2 pl-10 pr-4 text-sm transition-all focus:border-primary focus:ring-2 focus:ring-primary/20"
              placeholder={searchPlaceholder}
              value={searchValue ?? ""}
              onChange={(e) => onSearchChange(e.target.value)}
            />
          </div>
        </div>
      )}
      {filters.map((f) => (
        <div key={f.label} className="w-48">
          <label className="mb-1 ml-1 block text-[11px] font-bold uppercase text-on-surface-variant">
            {f.label}
          </label>
          <select
            className="w-full rounded-lg border border-outline-variant bg-surface-container-low py-2 text-sm transition-all focus:border-primary focus:ring-2 focus:ring-primary/20"
            value={f.value ?? ""}
            onChange={(e) => f.onChange?.(e.target.value)}
          >
            {f.options.map((opt) => (
              <option key={opt.value} value={opt.value}>
                {opt.label}
              </option>
            ))}
          </select>
        </div>
      ))}
      <button className="rounded-lg border border-outline-variant bg-surface-container-high p-2.5 text-on-surface-variant transition-colors hover:bg-surface-container-low">
        <Filter className="h-5 w-5" />
      </button>
      {children}
    </div>
  );
}
