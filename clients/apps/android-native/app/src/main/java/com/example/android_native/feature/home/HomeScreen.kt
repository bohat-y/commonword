package com.example.android_native.feature.home

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.Button
import androidx.compose.material3.Card
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.tooling.preview.Preview
import androidx.compose.ui.unit.dp
import com.example.android_native.data.api.PuzzleMetaDto
import com.example.android_native.data.api.PuzzlePublicDataDto
import com.example.android_native.data.api.PuzzlePublicDto
import com.example.android_native.ui.theme.AndroidnativeTheme

@Composable
fun HomeScreen(
    state: HomeUiState,
    onRetry: () -> Unit,
    onStartSession: () -> Unit,
    modifier: Modifier = Modifier
) {
    Box(modifier = modifier.fillMaxSize()) {
        when {
            state.loading ->
                CircularProgressIndicator(modifier = Modifier.align(Alignment.Center))
            state.puzzle != null ->
                HomeContent(
                    state = state,
                    onStartSession = onStartSession,
                    modifier = Modifier.fillMaxSize()
                )
            else ->
                EmptyState(
                    message = state.errorMessage ?: "No daily puzzle yet. Check back later.",
                    onRetry = onRetry,
                    modifier = Modifier.align(Alignment.Center)
                )
        }
    }
}

@Composable
private fun HomeContent(
    state: HomeUiState,
    onStartSession: () -> Unit,
    modifier: Modifier = Modifier
) {
    val puzzle = state.puzzle ?: return
    Column(
        modifier = modifier.padding(24.dp),
        verticalArrangement = Arrangement.spacedBy(16.dp)
    ) {
        Text(text = "CommonWord", style = MaterialTheme.typography.headlineMedium)
        Text(
            text = "Daily puzzle for focused solving sessions.",
            style = MaterialTheme.typography.bodyMedium
        )

        Card(modifier = Modifier.fillMaxWidth()) {
            Column(
                modifier = Modifier.padding(16.dp),
                verticalArrangement = Arrangement.spacedBy(8.dp)
            ) {
                Text(text = puzzle.title, style = MaterialTheme.typography.titleLarge)
                val author = puzzle.data.meta?.author
                if (!author.isNullOrBlank()) {
                    Text(
                        text = "By $author",
                        style = MaterialTheme.typography.bodySmall
                    )
                }
                val source = puzzle.data.meta?.source
                if (!source.isNullOrBlank()) {
                    Text(
                        text = "Source: $source",
                        style = MaterialTheme.typography.bodySmall
                    )
                }
                Text(
                    text = "${puzzle.data.width}x${puzzle.data.height} grid",
                    style = MaterialTheme.typography.bodySmall
                )

                if (!state.resumeSessionId.isNullOrBlank()) {
                    Text(
                        text = "Saved session available.",
                        style = MaterialTheme.typography.bodySmall
                    )
                }

                Button(
                    onClick = onStartSession,
                    enabled = !state.startingSession,
                    contentPadding = PaddingValues(horizontal = 14.dp, vertical = 10.dp)
                ) {
                    val buttonText =
                        when {
                            state.startingSession && !state.resumeSessionId.isNullOrBlank() -> "Resuming..."
                            state.startingSession -> "Starting..."
                            !state.resumeSessionId.isNullOrBlank() -> "Resume solving"
                            else -> "Start solving"
                        }
                    Text(buttonText)
                }
            }
        }

        state.startedSessionId?.let { sessionId ->
            Text(
                text = "Session ready: $sessionId",
                style = MaterialTheme.typography.bodyMedium,
                fontWeight = FontWeight.SemiBold
            )
        }

        if (!state.errorMessage.isNullOrBlank()) {
            Text(
                text = state.errorMessage,
                style = MaterialTheme.typography.bodyMedium,
                color = MaterialTheme.colorScheme.error
            )
        }
    }
}

@Composable
private fun EmptyState(
    message: String,
    onRetry: () -> Unit,
    modifier: Modifier = Modifier
) {
    Column(
        modifier = modifier,
        horizontalAlignment = Alignment.CenterHorizontally,
        verticalArrangement = Arrangement.spacedBy(12.dp)
    ) {
        Text(text = message, style = MaterialTheme.typography.bodyMedium)
        Button(onClick = onRetry) {
            Text("Retry")
        }
    }
}

@Preview(showBackground = true)
@Composable
private fun HomeScreenPreview() {
    AndroidnativeTheme {
        HomeScreen(
            state =
                HomeUiState(
                    loading = false,
                    puzzle =
                        PuzzlePublicDto(
                            id = "sample-id",
                            title = "Daily Crossword",
                            data =
                                PuzzlePublicDataDto(
                                    width = 15,
                                    height = 15,
                                    meta = PuzzleMetaDto(author = "CommonWord", source = "Preview")
                                )
                        ),
                    resumeSessionId = "session-id"
                ),
            onRetry = {},
            onStartSession = {}
        )
    }
}

@Preview(showBackground = true)
@Composable
private fun HomeScreenLoadingPreview() {
    AndroidnativeTheme {
        HomeScreen(
            state = HomeUiState(loading = true),
            onRetry = {},
            onStartSession = {}
        )
    }
}

@Preview(showBackground = true)
@Composable
private fun HomeScreenErrorPreview() {
    AndroidnativeTheme {
        HomeScreen(
            state = HomeUiState(loading = false, errorMessage = "Unable to reach the API."),
            onRetry = {},
            onStartSession = {}
        )
    }
}
