<script lang="ts">
  import Cell from "./Cell.svelte";
  import type { GridMatrix, SelectedCell } from "../stores/session";

  export let grid: GridMatrix = [];
  export let entries: Map<string, string> = new Map();
  export let selected: SelectedCell | null = null;
  export let highlighted: Set<string> = new Set();
  export let incorrect: Set<string> = new Set();
  export let correct: Set<string> = new Set();
  export let labels: Map<string, number> = new Map();
  export let onSelect: (row: number, col: number) => void;

  $: gridWidth = grid[0]?.length || 0;
  $: gridHeight = grid.length || 0;
  $: gridSize = Math.max(gridWidth, gridHeight);
  $: cellFont = (() => {
    if (gridSize <= 9) return 22;
    if (gridSize <= 11) return 20;
    if (gridSize <= 13) return 18;
    if (gridSize <= 15) return 16;
    if (gridSize <= 19) return 14;
    if (gridSize <= 23) return 12;
    return 11;
  })();
</script>

<div
  class="grid"
  style={`grid-template-columns: repeat(${gridWidth}, 1fr); --cell-font: ${cellFont}px;`}
>
  {#each grid as row}
    {#each row as cell}
      {@const key = `${cell.row},${cell.col}`}
      <Cell
        row={cell.row}
        col={cell.col}
        value={entries.get(key) || ""}
        isBlock={cell.isBlock}
        isSelected={!!selected && selected.row === cell.row && selected.col === cell.col}
        isInSelectedWord={highlighted.has(key)}
        isIncorrectFlash={incorrect.has(key)}
        isCorrectFlash={correct.has(key)}
        label={labels.get(key)}
        {onSelect}
      />
    {/each}
  {/each}
</div>

<style>
  .grid {
    display: grid;
    gap: 4px;
    margin: 16px 0;
  }
</style>
