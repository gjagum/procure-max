import { createFileRoute } from "@tanstack/react-router";
import { Landmark } from "lucide-react";
import { ComingSoon } from "@/components/ui/coming-soon";

export const Route = createFileRoute("/_app/gl-accounts")({
  component: () => (
    <ComingSoon
      title="GL Accounts"
      subtitle="Manage general ledger account codes."
      icon={Landmark}
    />
  ),
});
