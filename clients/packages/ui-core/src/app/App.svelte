<script lang="ts">
  import { onMount } from "svelte";
  import Home from "../lib/routes/Home.svelte";
  import Solve from "../lib/routes/Solve.svelte";
  import { initRouter, route } from "./router";

  onMount(() => initRouter());
</script>

<div class="app">
  {#if $route.name === "home"}
    <Home />
  {:else if $route.name === "solve"}
    {#key $route.sessionId}
      <Solve sessionId={$route.sessionId} />
    {/key}
  {/if}
</div>

<style>
  :global(body) {
    margin: 0;
    font-family: "Figtree", system-ui, sans-serif;
    background: #f8fafc;
    color: #0f172a;
  }

  .app {
    min-height: 100vh;
  }
</style>
