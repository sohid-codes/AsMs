"use client";
import { getToken } from "@/lib/auth";
import { logClientError } from "@/lib/client-logger";
const baseUrl = process.env.NEXT_PUBLIC_API_BASE_URL ?? "https://localhost:7295";
export async function api<T>(path: string, init: RequestInit = {}): Promise<T> {
  const token = getToken(); const response = await fetch(baseUrl + path, { ...init, headers: { "Content-Type": "application/json", ...(token ? { Authorization: `Bearer ${token}` } : {}), ...init.headers } });
  if (response.status === 401) throw new Error("Your session has expired. Please sign in again.");
  if (response.status === 403) throw new Error("You are not allowed to perform this action.");
  if (!response.ok) { const message = (await response.text()) || "The request could not be completed."; logClientError("API request failed", { path, status: response.status }); throw new Error(message); }
  if (response.status === 204) return undefined as T;
  return response.json() as Promise<T>;
}
export const send = <T>(path: string, method: string, body?: unknown) => api<T>(path, { method, body: body === undefined ? undefined : JSON.stringify(body) });
