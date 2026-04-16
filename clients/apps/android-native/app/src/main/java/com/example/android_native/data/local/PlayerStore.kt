package com.example.android_native.data.local

import android.content.Context
import androidx.datastore.preferences.core.edit
import androidx.datastore.preferences.core.stringPreferencesKey
import androidx.datastore.preferences.preferencesDataStore
import java.util.UUID
import kotlinx.coroutines.flow.first

private val Context.commonwordDataStore by preferencesDataStore(name = "commonword_prefs")

class PlayerStore(private val context: Context) {
    companion object {
        private val PLAYER_ID_KEY = stringPreferencesKey("player_id")
    }

    private fun sessionKeyForPuzzle(puzzleId: String) =
        stringPreferencesKey("session_${puzzleId.replace("-", "_")}")

    suspend fun getOrCreatePlayerId(): String {
        val current = context.commonwordDataStore.data.first()[PLAYER_ID_KEY]
        if (current != null) {
            return current
        }

        val newPlayerId = UUID.randomUUID().toString()
        context.commonwordDataStore.edit { prefs ->
            prefs[PLAYER_ID_KEY] = newPlayerId
        }
        return newPlayerId
    }

    suspend fun getSessionIdForPuzzle(puzzleId: String): String? =
        context.commonwordDataStore.data.first()[sessionKeyForPuzzle(puzzleId)]

    suspend fun setSessionIdForPuzzle(puzzleId: String, sessionId: String) {
        context.commonwordDataStore.edit { prefs ->
            prefs[sessionKeyForPuzzle(puzzleId)] = sessionId
        }
    }

    suspend fun clearSessionIdForPuzzle(puzzleId: String) {
        context.commonwordDataStore.edit { prefs ->
            prefs.remove(sessionKeyForPuzzle(puzzleId))
        }
    }
}
