const BASE_URL = "/api";

let accessToken: string | null = null;

export function setAccessToken(token: string | null) {
  accessToken = token;
}

export function getAccessToken() {
  return accessToken;
}

async function refreshTokens(): Promise<boolean> {
  const refreshToken = localStorage.getItem("refreshToken");
  if (!refreshToken) return false;

  const res = await fetch(`${BASE_URL}/auth/refresh`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ refreshToken }),
  });
  if (!res.ok) {
    localStorage.removeItem("refreshToken");
    setAccessToken(null);
    return false;
  }
  const data = await res.json();
  accessToken = data.accessToken;
  localStorage.setItem("refreshToken", data.refreshToken);
  return true;
}

let refreshPromise: Promise<boolean> | null = null;

export async function http<T>(
  path: string,
  options: RequestInit = {},
): Promise<T> {
  const headers = new Headers(options.headers);
  if (options.body && !headers.has("Content-Type")) {
    headers.set("Content-Type", "application/json");
  }
  if (accessToken) {
    headers.set("Authorization", `Bearer ${accessToken}`);
  }

  const doFetch = () =>
    fetch(`${BASE_URL}${path}`, { ...options, headers });

  let res = await doFetch();

  if (res.status === 401 && accessToken) {
    if (!refreshPromise) {
      refreshPromise = refreshTokens().finally(() => {
        refreshPromise = null;
      });
    }
    const refreshed = await refreshPromise;
    if (refreshed) {
      headers.set("Authorization", `Bearer ${accessToken}`);
      res = await doFetch();
    }
  }

  if (res.status === 401) {
    throw new HttpError(401, "Unauthorized");
  }
  if (res.status === 404) {
    throw new HttpError(404, "Not Found");
  }
  if (res.status === 409) {
    const body = await res.json().catch(() => ({}));
    throw new HttpError(409, body.detail ?? "Conflict");
  }
  if (res.status >= 400) {
    const body = await res.json().catch(() => ({}));
    throw new HttpError(res.status, body.detail ?? body.title ?? "Request failed");
  }
  if (res.status === 204) return undefined as T;
  return res.json() as Promise<T>;
}

export class HttpError extends Error {
  constructor(
    public status: number,
    message: string,
  ) {
    super(message);
  }
}

export function buildQueryString(params: object): string {
  const sp = new URLSearchParams();
  for (const [key, value] of Object.entries(params)) {
    if (value !== undefined && value !== null && value !== "") {
      sp.append(key, String(value));
    }
  }
  const qs = sp.toString();
  return qs ? `?${qs}` : "";
}
