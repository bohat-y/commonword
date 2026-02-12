<script lang="ts">
  export let row = 0;
  export let col = 0;
  export let value = "";
  export let isBlock = false;
  export let isSelected = false;
  export let isInSelectedWord = false;
  export let isIncorrectFlash = false;
  export let isCorrectFlash = false;
  export let label: number | undefined = undefined;
  export let onSelect: (row: number, col: number) => void;

  const handleClick = () => {
    if (!isBlock) {
      onSelect(row, col);
    }
  };
</script>

<button
  class={`cell ${isBlock ? "block" : ""} ${isInSelectedWord ? "word" : ""} ${
    isSelected ? "selected" : ""
  } ${isIncorrectFlash ? "incorrect" : ""} ${isCorrectFlash ? "correct" : ""}`}
  on:click={handleClick}
  disabled={isBlock}
  aria-label={`Row ${row + 1}, column ${col + 1}`}
>
  {#if !isBlock}
    {#if label !== undefined}
      <span class="label">{label}</span>
    {/if}
    <span class="value">{value}</span>
  {/if}
</button>

<style>
  .cell {
    aspect-ratio: 1 / 1;
    border: 1px solid var(--grid-border, #1f2937);
    background: var(--grid-cell-bg, #ffffff);
    font-weight: 600;
    font-size: var(--cell-font, 16px);
    display: grid;
    place-items: center;
    padding: 0;
    transition: background 120ms ease, border-color 120ms ease;
    position: relative;
  }

  .cell.word {
    background: var(--grid-word-bg, #fef3c7);
    border-color: var(--grid-word-border, #f59e0b);
  }

  .cell.block {
    background: var(--grid-block-bg, #111827);
    border-color: var(--grid-block-bg, #111827);
  }

  .cell.selected {
    outline: 3px solid var(--accent, #d97706);
  }

  .cell.incorrect {
    animation: flash-incorrect 0.8s ease-in-out 0s 2;
  }

  .cell.correct {
    animation: flash-correct 0.8s ease-in-out 0s 2;
  }

  @keyframes flash-incorrect {
    0% {
      background: #fecaca;
    }
    50% {
      background: #ffffff;
    }
    100% {
      background: #fecaca;
    }
  }

  @keyframes flash-correct {
    0% {
      background: #bbf7d0;
    }
    50% {
      background: #ffffff;
    }
    100% {
      background: #bbf7d0;
    }
  }

  .cell:disabled {
    cursor: default;
  }

  .label {
    position: absolute;
    top: 2px;
    left: 3px;
    font-size: calc(var(--cell-font, 16px) * 0.55);
    font-weight: 700;
    color: #64748b;
    line-height: 1;
  }

  .value {
    font-size: inherit;
  }
</style>
