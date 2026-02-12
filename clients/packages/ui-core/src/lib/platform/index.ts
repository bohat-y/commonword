import { postTelemetry } from "../api/client";
import { getOrCreatePlayerId as getTelegramPlayerId } from "./telegram";
import { getOrCreatePlayerId as getTauriPlayerId } from "./tauri";
import { getOrCreatePlayerId as getWebPlayerId } from "./web";
import type { TelemetryEventRequest } from "../api/types";

export type ClientName = "telegram" | "tauri" | "web";

type TelegramWebApp = {
  initDataUnsafe?: {
    user?: { id?: number | string };
  };
};

type PlatformWindow = Window & {
  Telegram?: { WebApp?: TelegramWebApp };
  __TAURI__?: unknown;
};

const isTelegram = () =>
  typeof window !== "undefined" && !!(window as PlatformWindow).Telegram?.WebApp;

const isTauri = () =>
  typeof window !== "undefined" && !!(window as PlatformWindow).__TAURI__;

export const getClientName = (): ClientName => {
  if (isTelegram()) return "telegram";
  if (isTauri()) return "tauri";
  return "web";
};

export const getOrCreatePlayerId = () => {
  if (isTelegram()) return getTelegramPlayerId();
  if (isTauri()) return getTauriPlayerId();
  return getWebPlayerId();
};

export const track = async (
  type: string,
  payload: Record<string, unknown>,
  sessionId?: string | null
) => {
  const event: TelemetryEventRequest = {
    occurredAt: new Date().toISOString(),
    client: getClientName(),
    playerId: getOrCreatePlayerId(),
    type,
    payload,
    sessionId: sessionId ?? null
  };

  try {
    await postTelemetry(event);
  } catch {
    // Telemetry failures should not block UX.
  }
};
