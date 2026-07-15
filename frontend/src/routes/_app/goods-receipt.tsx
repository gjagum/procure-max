import { createFileRoute } from "@tanstack/react-router";
import { PackageCheck } from "lucide-react";
import { ComingSoon } from "@/components/ui/coming-soon";

export const Route = createFileRoute("/_app/goods-receipt")({
  component: () => (
    <ComingSoon
      title="Goods Receipt"
      subtitle="Record received goods against purchase orders."
      icon={PackageCheck}
    />
  ),
});
