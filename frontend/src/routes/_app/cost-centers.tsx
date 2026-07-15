import { createFileRoute } from "@tanstack/react-router";
import { Wallet } from "lucide-react";
import { ComingSoon } from "@/components/ui/coming-soon";

export const Route = createFileRoute("/_app/cost-centers")({
  component: () => (
    <ComingSoon
      title="Cost Centers"
      subtitle="Manage cost center codes for budget tracking."
      icon={Wallet}
    />
  ),
});
