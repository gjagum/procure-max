import type { ReactNode } from "react";

type Variant = "success" | "error" | "warning" | "info" | "neutral";

const variantClasses: Record<Variant, string> = {
  success: "bg-success-container text-success border-success/20",
  error: "bg-error-container text-error border-error/20",
  warning: "bg-warning-container text-warning border-warning/20",
  info: "bg-primary-container text-on-primary-container border-primary/20",
  neutral: "bg-surface-container-high text-on-surface-variant border-outline",
};

interface BadgeProps {
  variant?: Variant;
  children: ReactNode;
  dot?: boolean;
}

export function Badge({ variant = "neutral", children, dot }: BadgeProps) {
  return (
    <span
      className={`inline-flex items-center gap-1.5 rounded-full border px-2.5 py-0.5 text-xs font-semibold ${variantClasses[variant]}`}
    >
      {dot && <span className="h-1.5 w-1.5 rounded-full bg-current" />}
      {children}
    </span>
  );
}
