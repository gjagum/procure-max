import { createFileRoute } from "@tanstack/react-router";
import { Ruler } from "lucide-react";
import { ComingSoon } from "@/components/ui/coming-soon";

export const Route = createFileRoute("/_app/units")({
  component: () => (
    <ComingSoon
      title="Units of Measure"
      subtitle="Manage measurement units for items."
      icon={Ruler}
    />
  ),
});
