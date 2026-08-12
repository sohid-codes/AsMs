"use client";
import Link from "next/link";
import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { clearSession, getRoles, type UserRole } from "@/lib/auth";
export function DashboardShell({ role, children }: { role: UserRole; children: React.ReactNode }) {
  const router = useRouter(); const [ready, setReady] = useState(false);
  useEffect(() => { if (!getRoles().includes(role)) router.replace("/login"); else setReady(true); }, [role, router]);
  if (!ready) return null;
  return <div className="min-h-screen bg-slate-50 text-slate-900"><header className="border-b border-slate-200 bg-white"><div className="mx-auto flex h-16 max-w-7xl items-center justify-between px-5"><Link href={`/${role.toLowerCase()}`} className="text-lg font-bold text-indigo-700">ASMS</Link><div className="flex items-center gap-3"><span className="rounded-full bg-indigo-50 px-3 py-1 text-sm font-medium text-indigo-700">{role}</span><button onClick={() => { clearSession(); router.push("/login"); }} className="text-sm font-medium text-slate-600">Sign out</button></div></div></header><main className="mx-auto max-w-7xl px-5 py-7">{children}</main></div>;
}
