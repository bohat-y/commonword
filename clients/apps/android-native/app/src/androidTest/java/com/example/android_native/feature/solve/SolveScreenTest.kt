package com.example.android_native.feature.solve

import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.test.onNodeWithText
import androidx.compose.ui.test.assertIsDisplayed
import androidx.compose.ui.test.performClick
import com.example.android_native.data.api.*
import com.example.android_native.data.repository.HomeRepository
import com.example.android_native.ui.theme.AndroidnativeTheme
import okhttp3.MediaType.Companion.toMediaTypeOrNull
import okhttp3.ResponseBody.Companion.toResponseBody
import org.junit.Rule
import org.junit.Test
import retrofit2.HttpException
import retrofit2.Response

class SolveScreenTest {

    @get:Rule
    val composeRule = createComposeRule()

    private fun fakePuzzleData(): PuzzlePublicDataDto {
        return PuzzlePublicDataDto(
            version = 1,
            width = 5,
            height = 5,
            blockCells = listOf(
                PuzzleBlockCellDto(0, 3),
                PuzzleBlockCellDto(1, 1),
                PuzzleBlockCellDto(2, 2),
                PuzzleBlockCellDto(3, 3),
                PuzzleBlockCellDto(4, 1)
            ),
            clues = PuzzleCluesDto(
                across = listOf(
                    PuzzleClueDto(1, "Morning drink"),
                    PuzzleClueDto(4, "Flying mammal")
                ),
                down = listOf(
                    PuzzleClueDto(1, "Musical beat"),
                    PuzzleClueDto(2, "Ocean movement")
                )
            ),
            wordIndex = PuzzleWordIndexDto(
                across = mapOf(
                    "1" to PuzzleWordIndexEntryDto(0, 0, 3),
                    "4" to PuzzleWordIndexEntryDto(2, 3, 2)
                ),
                down = mapOf(
                    "1" to PuzzleWordIndexEntryDto(0, 0, 5),
                    "2" to PuzzleWordIndexEntryDto(0, 2, 2)
                )
            ),
            meta = PuzzleMetaDto("Author", "Source")
        )
    }

    private fun fakeDetails(): SolveSessionDetailsDto {
        val data = fakePuzzleData()
        return SolveSessionDetailsDto(
            session = SolveSessionDto(
                id = "preview-session-id",
                puzzleId = "preview-puzzle-id",
                playerId = "preview-player",
                startedAt = "",
                updatedAt = "",
                completedAt = null
            ),
            puzzle = PuzzlePublicDto(
                id = "preview-puzzle-id",
                title = "Preview Crossword",
                isDaily = true,
                importedAt = "",
                data = data
            ),
            entries = listOf(
                EntryDto(0, 0, "C", ""),
                EntryDto(0, 1, "O", ""),
                EntryDto(0, 2, "F", "")
            )
        )
    }

    private class FakeApiSuccess(
        private val details: SolveSessionDetailsDto
    ) : CommonwordApi {

        override suspend fun getTodayPuzzle(): PuzzlePublicDto = details.puzzle

        override suspend fun startSession(request: StartSessionRequest): StartSessionResponse {
            return StartSessionResponse(details.session.id)
        }

        override suspend fun getSession(sessionId: String): SolveSessionDetailsDto = details

        override suspend fun setCell(
            sessionId: String,
            row: Int,
            col: Int,
            request: UpsertEntryRequest
        ): EntryDto {
            return EntryDto(
                row = row,
                col = col,
                value = request.value ?: "",
                updatedAt = ""
            )
        }

        override suspend fun checkWord(
            sessionId: String,
            request: CheckWordRequest
        ): CheckWordResponse {
            return CheckWordResponse(
                correct = true,
                complete = true,
                incorrectCells = emptyList()
            )
        }
    }

    private class FakeApiNotFound : CommonwordApi {
        private fun error(): Nothing {
            val body = "Not found".toResponseBody("text/plain".toMediaTypeOrNull())
            throw HttpException(Response.error<Any>(404, body))
        }

        override suspend fun getTodayPuzzle(): PuzzlePublicDto = error()
        override suspend fun startSession(request: StartSessionRequest): StartSessionResponse = error()
        override suspend fun getSession(sessionId: String): SolveSessionDetailsDto = error()
        override suspend fun setCell(sessionId: String, row: Int, col: Int, request: UpsertEntryRequest): EntryDto = error()
        override suspend fun checkWord(sessionId: String, request: CheckWordRequest): CheckWordResponse = error()
    }

    private class FakeApiError : CommonwordApi {
        private fun boom(): Nothing = throw RuntimeException("Network error")

        override suspend fun getTodayPuzzle(): PuzzlePublicDto = boom()
        override suspend fun startSession(request: StartSessionRequest): StartSessionResponse = boom()
        override suspend fun getSession(sessionId: String): SolveSessionDetailsDto = boom()
        override suspend fun setCell(sessionId: String, row: Int, col: Int, request: UpsertEntryRequest): EntryDto = boom()
        override suspend fun checkWord(sessionId: String, request: CheckWordRequest): CheckWordResponse = boom()
    }

    // -------------------------------------------------------------------------
    // TESTS
    // -------------------------------------------------------------------------

    @Test
    fun loadsContentOnSuccess() {
        val details = fakeDetails()
        val repo = HomeRepository(FakeApiSuccess(details))

        composeRule.setContent {
            AndroidnativeTheme {
                SolveScreen(
                    sessionId = details.session.id,
                    repository = repo,
                    onBack = {}
                )
            }
        }

        composeRule.onNodeWithText("Preview Crossword").assertIsDisplayed()
        composeRule.onNodeWithText("Session: ${details.session.id}").assertIsDisplayed()
    }

    @Test
    fun showsNotFoundError() {
        val repo = HomeRepository(FakeApiNotFound())

        composeRule.setContent {
            AndroidnativeTheme {
                SolveScreen(
                    sessionId = "missing",
                    repository = repo,
                    onBack = {}
                )
            }
        }

        composeRule.onNodeWithText("Session not found.").assertIsDisplayed()
        composeRule.onNodeWithText("Retry").assertIsDisplayed()
        composeRule.onNodeWithText("Back").assertIsDisplayed()
    }

    @Test
    fun showsGenericError() {
        val repo = HomeRepository(FakeApiError())

        composeRule.setContent {
            AndroidnativeTheme {
                SolveScreen(
                    sessionId = "error",
                    repository = repo,
                    onBack = {}
                )
            }
        }

        composeRule.onNodeWithText("Unable to load session.").assertIsDisplayed()
    }

    @Test
    fun backButtonCallsCallback() {
        val details = fakeDetails()
        val repo = HomeRepository(FakeApiSuccess(details))
        var backCalled = false

        composeRule.setContent {
            AndroidnativeTheme {
                SolveScreen(
                    sessionId = details.session.id,
                    repository = repo,
                    onBack = { backCalled = true }
                )
            }
        }

        composeRule.onNodeWithText("Back").performClick()

        assert(backCalled)
    }
}
