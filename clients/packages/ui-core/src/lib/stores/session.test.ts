import { get } from "svelte/store";
import { beforeEach, describe, expect, it } from "vitest";
import {
  entriesStore,
  gridStore,
  loadSessionSnapshot,
  puzzleStore,
  resetSession,
  sessionStore,
  setEntryValue
} from "./session";
import type { SessionSnapshot } from "../api/types";

const snapshot: SessionSnapshot = {
  id: "session-1",
  playerId: "player-1",
  startedAt: "2026-04-16T00:00:00Z",
  updatedAt: "2026-04-16T00:05:00Z",
  completedAt: null,
  puzzle: {
    id: "puzzle-1",
    title: "Daily",
    isDaily: true,
    importedAt: "2026-04-16T00:00:00Z",
    data: {
      version: 1,
      width: 2,
      height: 2,
      blockCells: [{ row: 0, col: 1 }],
      clues: {
        across: [{ number: 1, text: "Across" }],
        down: [{ number: 1, text: "Down" }]
      }
    }
  },
  entries: [
    {
      row: 0,
      col: 0,
      value: "A",
      updatedAt: "2026-04-16T00:05:00Z"
    }
  ]
};

describe("session store helpers", () => {
  beforeEach(() => {
    resetSession();
  });

  it("loads a snapshot into the Svelte stores", () => {
    loadSessionSnapshot(snapshot);

    expect(get(puzzleStore)?.id).toBe("puzzle-1");
    expect(get(sessionStore)?.id).toBe("session-1");
    expect(get(entriesStore).get("0,0")).toBe("A");
    expect(get(gridStore)[0][1]).toEqual({
      row: 0,
      col: 1,
      isBlock: true
    });
  });

  it("adds and clears a single entry value", () => {
    setEntryValue(1, 0, "B");
    expect(get(entriesStore).get("1,0")).toBe("B");

    setEntryValue(1, 0, "");
    expect(get(entriesStore).has("1,0")).toBe(false);
  });
});
