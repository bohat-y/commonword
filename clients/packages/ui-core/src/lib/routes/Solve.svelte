<script lang="ts">
  import { onMount } from "svelte";
  import { get } from "svelte/store";
  import Grid from "../components/Grid.svelte";
  import ClueList from "../components/ClueList.svelte";
  import { ApiError } from "../api/http";
  import { checkWord, getSession } from "../api/client";
  import { navigateToHome } from "../../app/router";
  import {
    entriesStore,
    errorStore,
    gridStore,
    loadSessionSnapshot,
    loadingStore,
    puzzleStore,
    queueEntrySync,
    resetSession,
    selectedCellStore,
    sessionStore,
    setEntryValue,
    setSelectedCell
  } from "../stores/session";
  import type { PuzzlePublicData, PuzzleWordIndexEntry, WordDirection } from "../api/types";
  import { track } from "../platform";

  export let sessionId: string;

  type ClueSelection = { direction: WordDirection; number: number } | null;

  let inputValue = "";
  let activeDirection: WordDirection = "across";
  let selectedClue: ClueSelection = null;
  let highlighted = new Set<string>();
  let incorrect = new Set<string>();
  let correct = new Set<string>();
  let statusMessage = "";
  let checking = false;
  let incorrectToken = 0;
  let correctToken = 0;
  let labelMap = new Map<string, number>();
  let entryInput: HTMLInputElement | null = null;
  let solvedClues = new Set<string>();
  let checkQueue: Promise<void> = Promise.resolve();
  const keyOf = (row: number, col: number) => `${row},${col}`;

  const focusEntryField = () => {
    if (!entryInput) return;
    try {
      entryInput.focus({ preventScroll: true });
    } catch {
      entryInput.focus();
    }
  };

  let blockSet = new Set<string>();
  let wordStartMaps: Record<WordDirection, Map<string, number>> = {
    across: new Map(),
    down: new Map()
  };

  const rebuildMaps = (data: PuzzlePublicData) => {
    blockSet = new Set(data.blockCells.map((cell) => keyOf(cell.row, cell.col)));

    const buildStartMap = (index?: Record<string, PuzzleWordIndexEntry> | null) => {
      const map = new Map<string, number>();
      if (!index) return map;
      for (const [number, entry] of Object.entries(index)) {
        map.set(keyOf(entry.row, entry.col), Number(number));
      }
      return map;
    };

    wordStartMaps = {
      across: buildStartMap(data.wordIndex?.across),
      down: buildStartMap(data.wordIndex?.down)
    };

    const nextLabels = new Map<string, number>();
    if (data.wordIndex) {
      for (const [number, entry] of Object.entries(data.wordIndex.across ?? {})) {
        nextLabels.set(keyOf(entry.row, entry.col), Number(number));
      }
      for (const [number, entry] of Object.entries(data.wordIndex.down ?? {})) {
        nextLabels.set(keyOf(entry.row, entry.col), Number(number));
      }
    }
    labelMap = nextLabels;
  };

  const findWordStart = (
    row: number,
    col: number,
    direction: WordDirection,
    data: PuzzlePublicData
  ) => {
    let r = row;
    let c = col;

    if (direction === "across") {
      while (c > 0 && !blockSet.has(keyOf(r, c - 1))) {
        c -= 1;
      }
    } else {
      while (r > 0 && !blockSet.has(keyOf(r - 1, c))) {
        r -= 1;
      }
    }

    return { row: r, col: c };
  };

  const resolveClueForCell = (
    row: number,
    col: number,
    direction: WordDirection
  ): ClueSelection => {
    const data = get(puzzleStore)?.data;
    if (!data?.wordIndex) return null;

    const start = findWordStart(row, col, direction, data);
    const number = wordStartMaps[direction].get(keyOf(start.row, start.col));
    if (!number) return null;

    return { direction, number };
  };

  const updateSelectedClue = (row: number, col: number) => {
    const primary = resolveClueForCell(row, col, activeDirection);
    if (primary) {
      selectedClue = primary;
      return;
    }

    const fallbackDirection: WordDirection = activeDirection === "across" ? "down" : "across";
    const fallback = resolveClueForCell(row, col, fallbackDirection);
    if (fallback) {
      activeDirection = fallbackDirection;
      selectedClue = fallback;
      return;
    }

    selectedClue = null;
  };

  const buildHighlighted = (clue: ClueSelection) => {
    const data = get(puzzleStore)?.data;
    if (!data?.wordIndex || !clue) return new Set<string>();
    const entry = data.wordIndex[clue.direction]?.[String(clue.number)];
    if (!entry) return new Set<string>();

    const cells = new Set<string>();
    for (let i = 0; i < entry.length; i += 1) {
      const row = entry.row + (clue.direction === "down" ? i : 0);
      const col = entry.col + (clue.direction === "across" ? i : 0);
      cells.add(keyOf(row, col));
    }
    return cells;
  };

  const buildWordCells = (clue: ClueSelection) => {
    const data = get(puzzleStore)?.data;
    if (!data?.wordIndex || !clue) return new Set<string>();
    const entry = data.wordIndex[clue.direction]?.[String(clue.number)];
    if (!entry) return new Set<string>();

    const cells = new Set<string>();
    for (let i = 0; i < entry.length; i += 1) {
      const row = entry.row + (clue.direction === "down" ? i : 0);
      const col = entry.col + (clue.direction === "across" ? i : 0);
      cells.add(keyOf(row, col));
    }
    return cells;
  };

  const wordKey = (direction: WordDirection, number: number) => `${direction}:${number}`;

  const getWordEntry = (clue: ClueSelection) => {
    const data = get(puzzleStore)?.data;
    if (!data?.wordIndex || !clue) return null;
    return data.wordIndex[clue.direction]?.[String(clue.number)] ?? null;
  };

  const isWordComplete = (clue: ClueSelection, entries: Map<string, string>) => {
    const entry = getWordEntry(clue);
    if (!entry) return false;

    for (let i = 0; i < entry.length; i += 1) {
      const row = entry.row + (clue?.direction === "down" ? i : 0);
      const col = entry.col + (clue?.direction === "across" ? i : 0);
      const value = entries.get(keyOf(row, col)) || "";
      if (!/^[A-Z]$/.test(value)) return false;
    }
    return true;
  };

  const enqueueCheck = (clue: ClueSelection, showStatus: boolean) => {
    if (!clue) return;
    checkQueue = checkQueue.then(async () => {
      await runCheckWordInternal(clue, showStatus);
    });
  };

  const markSolved = (clue: ClueSelection, solved: boolean) => {
    if (!clue) return;
    const key = wordKey(clue.direction, clue.number);
    const next = new Set(solvedClues);
    if (solved) {
      next.add(key);
    } else {
      next.delete(key);
    }
    solvedClues = next;
  };

  const clearSolvedForCell = (row: number, col: number) => {
    const across = resolveClueForCell(row, col, "across");
    const down = resolveClueForCell(row, col, "down");
    if (across) {
      markSolved(across, false);
    }
    if (down) {
      markSolved(down, false);
    }
  };

  const selectCell = (row: number, col: number) => {
    const current = get(selectedCellStore);
    if (current && current.row === row && current.col === col) {
      const otherDirection: WordDirection = activeDirection === "across" ? "down" : "across";
      const other = resolveClueForCell(row, col, otherDirection);
      if (other) {
        activeDirection = otherDirection;
        selectedClue = other;
        focusEntryField();
        return;
      }
    }
    setSelectedCell({ row, col });
    updateSelectedClue(row, col);
    focusEntryField();
  };

  const moveSelection = (deltaRow: number, deltaCol: number) => {
    const data = get(puzzleStore)?.data;
    const selected = get(selectedCellStore);
    if (!data || !selected) return;

    let r = selected.row + deltaRow;
    let c = selected.col + deltaCol;

    while (r >= 0 && r < data.height && c >= 0 && c < data.width) {
      if (!blockSet.has(keyOf(r, c))) {
        selectCell(r, c);
        return;
      }
      r += deltaRow;
      c += deltaCol;
    }
  };

  const findPreviousCell = (row: number, col: number, direction: WordDirection) => {
    const data = get(puzzleStore)?.data;
    if (!data) return null;

    let r = row + (direction === "down" ? -1 : 0);
    let c = col + (direction === "across" ? -1 : 0);

    while (r >= 0 && r < data.height && c >= 0 && c < data.width) {
      if (!blockSet.has(keyOf(r, c))) {
        return { row: r, col: c };
      }
      r += direction === "down" ? -1 : 0;
      c += direction === "across" ? -1 : 0;
    }

    return null;
  };

  const clearCurrentOrMoveBackward = () => {
    const selected = get(selectedCellStore);
    if (!selected) return;

    const currentValue = get(entriesStore).get(keyOf(selected.row, selected.col)) || "";
    if (currentValue) {
      inputValue = "";
      applyValue("");
      return;
    }

    const previous = findPreviousCell(selected.row, selected.col, activeDirection);
    if (!previous) return;

    selectCell(previous.row, previous.col);
    const previousValue = get(entriesStore).get(keyOf(previous.row, previous.col)) || "";
    if (previousValue) {
      inputValue = "";
      applyValue("");
    }
  };

  const applyTypedCharacter = (raw: string) => {
    const value = raw.toUpperCase().replace(/[^A-Z]/g, "").slice(-1);
    inputValue = value;
    applyValue(value, value.length === 1);
  };

  const applyValue = (value: string, advance = false) => {
    const selected = get(selectedCellStore);
    const session = get(sessionStore);
    if (!selected || !session) return;

    const entriesBefore = get(entriesStore);
    const currentValue = entriesBefore.get(keyOf(selected.row, selected.col)) || "";
    const normalized = value.trim().slice(0, 1).toUpperCase();
    if (!normalized && !currentValue) {
      return;
    }

    const changed = normalized !== currentValue;
    if (changed) {
      clearSolvedForCell(selected.row, selected.col);
    }
    errorStore.set("");
    setEntryValue(selected.row, selected.col, normalized);
    const flush = queueEntrySync(
      {
        sessionId: session.id,
        row: selected.row,
        col: selected.col,
        value: normalized
      },
      () => {
        errorStore.set("Failed to sync entry.");
      }
    );

    const nextEntries = new Map(entriesBefore);
    if (normalized) {
      nextEntries.set(keyOf(selected.row, selected.col), normalized);
    } else {
      nextEntries.delete(keyOf(selected.row, selected.col));
    }

    if (changed && get(puzzleStore)?.data.wordIndex) {
      const across = resolveClueForCell(selected.row, selected.col, "across");
      const down = resolveClueForCell(selected.row, selected.col, "down");

      if (across && isWordComplete(across, nextEntries)) {
        flush.then(() => enqueueCheck(across, false));
      }
      if (down && isWordComplete(down, nextEntries)) {
        flush.then(() => enqueueCheck(down, false));
      }
    }

    if (advance && normalized) {
      if (activeDirection === "down") {
        moveSelection(1, 0);
      } else {
        moveSelection(0, 1);
      }
    }
  };

  const handleKeyDown = (event: KeyboardEvent) => {
    const target = event.target as HTMLElement | null;
    const isEntryInput = target?.classList?.contains("entry-input");
    if (event.metaKey || event.ctrlKey || event.altKey || event.isComposing) return;

    const selected = get(selectedCellStore);
    if (!selected) return;

    if (event.key === "ArrowUp") {
      event.preventDefault();
      moveSelection(-1, 0);
      return;
    }

    if (event.key === "ArrowDown") {
      event.preventDefault();
      moveSelection(1, 0);
      return;
    }

    if (event.key === "ArrowLeft") {
      event.preventDefault();
      moveSelection(0, -1);
      return;
    }

    if (event.key === "ArrowRight") {
      event.preventDefault();
      moveSelection(0, 1);
      return;
    }

    if (event.key === "Backspace" || event.key === "Delete") {
      event.preventDefault();
      clearCurrentOrMoveBackward();
      return;
    }

    if (isEntryInput) return;

    if (/^[a-zA-Z]$/.test(event.key)) {
      event.preventDefault();
      applyTypedCharacter(event.key);
    }
  };

  const handleBeforeInput = (event: InputEvent) => {
    const selected = get(selectedCellStore);
    if (!selected) return;

    if (
      event.inputType === "deleteContentBackward" ||
      event.inputType === "deleteContentForward"
    ) {
      event.preventDefault();
      clearCurrentOrMoveBackward();
      return;
    }

    if (!event.inputType.startsWith("insert")) return;

    const raw = event.data ?? "";
    const value = raw.toUpperCase().replace(/[^A-Z]/g, "").slice(-1);
    if (!value) {
      event.preventDefault();
      return;
    }

    event.preventDefault();
    applyTypedCharacter(value);
  };

  const handleInput = (event: Event) => {
    const target = event.currentTarget as HTMLInputElement | null;
    if (!target) return;

    applyTypedCharacter(target.value);
  };

  const handleClueSelect = (direction: WordDirection, number: number) => {
    const data = get(puzzleStore)?.data;
    if (!data?.wordIndex) return;

    activeDirection = direction;
    selectedClue = { direction, number };

    const entry = data.wordIndex[direction]?.[String(number)];
    if (entry) {
      selectCell(entry.row, entry.col);
    }
  };

  const triggerIncorrectFlash = (cells: { row: number; col: number }[]) => {
    incorrect = new Set(cells.map((cell) => keyOf(cell.row, cell.col)));
    incorrectToken += 1;
    const token = incorrectToken;
    if (typeof window === "undefined") return;
    window.setTimeout(() => {
      if (incorrectToken === token) {
        incorrect = new Set();
      }
    }, 1200);
  };

  const triggerCorrectFlash = (clue: ClueSelection) => {
    correct = buildWordCells(clue);
    correctToken += 1;
    const token = correctToken;
    if (typeof window === "undefined") return;
    window.setTimeout(() => {
      if (correctToken === token) {
        correct = new Set();
      }
    }, 1600);
  };

  const runCheckWordInternal = async (clue: ClueSelection, showStatus: boolean) => {
    if (!clue) return;
    if (!get(puzzleStore)?.data.wordIndex) return;

    if (showStatus) statusMessage = "";

    try {
      const session = get(sessionStore);
      if (!session) return;
      const response = await checkWord(session.id, {
        direction: clue.direction,
        number: clue.number
      });

      if (response.incorrectCells && response.incorrectCells.length > 0) {
        triggerIncorrectFlash(response.incorrectCells);
      }

      if (response.correct) {
        markSolved(clue, true);
        triggerCorrectFlash(clue);
      } else {
        markSolved(clue, false);
      }

      if (showStatus) {
        if (response.correct) {
          statusMessage = "Correct word.";
        } else if (!response.complete) {
          statusMessage = "Word is incomplete.";
        } else {
          statusMessage = "Some letters are incorrect.";
        }
      }
    } catch {
      if (showStatus) {
        statusMessage = "Unable to check word.";
      }
    } finally {
      if (showStatus && statusMessage && typeof window !== "undefined") {
        const token = Date.now();
        const current = token;
        window.setTimeout(() => {
          if (current === token) {
            statusMessage = "";
          }
        }, 1600);
      }
    }
  };

  const handleCheckWord = async () => {
    const session = get(sessionStore);
    if (!session || !selectedClue) return;

    if (checking) return;
    checking = true;
    try {
      await runCheckWordInternal(selectedClue, true);
    } finally {
      checking = false;
    }
  };

  const load = async () => {
    if (!sessionId) {
      errorStore.set("Missing session id.");
      loadingStore.set(false);
      return;
    }

    resetSession();
    loadingStore.set(true);
    errorStore.set("");

    try {
      const details = await getSession(sessionId);
      loadSessionSnapshot(details);
      if (typeof window !== "undefined") {
        window.localStorage.setItem(`commonword.session.${details.puzzle.id}`, details.id);
      }
      await track("session_loaded", { sessionId }, sessionId);
    } catch (err) {
      if (err instanceof ApiError && err.status === 404) {
        errorStore.set("Session not found.");
      } else {
        errorStore.set("Unable to load session.");
      }
    } finally {
      loadingStore.set(false);
    }
  };

  onMount(() => {
    load();
    window.addEventListener("keydown", handleKeyDown);
    return () => window.removeEventListener("keydown", handleKeyDown);
  });

  $: if ($puzzleStore) {
    rebuildMaps($puzzleStore.data);
  } else {
    blockSet = new Set();
    wordStartMaps = { across: new Map(), down: new Map() };
    labelMap = new Map();
  }

  $: highlighted = buildHighlighted(selectedClue);
  $: if (!$puzzleStore) {
    solvedClues = new Set();
  }

  $: if ($selectedCellStore) {
    const key = keyOf($selectedCellStore.row, $selectedCellStore.col);
    inputValue = $entriesStore.get(key) || "";
  } else {
    inputValue = "";
  }

  const clueText = (direction: WordDirection, number: number) => {
    const data = get(puzzleStore)?.data;
    if (!data) return "";
    const list = direction === "across" ? data.clues.across : data.clues.down;
    return list.find((clue) => clue.number === number)?.text ?? "";
  };
</script>

<main class="shell">
  <header class="header">
    <button class="link" on:click={navigateToHome}>Back</button>
    <div>
      <h1>{$puzzleStore?.title || "Solve"}</h1>
      {#if $puzzleStore}
        <p class="meta">
          {$puzzleStore.data.width}x{$puzzleStore.data.height} puzzle
        </p>
      {/if}
    </div>
  </header>

  {#if $loadingStore}
    <p>Loading session...</p>
  {:else if $errorStore}
    <p class="error">{$errorStore}</p>
  {:else if $puzzleStore}
    <div class="layout">
      <section class="board" on:touchstart={focusEntryField}>
    <Grid
      grid={$gridStore}
      entries={$entriesStore}
      selected={$selectedCellStore}
      highlighted={highlighted}
      incorrect={incorrect}
      correct={correct}
      labels={labelMap}
      onSelect={selectCell}
    />

        <input
          class="entry-input"
          type="text"
          maxlength="1"
          bind:this={entryInput}
          bind:value={inputValue}
          on:beforeinput={handleBeforeInput}
          on:input={handleInput}
          aria-label="Cell entry"
          autocomplete="off"
          autocapitalize="characters"
          autocorrect="off"
          inputmode="text"
          spellcheck="false"
          enterkeyhint="done"
        />

        <section class="clue-actions">
          {#if selectedClue}
            <div>
              <p class="clue-label">
                {selectedClue.number} {selectedClue.direction.toUpperCase()}
              </p>
              <p class="clue-text">{clueText(selectedClue.direction, selectedClue.number)}</p>
            </div>
          {:else}
            <p class="clue-text">Select a cell to see its clue.</p>
          {/if}

          <div class="clue-buttons">
            <button
              class="secondary"
              disabled={!$selectedCellStore}
              on:click={() => {
                inputValue = "";
                applyValue("");
              }}
            >
              Clear cell
            </button>
            <button class="secondary" disabled={!selectedClue || checking} on:click={handleCheckWord}>
              {checking ? "Checking..." : "Check word"}
            </button>
            {#if statusMessage}
              <span class="status">{statusMessage}</span>
            {/if}
          </div>
        </section>
      </section>

      <aside class="clues-panel">
        <ClueList
          clues={$puzzleStore.data.clues}
          active={selectedClue}
          completed={solvedClues}
          onSelect={handleClueSelect}
        />
      </aside>
    </div>
  {/if}
</main>

<style>
  .shell {
    padding: 24px;
    max-width: 1200px;
    margin: 0 auto;
  }

  .header {
    display: grid;
    gap: 12px;
    margin-bottom: 16px;
  }

  .link {
    background: none;
    border: none;
    color: var(--accent, #1f2937);
    font-weight: 600;
    cursor: pointer;
    padding: 0;
    width: max-content;
  }

  .meta {
    margin: 6px 0 0 0;
    color: var(--text-muted, #6b7280);
  }

  button.secondary {
    padding: 8px 12px;
    border: 1px solid #d1d5db;
    background: #ffffff;
    border-radius: 6px;
    cursor: pointer;
  }

  .layout {
    display: grid;
    gap: 24px;
  }

  .board {
    min-width: 0;
    position: relative;
  }

  .clues-panel {
    min-width: 0;
  }

  .clue-actions {
    display: flex;
    justify-content: space-between;
    align-items: flex-start;
    gap: 16px;
    margin-top: 16px;
    padding: 12px 14px;
    border-radius: 10px;
    background: #f8fafc;
  }

  .clue-label {
    margin: 0;
    font-weight: 700;
  }

  .clue-text {
    margin: 4px 0 0 0;
    color: var(--text-muted, #4b5563);
  }

  .clue-buttons {
    display: grid;
    gap: 6px;
    justify-items: end;
  }

  .status {
    font-size: 13px;
    color: var(--text-muted, #4b5563);
  }

  .error {
    color: #b00020;
  }

  .entry-input {
    position: absolute;
    top: 0;
    left: 0;
    opacity: 0;
    width: 1px;
    height: 1px;
    border: 0;
    padding: 0;
    font-size: 16px;
    pointer-events: none;
  }

  @media (min-width: 980px) {
    .layout {
      grid-template-columns: minmax(0, 1fr) 440px;
      align-items: start;
    }

    .clues-panel {
      position: sticky;
      top: 20px;
    }
  }
</style>
