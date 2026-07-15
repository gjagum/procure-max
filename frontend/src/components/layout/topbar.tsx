import { Search, Bell, HelpCircle } from "lucide-react";

export function TopBar() {
  return (
    <header className="sticky top-0 z-40 flex h-16 w-full items-center justify-between border-b border-outline-variant bg-surface px-6">
      <div className="flex items-center gap-4">
        <div className="relative hidden sm:block">
          <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-on-surface-variant" />
          <input
            className="w-64 rounded-lg border-none bg-surface-container-high py-1.5 pl-9 pr-4 text-sm transition-all focus:bg-surface focus:ring-1 focus:ring-primary"
            placeholder="Search POs, Requisitions..."
            type="text"
          />
        </div>
      </div>
      <div className="flex items-center gap-3">
        <button className="relative rounded-full p-2 text-on-surface-variant transition-colors hover:bg-surface-container-high">
          <Bell className="h-5 w-5" />
          <span className="absolute right-1.5 top-1.5 h-2 w-2 rounded-full border border-surface bg-red-500" />
        </button>
        <button className="rounded-full p-2 text-on-surface-variant transition-colors hover:bg-surface-container-high">
          <HelpCircle className="h-5 w-5" />
        </button>
        <div className="ml-3 flex h-8 w-8 items-center justify-center rounded-full border border-outline bg-primary-container text-xs font-bold text-on-primary-container">
          AD
        </div>
      </div>
    </header>
  );
}
