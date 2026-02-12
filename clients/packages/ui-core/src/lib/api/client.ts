import { apiFetch, ApiError } from "./http";
import type {
  CheckWordRequest,
  CheckWordResponse,
  PuzzlePublicDto,
  SessionSnapshot,
  SessionSnapshotDto,
  StartSessionRequest,
  StartSessionResponse,
  TelemetryEventRequest,
  UpsertEntryRequest
} from "./types";

const rawBase = import.meta.env.VITE_API_BASE_URL || "http://localhost:5000";
const apiBase = rawBase.replace(/\/+$/, "");

const normalizeSession = (payload: SessionSnapshotDto): SessionSnapshot => {
  if ("session" in payload) {
    return {
      id: payload.session.id,
      puzzle: payload.puzzle,
      entries: payload.entries ?? [],
      playerId: payload.session.playerId,
      startedAt: payload.session.startedAt,
      updatedAt: payload.session.updatedAt,
      completedAt: payload.session.completedAt ?? null
    };
  }

  return {
    id: payload.id,
    puzzle: payload.puzzle,
    entries: payload.entries ?? [],
    playerId: payload.playerId,
    startedAt: payload.startedAt,
    updatedAt: payload.updatedAt,
    completedAt: payload.completedAt ?? null
  };
};

export const getTodayPuzzle = async (): Promise<PuzzlePublicDto | null> => {
  try {
    return await apiFetch<PuzzlePublicDto>(apiBase, "/puzzles/today");
  } catch (err) {
    if (err instanceof ApiError && err.status === 404) {
      return null;
    }
    throw err;
  }
};

export const getPuzzle = (id: string) => apiFetch<PuzzlePublicDto>(apiBase, `/puzzles/${id}`);

export const startSession = async (request: StartSessionRequest): Promise<StartSessionResponse> => {
  const response = await apiFetch<StartSessionResponse>(apiBase, "/sessions", {
    method: "POST",
    body: JSON.stringify(request)
  });

  return { id: response.id };
};

export const getSession = async (id: string): Promise<SessionSnapshot> => {
  const response = await apiFetch<SessionSnapshotDto>(apiBase, `/sessions/${id}`);
  return normalizeSession(response);
};

export const setCell = (sessionId: string, row: number, col: number, request: UpsertEntryRequest) =>
  apiFetch<void>(apiBase, `/sessions/${sessionId}/cells/${row}/${col}`, {
    method: "PUT",
    body: JSON.stringify(request)
  });

export const checkWord = (sessionId: string, request: CheckWordRequest): Promise<CheckWordResponse> =>
  apiFetch<CheckWordResponse>(apiBase, `/sessions/${sessionId}/check-word`, {
    method: "POST",
    body: JSON.stringify(request)
  });

export const postTelemetry = (request: TelemetryEventRequest) =>
  apiFetch<void>(apiBase, "/telemetry/events", {
    method: "POST",
    body: JSON.stringify(request)
  });
