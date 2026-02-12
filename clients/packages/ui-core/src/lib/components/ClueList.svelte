<script lang="ts">
  import type { PuzzleClues, WordDirection } from "../api/types";

  export type ActiveClue = { direction: WordDirection; number: number } | null;

  export let clues: PuzzleClues | null = null;
  export let active: ActiveClue = null;
  export let completed: Set<string> = new Set();
  export let onSelect: (direction: WordDirection, number: number) => void;

  const keyOf = (direction: WordDirection, number: number) => `${direction}:${number}`;
</script>

{#if clues}
  <div class="clues">
    <section>
      <h3>Across</h3>
      <ul>
        {#each clues.across as clue}
          <li>
            <button
              type="button"
              class:active={active?.direction === "across" && active?.number === clue.number}
              class:completed={completed.has(keyOf("across", clue.number))}
              on:click={() => onSelect("across", clue.number)}
            >
              <strong>{clue.number}.</strong> {clue.text}
            </button>
          </li>
        {/each}
      </ul>
    </section>
    <section>
      <h3>Down</h3>
      <ul>
        {#each clues.down as clue}
          <li>
            <button
              type="button"
              class:active={active?.direction === "down" && active?.number === clue.number}
              class:completed={completed.has(keyOf("down", clue.number))}
              on:click={() => onSelect("down", clue.number)}
            >
              <strong>{clue.number}.</strong> {clue.text}
            </button>
          </li>
        {/each}
      </ul>
    </section>
  </div>
{/if}

<style>
  .clues {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
    gap: 16px;
    margin-top: 24px;
  }

  h3 {
    margin: 0 0 8px 0;
    font-size: 14px;
    text-transform: uppercase;
    letter-spacing: 0.04em;
  }

  ul {
    list-style: none;
    padding: 0;
    margin: 0;
    display: grid;
    gap: 6px;
  }

  button {
    width: 100%;
    text-align: left;
    padding: 6px 8px;
    border-radius: 8px;
    border: 1px solid transparent;
    background: transparent;
    font-size: 14px;
    color: var(--text-muted, #4b5563);
    cursor: pointer;
  }

  button:hover {
    border-color: #d1d5db;
  }

  button.active {
    border-color: var(--accent, #d97706);
    background: #fff7ed;
    color: var(--text, #111827);
  }

  button.completed {
    opacity: 0.55;
    text-decoration: line-through;
  }

  strong {
    color: var(--text, #111827);
  }

  @media (min-width: 980px) {
    .clues {
      grid-template-columns: repeat(2, minmax(0, 1fr));
    }
  }
</style>
