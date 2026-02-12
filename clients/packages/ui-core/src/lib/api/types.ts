export type WordDirection = "across" | "down";

export type PuzzleClue = {
  number: number;
  text: string;
};

export type PuzzleClues = {
  across: PuzzleClue[];
  down: PuzzleClue[];
};

export type PuzzleBlockCell = {
  row: number;
  col: number;
};

export type PuzzleWordIndexEntry = {
  row: number;
  col: number;
  length: number;
};

export type PuzzleWordIndex = {
  across: Record<string, PuzzleWordIndexEntry>;
  down: Record<string, PuzzleWordIndexEntry>;
};

export type PuzzleMeta = {
  author?: string | null;
  title?: string | null;
  source?: string | null;
};

export type PuzzlePublicData = {
  version: number;
  width: number;
  height: number;
  blockCells: PuzzleBlockCell[];
  clues: PuzzleClues;
  wordIndex?: PuzzleWordIndex | null;
  meta?: PuzzleMeta | null;
};

export type PuzzlePublicDto = {
  id: string;
  title: string;
  isDaily: boolean;
  importedAt: string;
  data: PuzzlePublicData;
};

export type StartSessionRequest = {
  puzzleId: string;
  playerId: string;
};

export type StartSessionResponse = {
  id: string;
};

export type UpsertEntryRequest = {
  value: string;
};

export type SolveSessionDto = {
  id: string;
  puzzleId?: string;
  playerId: string;
  startedAt: string;
  updatedAt: string;
  completedAt?: string | null;
};

export type EntryDto = {
  row: number;
  col: number;
  value: string;
  updatedAt: string;
};

export type SessionEnvelopeDto = {
  session: SolveSessionDto;
  puzzle: PuzzlePublicDto;
  entries: EntryDto[];
};

export type SessionSnapshotDto =
  | (SolveSessionDto & { puzzle: PuzzlePublicDto; entries: EntryDto[] })
  | SessionEnvelopeDto;

export type SessionSnapshot = {
  id: string;
  puzzle: PuzzlePublicDto;
  entries: EntryDto[];
  playerId: string;
  startedAt: string;
  updatedAt: string;
  completedAt?: string | null;
};

export type CheckWordRequest = {
  direction: WordDirection;
  number: number;
};

export type CheckWordResponse = {
  complete: boolean;
  correct: boolean;
  incorrectCells?: { row: number; col: number }[] | null;
};

export type TelemetryEventRequest = {
  occurredAt?: string | null;
  client: "telegram" | "tauri" | "web";
  playerId: string;
  type: string;
  payload: Record<string, unknown>;
  sessionId?: string | null;
};
