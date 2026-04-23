package com.example.android_native.feature.home

import com.example.android_native.data.api.*
import com.example.android_native.data.local.PlayerStore
import com.example.android_native.data.repository.HomeRepository
import io.mockk.coEvery
import io.mockk.coVerify
import io.mockk.mockk
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.ExperimentalCoroutinesApi
import kotlinx.coroutines.test.StandardTestDispatcher
import kotlinx.coroutines.test.advanceUntilIdle
import kotlinx.coroutines.test.resetMain
import kotlinx.coroutines.test.runTest
import kotlinx.coroutines.test.setMain
import org.junit.After
import org.junit.Assert.*
import org.junit.Before
import org.junit.Test

@OptIn(ExperimentalCoroutinesApi::class)
class HomeViewModelTest {

    private lateinit var repository: HomeRepository
    private lateinit var playerStore: PlayerStore
    private lateinit var viewModel: HomeViewModel

    private val dispatcher = StandardTestDispatcher()

    private fun fakePuzzle(): PuzzlePublicDto {
        return PuzzlePublicDto(
            id = "puzzle123",
            title = "Daily Crossword",
            isDaily = true,
            importedAt = null,
            data = PuzzlePublicDataDto(
                version = 1,
                width = 5,
                height = 5,
                blockCells = emptyList(),
                clues = PuzzleCluesDto(),
                wordIndex = null,
                meta = PuzzleMetaDto(author = null, source = null)
            )
        )
    }

    @Before
    fun setup() {
        // CRITICAL: Replace Android Main dispatcher with test dispatcher
        Dispatchers.setMain(dispatcher)

        repository = mockk()
        playerStore = mockk()

        // Prevent real DataStore calls
        coEvery { playerStore.getSessionIdForPuzzle(any()) } returns null
        coEvery { playerStore.getOrCreatePlayerId() } returns "player1"
        coEvery { playerStore.setSessionIdForPuzzle(any(), any()) } returns Unit
        coEvery { playerStore.clearSessionIdForPuzzle(any()) } returns Unit
    }

    @After
    fun tearDown() {
        Dispatchers.resetMain()
    }

    @Test
    fun refreshLoadsPuzzleSuccessfully() = runTest(dispatcher) {
        val puzzle = fakePuzzle()
        coEvery { repository.getTodayPuzzle() } returns puzzle

        viewModel = HomeViewModel(repository, playerStore)
        advanceUntilIdle()

        val state = viewModel.uiState.value
        assertFalse(state.loading)
        assertEquals(puzzle, state.puzzle)
        assertNull(state.errorMessage)
    }

    @Test
    fun refreshHandlesNullPuzzle() = runTest(dispatcher) {
        coEvery { repository.getTodayPuzzle() } returns null

        viewModel = HomeViewModel(repository, playerStore)
        advanceUntilIdle()

        val state = viewModel.uiState.value
        assertFalse(state.loading)
        assertEquals("No daily puzzle yet. Check back later.", state.errorMessage)
        assertNull(state.puzzle)
    }

    @Test
    fun refreshHandlesApiError() = runTest(dispatcher) {
        coEvery { repository.getTodayPuzzle() } throws RuntimeException("API down")

        viewModel = HomeViewModel(repository, playerStore)
        advanceUntilIdle()

        val state = viewModel.uiState.value
        assertEquals("Unable to reach the API.", state.errorMessage)
        assertNull(state.puzzle)
    }

    @Test
    fun refreshResumesValidSession() = runTest(dispatcher) {
        val puzzle = fakePuzzle()
        coEvery { repository.getTodayPuzzle() } returns puzzle
        coEvery { playerStore.getSessionIdForPuzzle("puzzle123") } returns "sessionABC"
        coEvery { repository.sessionExists("sessionABC") } returns true

        viewModel = HomeViewModel(repository, playerStore)
        advanceUntilIdle()

        val state = viewModel.uiState.value
        assertEquals("sessionABC", state.resumeSessionId)
    }

    @Test
    fun refreshClearsInvalidSession() = runTest(dispatcher) {
        val puzzle = fakePuzzle()
        coEvery { repository.getTodayPuzzle() } returns puzzle
        coEvery { playerStore.getSessionIdForPuzzle("puzzle123") } returns "sessionABC"
        coEvery { repository.sessionExists("sessionABC") } returns false

        viewModel = HomeViewModel(repository, playerStore)
        advanceUntilIdle()

        coVerify { playerStore.clearSessionIdForPuzzle("puzzle123") }
    }

    @Test
    fun startSessionResumesExistingSession() = runTest(dispatcher) {
        val puzzle = fakePuzzle()
        coEvery { repository.getTodayPuzzle() } returns puzzle
        coEvery { playerStore.getSessionIdForPuzzle("puzzle123") } returns "sessionABC"
        coEvery { repository.sessionExists("sessionABC") } returns true

        viewModel = HomeViewModel(repository, playerStore)
        advanceUntilIdle()

        viewModel.startSession()
        advanceUntilIdle()

        val state = viewModel.uiState.value
        assertEquals("sessionABC", state.startedSessionId)
    }

    @Test
    fun startSessionCreatesNewSession() = runTest(dispatcher) {
        val puzzle = fakePuzzle()
        coEvery { repository.getTodayPuzzle() } returns puzzle
        coEvery { playerStore.getSessionIdForPuzzle("puzzle123") } returns null
        coEvery { repository.startSession("puzzle123", "player1") } returns StartSessionResponse("newSession")

        viewModel = HomeViewModel(repository, playerStore)
        advanceUntilIdle()

        viewModel.startSession()
        advanceUntilIdle()

        val state = viewModel.uiState.value
        assertEquals("newSession", state.startedSessionId)
        assertEquals("newSession", state.resumeSessionId)
    }

    @Test
    fun startSessionHandlesFailure() = runTest(dispatcher) {
        val puzzle = fakePuzzle()
        coEvery { repository.getTodayPuzzle() } returns puzzle
        coEvery { repository.startSession(any(), any()) } throws RuntimeException("fail")

        viewModel = HomeViewModel(repository, playerStore)
        advanceUntilIdle()

        viewModel.startSession()
        advanceUntilIdle()

        val state = viewModel.uiState.value
        assertEquals("Failed to start a solving session.", state.errorMessage)
        assertNull(state.startedSessionId)
    }

    @Test
    fun consumeStartedSessionClearsStartedSessionId() = runTest(dispatcher) {
        val puzzle = fakePuzzle()
        coEvery { repository.getTodayPuzzle() } returns puzzle
        coEvery { repository.startSession("puzzle123", "player1") } returns StartSessionResponse("newSession")

        viewModel = HomeViewModel(repository, playerStore)
        advanceUntilIdle()

        viewModel.startSession()
        advanceUntilIdle()

        viewModel.consumeStartedSession()

        val state = viewModel.uiState.value
        assertNull(state.startedSessionId)
    }
}
