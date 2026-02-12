import { defineConfig } from "vite";
import { svelte } from "@sveltejs/vite-plugin-svelte";

export default defineConfig({
  plugins: [svelte()],
  build: {
    lib: {
      entry: "src/app/App.svelte",
      name: "UiCore"
    },
    rollupOptions: {
      external: ["svelte"]
    }
  }
});
