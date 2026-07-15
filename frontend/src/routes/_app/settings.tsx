import { createFileRoute } from "@tanstack/react-router";
import { Settings } from "lucide-react";
import { ComingSoon } from "@/components/ui/coming-soon";

export const Route = createFileRoute("/_app/settings")({
  component: () => (
    <ComingSoon
      title="Settings"
      subtitle="Configure system preferences."
      icon={Settings}
    />
  ),
});
