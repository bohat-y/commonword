import { writable } from "svelte/store";
import type { PuzzlePublicDto, SessionSnapshot } from "../api/types";
import { setCell } from "../api/client";

export type SelectedCell = { row: number; col: number };

export type GridCell = { row: number; col: number; isBlock: boolean };
export type GridMatrix = GridCell[][];

export type SessionInfo = {
  id: string;
  playerId: string;
  startedAt: string;
  updatedAt: string;
  completedAt?: string | null;
};

export const puzzleStore = writable<PuzzlePublicDto | null>(null);
export const sessionStore = writable<SessionInfo | null>(null);
export const entriesStore = writable<Map<string, string>>(new Map());
export const selectedCellStore = writable<SelectedCell | null>(null);
export const gridStore = writable<GridMatrix>([]);
export const loadingStore = writable<boolean>(false);
export const errorStore = writable<string>("");
const entryKey = (row: number, col: number) => `${row},${col}`;

const buildGrid = (
  width: number,
  height: number,
  blockCells: { row: number; col: number }[]
): GridMatrix => {
  const blockSet = new Set(blockCells.map((cell) => entryKey(cell.row, cell.col)));
  return Array.from({ length: height }, (_, rowIndex) =>
    Array.from({ length: width }, (_, colIndex) => ({
      row: rowIndex,
      col: colIndex,
      isBlock: blockSet.has(entryKey(rowIndex, colIndex))
    }))
  );
};

export const resetSession = () => {
  puzzleStore.set(null);
  sessionStore.set(null);
  entriesStore.set(new Map());
  selectedCellStore.set(null);
  gridStore.set([]);
  loadingStore.set(false);
  errorStore.set("");
};

export const loadSessionSnapshot = (snapshot: SessionSnapshot) => {
  puzzleStore.set(snapshot.puzzle);
  sessionStore.set({
    id: snapshot.id,
    playerId: snapshot.playerId,
    startedAt: snapshot.startedAt,
    updatedAt: snapshot.updatedAt,
    completedAt: snapshot.completedAt ?? null
  });

  gridStore.set(
    buildGrid(
      snapshot.puzzle.data.width,
      snapshot.puzzle.data.height,
      snapshot.puzzle.data.blockCells
    )
  );

  const map = new Map<string, string>();
  for (const entry of snapshot.entries || []) {
    map.set(entryKey(entry.row, entry.col), entry.value);
  }
  entriesStore.set(map);
};

export const setSelectedCell = (cell: SelectedCell | null) => {
  selectedCellStore.set(cell);
};

export const setEntryValue = (row: number, col: number, value: string) => {
  entriesStore.update((current) => {
    const next = new Map(current);
    if (value) {
      next.set(entryKey(row, col), value);
    } else {
      next.delete(entryKey(row, col));
    }
    return next;
  });
};

type PendingEntry = {
  sessionId: string;
  row: number;
  col: number;
  value: string;
};

const pending = new Map<string, PendingEntry>();
let timer: number | null = null;
let flushPromise: Promise<void> | null = null;
let flushResolve: (() => void) | null = null;

export const queueEntrySync = (
  entry: PendingEntry,
  onError?: (err: unknown) => void
): Promise<void> => {
  pending.set(entryKey(entry.row, entry.col), entry);

  if (!flushPromise) {
    flushPromise = new Promise<void>((resolve) => {
      flushResolve = resolve;
    });
  }

  if (timer) {
    window.clearTimeout(timer);
  }

  timer = window.setTimeout(async () => {
    const entries = Array.from(pending.values());
    pending.clear();
    timer = null;

    try {
      for (const item of entries) {
        try {
          await setCell(item.sessionId, item.row, item.col, { value: item.value });
        } catch (err) {
          if (onError) onError(err);
        }
      }
    } finally {
      flushResolve?.();
      flushResolve = null;
      flushPromise = null;
    }
  }, 200);

  return flushPromise;
};
