import type { LucideIcon } from "lucide-react";
import { Construction } from "lucide-react";
import { PageHeader } from "@/components/ui/page-header";

interface ComingSoonProps {
  title: string;
  subtitle?: string;
  icon?: LucideIcon;
}

export function ComingSoon({ title, subtitle, icon: Icon = Construction }: ComingSoonProps) {
  return (
    <>
      <PageHeader title={title} subtitle={subtitle} />
      <div className="flex flex-1 flex-col items-center justify-center rounded-xl border border-dashed border-outline bg-surface p-12 text-center">
        <div className="mb-4 flex h-16 w-16 items-center justify-center rounded-full bg-surface-container-high">
          <Icon className="h-8 w-8 text-on-surface-variant" />
        </div>
        <h3 className="text-lg font-semibold text-on-surface">Coming Soon</h3>
        <p className="mt-1 max-w-md text-sm text-on-surface-variant">
          This module is part of the ProcureMax roadmap and will be available in
          a future release.
        </p>
      </div>
    </>
  );
}
