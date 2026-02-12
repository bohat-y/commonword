const storageKey = "commonword.telegram.playerId";

const getStoredId = () => {
  if (typeof window === "undefined" || !window.localStorage) return null;
  return window.localStorage.getItem(storageKey);
};

const setStoredId = (value: string) => {
  if (typeof window === "undefined" || !window.localStorage) return;
  window.localStorage.setItem(storageKey, value);
};

const createId = () => `tg-${Math.random().toString(36).slice(2, 10)}`;

type TelegramWebApp = {
  initDataUnsafe?: {
    user?: { id?: number | string };
  };
};

type TelegramWindow = Window & {
  Telegram?: {
    WebApp?: TelegramWebApp;
  };
};

export const getOrCreatePlayerId = () => {
  const telegram = typeof window !== "undefined" ? (window as TelegramWindow).Telegram?.WebApp : null;
  const telegramId = telegram?.initDataUnsafe?.user?.id;
  if (telegramId) return `tg-${telegramId}`;

  const stored = getStoredId();
  if (stored) return stored;

  const next = createId();
  setStoredId(next);
  return next;
};
