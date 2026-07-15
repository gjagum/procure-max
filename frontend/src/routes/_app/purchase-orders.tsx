import { createFileRoute } from "@tanstack/react-router";
import { ShoppingCart } from "lucide-react";
import { ComingSoon } from "@/components/ui/coming-soon";

export const Route = createFileRoute("/_app/purchase-orders")({
  component: () => (
    <ComingSoon
      title="Purchase Orders"
      subtitle="Issue and manage purchase orders to suppliers."
      icon={ShoppingCart}
    />
  ),
});
