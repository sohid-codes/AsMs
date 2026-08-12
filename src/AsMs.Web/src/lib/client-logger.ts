"use client";

type LogMetadata = Record<string, string | number | boolean | null | undefined>;

export function logClientError(message: string, metadata: LogMetadata = {}) {
  void fetch("/api/client-logs", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ level: "error", message, metadata: { ...metadata, path: window.location.pathname } }),
    keepalive: true,
  }).catch(() => undefined);
}
