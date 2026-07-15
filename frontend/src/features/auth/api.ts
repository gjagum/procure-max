import { http, setAccessToken } from "@/lib/http";
import type { AuthResponse, UserProfile } from "@/lib/types";

export async function login(email: string, password: string): Promise<AuthResponse> {
  const res = await http<AuthResponse>("/auth/login", {
    method: "POST",
    body: JSON.stringify({ email, password }),
  });
  setAccessToken(res.accessToken);
  localStorage.setItem("refreshToken", res.refreshToken);
  return res;
}

export async function fetchProfile(): Promise<UserProfile> {
  return http<UserProfile>("/auth/me");
}

export async function logout(): Promise<void> {
  const refreshToken = localStorage.getItem("refreshToken");
  if (refreshToken) {
    await http("/auth/logout", {
      method: "POST",
      body: JSON.stringify({ refreshToken }),
    });
  }
  setAccessToken(null);
  localStorage.removeItem("refreshToken");
}
