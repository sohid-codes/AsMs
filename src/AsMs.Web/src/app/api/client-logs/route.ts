import { appendFile, mkdir } from "node:fs/promises";
import path from "node:path";
import { NextResponse } from "next/server";

export const runtime = "nodejs";

const logFilePath = process.env.WEB_LOG_FILE_PATH ?? "C:\\Logs\\ASMS\\AsMs.Web.Log";

export async function POST(request: Request) {
  try {
    const body = await request.json() as { level?: string; message?: string; metadata?: Record<string, unknown> };
    if (body.level !== "error" || typeof body.message !== "string" || body.message.length === 0 || body.message.length > 1000) {
      return NextResponse.json({ title: "Invalid log payload." }, { status: 400 });
    }

    await mkdir(path.dirname(logFilePath), { recursive: true });
    const date = new Date().toISOString().slice(0, 10).replaceAll("-", "");
    const datedLogFilePath = `${logFilePath}-${date}.log`;
    const entry = JSON.stringify({ timestampUtc: new Date().toISOString(), level: body.level, message: body.message, metadata: body.metadata ?? {} });
    await appendFile(datedLogFilePath, `${entry}\n`, "utf8");
    return new NextResponse(null, { status: 204 });
  } catch {
    return NextResponse.json({ title: "Unable to record client log." }, { status: 500 });
  }
}
