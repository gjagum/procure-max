import { createFileRoute } from "@tanstack/react-router";
import { ShieldCheck } from "lucide-react";
import { ComingSoon } from "@/components/ui/coming-soon";

export const Route = createFileRoute("/_app/roles")({
  component: () => (
    <ComingSoon
      title="Roles & Permissions"
      subtitle="Manage roles and their permission assignments."
      icon={ShieldCheck}
    />
  ),
});
