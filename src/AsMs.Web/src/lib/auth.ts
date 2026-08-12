"use client";

const tokenKey = "asms.access-token";
const rolesKey = "asms.roles";
export type UserRole = "Admin" | "Teacher" | "Student";
export function saveSession(accessToken: string, roles: string[]) { sessionStorage.setItem(tokenKey, accessToken); sessionStorage.setItem(rolesKey, JSON.stringify(roles)); }
export function getToken(): string | null { if (typeof window === "undefined") return null; return sessionStorage.getItem(tokenKey); }
export function getRoles(): UserRole[] { if (typeof window === "undefined") return []; const value = sessionStorage.getItem(rolesKey); return value ? JSON.parse(value) : []; }
export function clearSession() { if (typeof window === "undefined") return; sessionStorage.removeItem(tokenKey); sessionStorage.removeItem(rolesKey); }
