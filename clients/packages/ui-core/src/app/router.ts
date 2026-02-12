import { writable } from "svelte/store";

export type Route = { name: "home" } | { name: "solve"; sessionId: string };

const parseHash = (hash: string): Route => {
  const cleaned = hash.replace(/^#/, "");
  if (cleaned.startsWith("/solve/")) {
    const sessionId = cleaned.replace("/solve/", "");
    if (sessionId) return { name: "solve", sessionId };
  }
  return { name: "home" };
};

export const route = writable<Route>({ name: "home" });

export const initRouter = () => {
  if (typeof window === "undefined") return;

  const update = () => route.set(parseHash(window.location.hash));
  window.addEventListener("hashchange", update);
  update();

  return () => window.removeEventListener("hashchange", update);
};

export const navigateToHome = () => {
  if (typeof window === "undefined") return;
  window.location.hash = "#/";
};

export const navigateToSolve = (sessionId: string) => {
  if (typeof window === "undefined") return;
  window.location.hash = `#/solve/${sessionId}`;
};
