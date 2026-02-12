<script lang="ts">
  import { onMount } from "svelte";
  import { getSession, getTodayPuzzle, startSession } from "../api/client";
  import type { PuzzlePublicDto } from "../api/types";
  import { getOrCreatePlayerId, track } from "../platform";
  import { navigateToSolve } from "../../app/router";

  let puzzle: PuzzlePublicDto | null = null;
  let error = "";
  let loading = true;

  const loadToday = async () => {
    loading = true;
    error = "";
    puzzle = null;

    try {
      puzzle = await getTodayPuzzle();
      if (!puzzle) {
        error = "No daily puzzle yet. Check back later.";
      }
    } catch {
      error = "Unable to reach the API.";
    } finally {
      loading = false;
    }
  };

  const handleStart = async () => {
    if (!puzzle) return;

    try {
      const storedKey = `commonword.session.${puzzle.id}`;
      const storedSession =
        typeof window !== "undefined" ? window.localStorage.getItem(storedKey) : null;

      if (storedSession) {
        try {
          await getSession(storedSession);
          await track("session_resumed", { puzzleId: puzzle.id }, storedSession);
          navigateToSolve(storedSession);
          return;
        } catch {
          window.localStorage.removeItem(storedKey);
        }
      }

      const session = await startSession({
        puzzleId: puzzle.id,
        playerId: getOrCreatePlayerId()
      });
      if (typeof window !== "undefined") {
        window.localStorage.setItem(storedKey, session.id);
      }
      await track("session_started", { puzzleId: puzzle.id }, session.id);
      navigateToSolve(session.id);
    } catch {
      error = "Failed to start a solving session.";
    }
  };

  onMount(loadToday);
</script>

<main class="shell">
  <header class="header">
    <h1>Commonword</h1>
    <p>Daily puzzle for focused solving sessions.</p>
  </header>

  {#if loading}
    <p>Loading today&apos;s puzzle...</p>
  {:else if error}
    <div class="empty">
      <p>{error}</p>
      <button on:click={loadToday}>Retry</button>
    </div>
  {:else if puzzle}
    <section class="card">
      <h2>{puzzle.title}</h2>
      {#if puzzle.data.meta?.author}
        <p class="meta">By {puzzle.data.meta.author}</p>
      {/if}
      {#if puzzle.data.meta?.source}
        <p class="meta">Source: {puzzle.data.meta.source}</p>
      {/if}
      <p class="meta">
        {puzzle.data.width}x{puzzle.data.height} grid
      </p>
      <button on:click={handleStart}>Start solving</button>
    </section>
  {/if}
</main>

<style>
  .shell {
    padding: 28px;
    max-width: 720px;
    margin: 0 auto;
  }

  .header h1 {
    margin: 0 0 6px 0;
  }

  .header p {
    margin: 0;
    color: var(--text-muted, #4b5563);
  }

  .card {
    margin-top: 24px;
    padding: 20px;
    border: 1px solid #e5e7eb;
    border-radius: 12px;
    background: #ffffff;
  }

  .meta {
    color: var(--text-muted, #6b7280);
    margin: 8px 0 0 0;
  }

  button {
    margin-top: 16px;
    padding: 10px 14px;
    border: none;
    background: var(--accent, #111827);
    color: #ffffff;
    border-radius: 8px;
    cursor: pointer;
  }

  .empty {
    margin-top: 24px;
    padding: 16px;
    border-radius: 10px;
    background: #f3f4f6;
    display: grid;
    gap: 12px;
  }
</style>
