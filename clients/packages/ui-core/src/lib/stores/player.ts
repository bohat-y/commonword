import { readable } from "svelte/store";
import { getOrCreatePlayerId } from "../platform";

export const playerId = readable(getOrCreatePlayerId());
