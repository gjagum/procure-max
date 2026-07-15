import { useState } from "react";
import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { Mail, Lock, Eye, EyeOff, Package } from "lucide-react";
import { Button } from "@/components/ui/button";
import { login } from "@/features/auth/api";

export const Route = createFileRoute("/_auth/login")({
  component: LoginPage,
});

function LoginPage() {
  const navigate = useNavigate();
  const [email, setEmail] = useState("admin@procuremax.local");
  const [password, setPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError("");
    setLoading(true);
    try {
      await login(email, password);
      navigate({ to: "/dashboard" });
    } catch {
      setError("Invalid email or password.");
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="flex min-h-screen flex-col items-center justify-center bg-gradient-to-br from-blue-50 to-white p-4">
      <main className="w-full max-w-md">
        <div className="mb-8 text-center">
          <div className="mb-3 flex justify-center">
            <div className="flex h-14 w-14 items-center justify-center rounded-xl bg-primary shadow-lg">
              <Package className="h-7 w-7 text-on-primary" />
            </div>
          </div>
          <h1 className="mb-2 text-3xl font-bold text-primary">ProcureMax</h1>
          <p className="text-sm text-on-surface-variant">
            Procurement Management System
          </p>
        </div>

        <div className="rounded-xl border border-gray-100 bg-white p-8 shadow-[0_8px_30px_rgb(0,0,0,0.04)] backdrop-blur-sm sm:p-10">
          <h2 className="mb-6 text-center text-xl font-semibold text-gray-800">
            Sign In
          </h2>

          {error && (
            <div className="mb-4 rounded-lg border border-error/20 bg-error-container px-4 py-3 text-sm text-error">
              {error}
            </div>
          )}

          <form onSubmit={handleSubmit} className="space-y-6">
            <div>
              <label
                htmlFor="email"
                className="mb-2 block text-sm font-medium text-gray-700"
              >
                Email Address
              </label>
              <div className="relative">
                <Mail className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-gray-400" />
                <input
                  id="email"
                  type="email"
                  required
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  placeholder="name@company.com"
                  className="block w-full rounded-lg border border-gray-200 py-2.5 pl-10 pr-3 text-sm transition-colors focus:border-primary focus:ring-2 focus:ring-primary/20"
                />
              </div>
            </div>

            <div>
              <div className="mb-2 flex items-center justify-between">
                <label
                  htmlFor="password"
                  className="block text-sm font-medium text-gray-700"
                >
                  Password
                </label>
                <a
                  href="#"
                  className="text-sm font-medium text-primary hover:text-primary-hover"
                >
                  Forgot password?
                </a>
              </div>
              <div className="relative">
                <Lock className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-gray-400" />
                <input
                  id="password"
                  type={showPassword ? "text" : "password"}
                  required
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  placeholder="••••••••"
                  className="block w-full rounded-lg border border-gray-200 py-2.5 pl-10 pr-10 text-sm transition-colors focus:border-primary focus:ring-2 focus:ring-primary/20"
                />
                <button
                  type="button"
                  onClick={() => setShowPassword((v) => !v)}
                  className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-400 transition-colors hover:text-gray-600"
                >
                  {showPassword ? (
                    <EyeOff className="h-4 w-4" />
                  ) : (
                    <Eye className="h-4 w-4" />
                  )}
                </button>
              </div>
            </div>

            <div className="flex items-center">
              <input
                id="remember"
                type="checkbox"
                className="h-4 w-4 rounded border-gray-300 text-primary focus:ring-primary"
              />
              <label
                htmlFor="remember"
                className="ml-2 text-sm text-gray-600"
              >
                Remember this device
              </label>
            </div>

            <Button type="submit" disabled={loading} className="w-full" size="lg">
              {loading ? "Signing in..." : "Sign In"}
            </Button>
          </form>

          <div className="mt-8 text-center text-sm text-gray-500">
            Need an account?{" "}
            <a className="font-semibold text-primary hover:text-primary-hover">
              Contact Administrator
            </a>
          </div>
        </div>
      </main>

      <footer className="mt-8 flex gap-8 pb-8 text-sm text-on-surface-variant">
        <a className="transition-all hover:text-primary hover:underline">
          Privacy Policy
        </a>
        <a className="transition-all hover:text-primary hover:underline">
          Terms of Service
        </a>
        <span>© 2026 ProcureMax</span>
      </footer>
    </div>
  );
}
