const storageKey = "commonword.playerId";

const getStoredId = () => {
  if (typeof window === "undefined" || !window.localStorage) return null;
  return window.localStorage.getItem(storageKey);
};

const setStoredId = (value: string) => {
  if (typeof window === "undefined" || !window.localStorage) return;
  window.localStorage.setItem(storageKey, value);
};

const createId = () => {
  if (typeof crypto !== "undefined" && "randomUUID" in crypto) {
    return crypto.randomUUID();
  }
  return `player-${Math.random().toString(36).slice(2, 10)}`;
};

export const getOrCreatePlayerId = () => {
  const existing = getStoredId();
  if (existing) return existing;

  const next = createId();
  setStoredId(next);
  return next;
};
