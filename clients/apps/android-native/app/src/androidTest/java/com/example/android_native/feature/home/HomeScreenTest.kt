package com.example.android_native.feature.home

import androidx.compose.ui.test.assertIsDisplayed
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onNodeWithContentDescription
import androidx.compose.ui.test.onNodeWithText
import androidx.compose.ui.test.performClick
import androidx.test.ext.junit.runners.AndroidJUnit4
import com.example.android_native.data.api.PuzzleMetaDto
import com.example.android_native.data.api.PuzzlePublicDataDto
import com.example.android_native.data.api.PuzzlePublicDto
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith

@RunWith(AndroidJUnit4::class)
class HomeScreenTest {

    @get:Rule
    val composeRule = createComposeRule()

    private fun samplePuzzle(): PuzzlePublicDto {
        return PuzzlePublicDto(
            id = "test-id",
            title = "Daily Crossword",
            data = PuzzlePublicDataDto(
                width = 15,
                height = 15,
                meta = PuzzleMetaDto(
                    author = "CommonWord",
                    source = "UnitTest"
                )
            )
        )
    }

    @Test
    fun homeScreen_showsLoadingIndicator() {
        composeRule.setContent {
            HomeScreen(
                state = HomeUiState(loading = true),
                onRetry = {},
                onStartSession = {}
            )
        }

        composeRule
            .onNodeWithContentDescription("CircularProgressIndicator")
            .assertExists()
    }

    @Test
    fun homeScreen_showsPuzzleDetails() {
        composeRule.setContent {
            HomeScreen(
                state = HomeUiState(
                    loading = false,
                    puzzle = samplePuzzle()
                ),
                onRetry = {},
                onStartSession = {}
            )
        }

        composeRule.onNodeWithText("Daily Crossword").assertIsDisplayed()
        composeRule.onNodeWithText("By CommonWord").assertIsDisplayed()
        composeRule.onNodeWithText("Source: UnitTest").assertIsDisplayed()
        composeRule.onNodeWithText("15x15 grid").assertIsDisplayed()
    }

    @Test
    fun homeScreen_startButtonClickable() {
        var clicked = false

        composeRule.setContent {
            HomeScreen(
                state = HomeUiState(
                    loading = false,
                    puzzle = samplePuzzle()
                ),
                onRetry = {},
                onStartSession = { clicked = true }
            )
        }

        composeRule.onNodeWithText("Start solving").performClick()

        assert(clicked)
    }

    @Test
    fun homeScreen_showsResumeSession() {
        composeRule.setContent {
            HomeScreen(
                state = HomeUiState(
                    loading = false,
                    puzzle = samplePuzzle(),
                    resumeSessionId = "session-123"
                ),
                onRetry = {},
                onStartSession = {}
            )
        }

        composeRule.onNodeWithText("Saved session available.").assertIsDisplayed()
        composeRule.onNodeWithText("Resume solving").assertIsDisplayed()
    }

    @Test
    fun homeScreen_showsErrorMessage() {
        composeRule.setContent {
            HomeScreen(
                state = HomeUiState(
                    loading = false,
                    errorMessage = "Unable to reach the API."
                ),
                onRetry = {},
                onStartSession = {}
            )
        }

        composeRule.onNodeWithText("Unable to reach the API.").assertIsDisplayed()
        composeRule.onNodeWithText("Retry").assertIsDisplayed()
    }

    @Test
    fun homeScreen_showsEmptyState() {
        composeRule.setContent {
            HomeScreen(
                state = HomeUiState(
                    loading = false,
                    puzzle = null,
                    errorMessage = null
                ),
                onRetry = {},
                onStartSession = {}
            )
        }

        composeRule.onNodeWithText("No daily puzzle yet. Check back later.").assertIsDisplayed()
        composeRule.onNodeWithText("Retry").assertIsDisplayed()
    }
}
