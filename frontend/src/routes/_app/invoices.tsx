import { createFileRoute } from "@tanstack/react-router";
import { Receipt } from "lucide-react";
import { ComingSoon } from "@/components/ui/coming-soon";

export const Route = createFileRoute("/_app/invoices")({
  component: () => (
    <ComingSoon
      title="Invoices"
      subtitle="Manage supplier invoices and approvals."
      icon={Receipt}
    />
  ),
});
