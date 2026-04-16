package com.example.android_native.feature.home

import androidx.lifecycle.ViewModel
import androidx.lifecycle.ViewModelProvider
import androidx.lifecycle.viewModelScope
import com.example.android_native.data.api.PuzzlePublicDto
import com.example.android_native.data.local.PlayerStore
import com.example.android_native.data.repository.HomeRepository
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import kotlinx.coroutines.withTimeoutOrNull

data class HomeUiState(
    val loading: Boolean = true,
    val puzzle: PuzzlePublicDto? = null,
    val resumeSessionId: String? = null,
    val errorMessage: String? = null,
    val startingSession: Boolean = false,
    val startedSessionId: String? = null
)

class HomeViewModel(
    private val repository: HomeRepository,
    private val playerStore: PlayerStore
) : ViewModel() {
    private val _uiState = MutableStateFlow(HomeUiState())
    val uiState: StateFlow<HomeUiState> = _uiState.asStateFlow()

    init {
        refresh()
    }

    fun refresh() {
        viewModelScope.launch {
            _uiState.value = HomeUiState(loading = true)

            try {
                val puzzle = repository.getTodayPuzzle()
                if (puzzle == null) {
                    _uiState.value =
                        HomeUiState(
                            loading = false,
                            errorMessage = "No daily puzzle yet. Check back later."
                        )
                } else {
                    var resumeSessionId: String? = null
                    val storedSessionId = playerStore.getSessionIdForPuzzle(puzzle.id)
                    if (!storedSessionId.isNullOrBlank()) {
                        try {
                            val valid =
                                withTimeoutOrNull(1200) {
                                    repository.sessionExists(storedSessionId)
                                }
                            if (valid == true) {
                                resumeSessionId = storedSessionId
                            } else {
                                if (valid == false) {
                                    playerStore.clearSessionIdForPuzzle(puzzle.id)
                                }
                            }
                        } catch (_: Exception) {
                            // Do not block puzzle load if session validation fails.
                        }
                    }

                    _uiState.value =
                        HomeUiState(
                            loading = false,
                            puzzle = puzzle,
                            resumeSessionId = resumeSessionId
                        )
                }
            } catch (_: Exception) {
                _uiState.value =
                    HomeUiState(loading = false, errorMessage = "Unable to reach the API.")
            }
        }
    }

    fun startSession() {
        val puzzle = _uiState.value.puzzle ?: return
        if (_uiState.value.startingSession) {
            return
        }

        viewModelScope.launch {
            _uiState.update { state ->
                state.copy(startingSession = true, startedSessionId = null, errorMessage = null)
            }

            try {
                val resumeSessionId = _uiState.value.resumeSessionId
                if (!resumeSessionId.isNullOrBlank()) {
                    _uiState.update { state ->
                        state.copy(startingSession = false, startedSessionId = resumeSessionId)
                    }
                    return@launch
                }

                val playerId = playerStore.getOrCreatePlayerId()
                val response = repository.startSession(puzzleId = puzzle.id, playerId = playerId)
                playerStore.setSessionIdForPuzzle(puzzle.id, response.id)
                _uiState.update { state ->
                    state.copy(
                        startingSession = false,
                        startedSessionId = response.id,
                        resumeSessionId = response.id
                    )
                }
            } catch (_: Exception) {
                _uiState.update { state ->
                    state.copy(
                        startingSession = false,
                        errorMessage = "Failed to start a solving session."
                    )
                }
            }
        }
    }

    fun consumeStartedSession() {
        _uiState.update { state ->
            state.copy(startedSessionId = null)
        }
    }
}

class HomeViewModelFactory(
    private val repository: HomeRepository,
    private val playerStore: PlayerStore
) : ViewModelProvider.Factory {
    override fun <T : ViewModel> create(modelClass: Class<T>): T {
        if (modelClass.isAssignableFrom(HomeViewModel::class.java)) {
            @Suppress("UNCHECKED_CAST")
            return HomeViewModel(repository, playerStore) as T
        }
        throw IllegalArgumentException("Unknown ViewModel class: ${modelClass.name}")
    }
}
