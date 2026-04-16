package com.example.android_native.feature.solve

import android.annotation.SuppressLint
import androidx.activity.compose.BackHandler
import androidx.compose.animation.animateColorAsState
import androidx.compose.animation.core.animateFloatAsState
import androidx.compose.animation.core.tween
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.BoxWithConstraints
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.aspectRatio
import androidx.compose.foundation.layout.fillMaxHeight
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.text.BasicTextField
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material3.Button
import androidx.compose.material3.Card
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.mutableStateMapOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.runtime.setValue
import androidx.compose.runtime.snapshotFlow
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.alpha
import androidx.compose.ui.draw.clip
import androidx.compose.ui.focus.FocusRequester
import androidx.compose.ui.focus.focusRequester
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.input.key.Key
import androidx.compose.ui.input.key.KeyEventType
import androidx.compose.ui.input.key.isAltPressed
import androidx.compose.ui.input.key.isCtrlPressed
import androidx.compose.ui.input.key.isMetaPressed
import androidx.compose.ui.input.key.key
import androidx.compose.ui.input.key.onPreviewKeyEvent
import androidx.compose.ui.input.key.type
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.platform.LocalSoftwareKeyboardController
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.KeyboardCapitalization
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.text.style.TextDecoration
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.tooling.preview.Preview
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.android_native.data.api.EntryDto
import com.example.android_native.data.api.PuzzleBlockCellDto
import com.example.android_native.data.api.PuzzleClueDto
import com.example.android_native.data.api.PuzzleCluesDto
import com.example.android_native.data.api.PuzzleMetaDto
import com.example.android_native.data.api.PuzzlePublicDto
import com.example.android_native.data.api.PuzzlePublicDataDto
import com.example.android_native.data.api.PuzzleWordIndexDto
import com.example.android_native.data.api.PuzzleWordIndexEntryDto
import com.example.android_native.data.api.SolveSessionDetailsDto
import com.example.android_native.data.api.SolveSessionDto
import com.example.android_native.data.repository.HomeRepository
import com.example.android_native.ui.theme.AndroidnativeTheme
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.delay
import kotlinx.coroutines.FlowPreview
import kotlinx.coroutines.flow.debounce
import kotlinx.coroutines.flow.filter
import kotlinx.coroutines.launch
import kotlinx.coroutines.sync.Mutex
import kotlinx.coroutines.sync.withLock
import kotlinx.coroutines.withContext
import retrofit2.HttpException

private data class SolveUiState(
    val loading: Boolean = true,
    val details: SolveSessionDetailsDto? = null,
    val errorMessage: String? = null
)

private data class CellPosition(val row: Int, val col: Int)

private enum class WordDirection {
    Across,
    Down
}

private data class ClueSelection(
    val direction: WordDirection,
    val number: Int
)

private data class PendingCellUpdate(
    val row: Int,
    val col: Int,
    val value: String
)

private const val ENABLE_CELL_SYNC = true

@Composable
fun SolveScreen(
    sessionId: String,
    repository: HomeRepository,
    onBack: () -> Unit,
    modifier: Modifier = Modifier
) {
    var uiState by remember(sessionId) { mutableStateOf(SolveUiState(loading = true)) }

    fun reload() {
        uiState = SolveUiState(loading = true)
    }

    LaunchedEffect(sessionId, uiState.loading) {
        if (!uiState.loading) return@LaunchedEffect

        uiState =
            try {
                val details = repository.getSessionDetails(sessionId)
                SolveUiState(loading = false, details = details)
            } catch (err: HttpException) {
                val message =
                    if (err.code() == 404) {
                        "Session not found."
                    } else {
                        "Unable to load session."
                    }
                SolveUiState(loading = false, errorMessage = message)
            } catch (_: Exception) {
                SolveUiState(loading = false, errorMessage = "Unable to load session.")
            }
    }

    BackHandler(onBack = onBack)

    Box(modifier = modifier.fillMaxSize()) {
        when {
            uiState.loading ->
                CircularProgressIndicator(modifier = Modifier.align(Alignment.Center))
            uiState.details != null ->
                SolveContent(
                    details = uiState.details!!,
                    repository = repository,
                    onBack = onBack,
                    modifier = Modifier.fillMaxSize()
                )
            else ->
                SolveError(
                    message = uiState.errorMessage ?: "Unable to load session.",
                    onRetry = ::reload,
                    onBack = onBack,
                    modifier = Modifier.align(Alignment.Center)
                )
        }
    }
}

@Composable
@OptIn(FlowPreview::class)
private fun SolveContent(
    details: SolveSessionDetailsDto,
    repository: HomeRepository?,
    onBack: () -> Unit,
    modifier: Modifier = Modifier
) {
    val data = details.puzzle.data
    val blockSet = remember(data.blockCells) { data.blockCells.map { keyOf(it.row, it.col) }.toSet() }
    val labelMap = remember(data.wordIndex) { buildLabelMap(data.wordIndex) }
    val entries = remember(details.session.id) {
        mutableStateMapOf<String, String>().apply {
            putAll(buildEntriesMap(details.entries))
        }
    }
    val pendingSync = remember(details.session.id) { mutableStateMapOf<String, PendingCellUpdate>() }
    var pendingRevision by remember(details.session.id) { mutableIntStateOf(0) }
    var syncErrorMessage by remember(details.session.id) { mutableStateOf<String?>(null) }
    var statusMessage by remember(details.session.id) { mutableStateOf("") }
    var checkingWord by remember(details.session.id) { mutableStateOf(false) }
    var inputValue by remember(details.session.id) { mutableStateOf("") }
    var incorrectFlash by remember(details.session.id) { mutableStateOf<Set<String>>(emptySet()) }
    var correctFlash by remember(details.session.id) { mutableStateOf<Set<String>>(emptySet()) }
    var solvedClues by remember(details.session.id) { mutableStateOf<Set<String>>(emptySet()) }
    var incorrectFlashRevision by remember(details.session.id) { mutableIntStateOf(0) }
    var correctFlashRevision by remember(details.session.id) { mutableIntStateOf(0) }
    val pendingAutoChecks = remember(details.session.id) { mutableStateMapOf<String, ClueSelection>() }
    val checkMutex = remember { Mutex() }
    val focusRequester = remember { FocusRequester() }
    val keyboardController = LocalSoftwareKeyboardController.current
    val coroutineScope = rememberCoroutineScope()

    var selectedCell by remember(details.session.id) {
        mutableStateOf(findFirstOpenCell(data.width, data.height, blockSet))
    }
    var activeDirection by remember(details.session.id) { mutableStateOf(WordDirection.Across) }
    var selectedClue by remember(details.session.id) { mutableStateOf<ClueSelection?>(null) }

    fun focusCellInput(showKeyboard: Boolean = false) {
        focusRequester.requestFocus()
        if (showKeyboard) {
            keyboardController?.show()
        }
    }

    fun clueKey(direction: WordDirection, number: Int): String =
        "${direction.toApiDirection()}:$number"

    fun clueKey(clue: ClueSelection): String = clueKey(clue.direction, clue.number)

    fun markSolved(clue: ClueSelection, solved: Boolean) {
        val key = clueKey(clue)
        solvedClues =
            if (solved) {
                solvedClues + key
            } else {
                solvedClues - key
            }
    }

    fun updateSelectedClue(row: Int, col: Int) {
        val primary = resolveClueForCell(row, col, activeDirection, data, blockSet)
        if (primary != null) {
            selectedClue = primary
            return
        }

        val fallbackDirection = toggleDirection(activeDirection)
        val fallback = resolveClueForCell(row, col, fallbackDirection, data, blockSet)
        if (fallback != null) {
            activeDirection = fallbackDirection
            selectedClue = fallback
            return
        }

        selectedClue = null
    }

    fun isWordComplete(clue: ClueSelection): Boolean {
        val entry = getWordEntry(clue, data.wordIndex) ?: return false
        for (index in 0 until entry.length) {
            val row = entry.row + if (clue.direction == WordDirection.Down) index else 0
            val col = entry.col + if (clue.direction == WordDirection.Across) index else 0
            val value = entries[keyOf(row, col)].orEmpty()
            if (value.length != 1 || value[0] !in 'A'..'Z') {
                return false
            }
        }
        return true
    }

    fun clearSolvedForCell(row: Int, col: Int) {
        val across = resolveClueForCell(row, col, WordDirection.Across, data, blockSet)
        val down = resolveClueForCell(row, col, WordDirection.Down, data, blockSet)
        if (across != null) {
            markSolved(across, solved = false)
        }
        if (down != null) {
            markSolved(down, solved = false)
        }
    }

    fun updateAutoCheckCandidates(row: Int, col: Int) {
        val across = resolveClueForCell(row, col, WordDirection.Across, data, blockSet)
        val down = resolveClueForCell(row, col, WordDirection.Down, data, blockSet)
        for (candidate in listOfNotNull(across, down)) {
            val key = clueKey(candidate)
            if (isWordComplete(candidate)) {
                pendingAutoChecks[key] = candidate
            } else {
                pendingAutoChecks.remove(key)
            }
        }
    }

    fun setCellValue(row: Int, col: Int, rawValue: String) {
        if (blockSet.contains(keyOf(row, col))) return

        val normalized = rawValue.uppercase().filter { it in 'A'..'Z' }.takeLast(1)
        val key = keyOf(row, col)
        val current = entries[key].orEmpty()
        if (current == normalized) return

        if (normalized.isEmpty()) {
            entries.remove(key)
        } else {
            entries[key] = normalized
        }

        clearSolvedForCell(row, col)
        updateAutoCheckCandidates(row, col)
        pendingSync[key] = PendingCellUpdate(row = row, col = col, value = normalized)
        pendingRevision += 1
        syncErrorMessage = null
        incorrectFlash = incorrectFlash - key
        correctFlash = correctFlash - key
    }

    fun selectCell(row: Int, col: Int, requestKeyboard: Boolean = true) {
        if (blockSet.contains(keyOf(row, col))) return

        if (selectedCell?.row == row && selectedCell?.col == col) {
            val toggled = toggleDirection(activeDirection)
            val toggledClue = resolveClueForCell(row, col, toggled, data, blockSet)
            if (toggledClue != null) {
                activeDirection = toggled
                selectedClue = toggledClue
                if (requestKeyboard) {
                    focusCellInput(showKeyboard = true)
                }
                return
            }
        }

        selectedCell = CellPosition(row, col)
        updateSelectedClue(row, col)
        if (requestKeyboard) {
            focusCellInput(showKeyboard = true)
        }
    }

    fun moveSelection(deltaRow: Int, deltaCol: Int) {
        val current = selectedCell ?: return

        var row = current.row + deltaRow
        var col = current.col + deltaCol

        while (row in 0 until data.height && col in 0 until data.width) {
            if (!blockSet.contains(keyOf(row, col))) {
                selectCell(row, col, requestKeyboard = false)
                return
            }
            row += deltaRow
            col += deltaCol
        }
    }

    fun applyValue(value: String, advance: Boolean = false) {
        val current = selectedCell ?: return
        val key = keyOf(current.row, current.col)
        val currentValue = entries[key].orEmpty()
        val normalized = value.trim().uppercase().filter { it in 'A'..'Z' }.takeLast(1)

        if (normalized.isEmpty() && currentValue.isEmpty()) {
            inputValue = ""
            return
        }

        setCellValue(current.row, current.col, normalized)
        inputValue = normalized

        if (advance && normalized.isNotEmpty()) {
            if (activeDirection == WordDirection.Down) {
                moveSelection(1, 0)
            } else {
                moveSelection(0, 1)
            }
        }
    }

    fun handleDelete() {
        val current = selectedCell ?: return
        val currentValue = entries[keyOf(current.row, current.col)].orEmpty()
        if (currentValue.isNotEmpty()) {
            inputValue = ""
            applyValue("")
            return
        }

        val previous =
            findPreviousCell(
                row = current.row,
                col = current.col,
                direction = activeDirection,
                width = data.width,
                height = data.height,
                blockSet = blockSet
            ) ?: return

        selectCell(previous.row, previous.col, requestKeyboard = false)
        val previousValue = entries[keyOf(previous.row, previous.col)].orEmpty()
        if (previousValue.isNotEmpty()) {
            inputValue = ""
            setCellValue(previous.row, previous.col, "")
        }
    }

    fun triggerIncorrectFlash(cells: Set<String>) {
        incorrectFlash = cells
        incorrectFlashRevision += 1
    }

    fun triggerCorrectFlash(clue: ClueSelection) {
        correctFlash = buildHighlighted(clue, data.wordIndex)
        correctFlashRevision += 1
    }

    fun triggerCorrectFlashCells(cells: Set<String>) {
        if (cells.isEmpty()) return
        correctFlash = cells
        correctFlashRevision += 1
    }

    suspend fun runCheckWordInternal(clue: ClueSelection, showStatus: Boolean) {
        val repo = repository ?: return
        checkMutex.withLock {
            if (showStatus) {
                statusMessage = ""
            }

            try {
                val result =
                    withContext(Dispatchers.IO) {
                        repo.checkWord(
                            sessionId = details.session.id,
                            direction = clue.direction.toApiDirection(),
                            number = clue.number
                        )
                    }

                val incorrectKeys =
                    result.incorrectCells
                        ?.map { keyOf(it.row, it.col) }
                        ?.toSet()
                        .orEmpty()

                if (incorrectKeys.isNotEmpty()) {
                    triggerIncorrectFlash(incorrectKeys)
                }

                val clueCells = buildHighlighted(clue, data.wordIndex)
                val correctCells =
                    clueCells.filter { key ->
                        entries[key].orEmpty().isNotBlank() && !incorrectKeys.contains(key)
                    }.toSet()

                if (result.correct) {
                    markSolved(clue, solved = true)
                    triggerCorrectFlash(clue)
                } else {
                    markSolved(clue, solved = false)
                    triggerCorrectFlashCells(correctCells)
                }

                if (showStatus) {
                    statusMessage =
                        when {
                            result.correct -> "Correct word."
                            !result.complete -> "Word is incomplete."
                            else -> "Some letters are incorrect."
                        }
                }
            } catch (_: Exception) {
                if (showStatus) {
                    statusMessage = "Unable to check word."
                }
            }
        }
    }

    fun runCheckWord() {
        val clue = selectedClue ?: return
        if (checkingWord) return

        checkingWord = true
        coroutineScope.launch {
            try {
                runCheckWordInternal(clue, showStatus = true)
            } finally {
                checkingWord = false
            }
        }
    }

    fun selectClue(direction: WordDirection, number: Int) {
        val clue = ClueSelection(direction, number)
        activeDirection = direction
        selectedClue = clue
        val entry = getWordEntry(clue, data.wordIndex) ?: return
        selectedCell = CellPosition(entry.row, entry.col)
        focusCellInput(showKeyboard = true)
    }

    LaunchedEffect(details.session.id) {
        val initial = selectedCell ?: findFirstOpenCell(data.width, data.height, blockSet)
        if (initial != null) {
            selectedCell = initial
            updateSelectedClue(initial.row, initial.col)
            focusCellInput(showKeyboard = true)
        }
    }

    val highlightedCells = remember(selectedClue, data.wordIndex) { buildHighlighted(selectedClue, data.wordIndex) }
    val activeClueText = selectedClue?.let { clueText(it, data.clues) }.orEmpty()
    val selectedKey = selectedCell?.let { keyOf(it.row, it.col) }
    val selectedValue = selectedKey?.let { entries[it].orEmpty() }.orEmpty()

    LaunchedEffect(selectedCell?.row, selectedCell?.col, selectedValue) {
        inputValue = selectedValue
    }

    LaunchedEffect(details.session.id) {
        val repo = repository ?: return@LaunchedEffect
        snapshotFlow { pendingRevision }
            .filter { it > 0 }
            .debounce(220)
            .collect {
                if (!ENABLE_CELL_SYNC) {
                    pendingSync.clear()
                    pendingAutoChecks.clear()
                    syncErrorMessage = null
                    return@collect
                }

                if (pendingSync.isEmpty()) return@collect
                val snapshot = pendingSync.values.toList()
                pendingSync.clear()

                val failed =
                    withContext(Dispatchers.IO) {
                        var hasFailure = false
                        for (update in snapshot) {
                            try {
                                repo.setCell(
                                    sessionId = details.session.id,
                                    row = update.row,
                                    col = update.col,
                                    value = update.value.ifBlank { null }
                                )
                            } catch (_: Exception) {
                                hasFailure = true
                            }
                        }
                        hasFailure
                    }

                if (failed) {
                    syncErrorMessage = "Failed to sync cell update."
                    return@collect
                }

                val autoChecks = pendingAutoChecks.values.toList()
                pendingAutoChecks.clear()
                for (clue in autoChecks) {
                    runCheckWordInternal(clue, showStatus = false)
                }
            }
    }

    LaunchedEffect(incorrectFlashRevision) {
        if (incorrectFlashRevision <= 0 || incorrectFlash.isEmpty()) return@LaunchedEffect
        delay(1200)
        incorrectFlash = emptySet()
    }

    LaunchedEffect(correctFlashRevision) {
        if (correctFlashRevision <= 0 || correctFlash.isEmpty()) return@LaunchedEffect
        delay(1200)
        correctFlash = emptySet()
    }

    LaunchedEffect(statusMessage) {
        if (statusMessage.isBlank()) return@LaunchedEffect
        delay(1600)
        statusMessage = ""
    }

    Box(
        modifier =
            modifier
                .fillMaxSize()
                .onPreviewKeyEvent { event ->
                    if (event.type != KeyEventType.KeyDown) {
                        return@onPreviewKeyEvent false
                    }
                    if (event.isAltPressed || event.isCtrlPressed || event.isMetaPressed) {
                        return@onPreviewKeyEvent false
                    }
                    if (selectedCell == null) {
                        return@onPreviewKeyEvent false
                    }

                    when (event.key) {
                        Key.DirectionUp -> {
                            moveSelection(-1, 0)
                            true
                        }
                        Key.DirectionDown -> {
                            moveSelection(1, 0)
                            true
                        }
                        Key.DirectionLeft -> {
                            moveSelection(0, -1)
                            true
                        }
                        Key.DirectionRight -> {
                            moveSelection(0, 1)
                            true
                        }
                        Key.Backspace, Key.Delete -> {
                            handleDelete()
                            true
                        }
                        else -> {
                            val unicode = event.nativeKeyEvent.unicodeChar
                            if (unicode == 0) {
                                false
                            } else {
                                val char = unicode.toChar()
                                if (char.isLetter()) {
                                    applyValue(char.toString(), advance = true)
                                    true
                                } else {
                                    false
                                }
                            }
                        }
                    }
                }
    ) {
        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(16.dp),
            verticalArrangement = Arrangement.spacedBy(12.dp)
        ) {
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.spacedBy(10.dp),
                verticalAlignment = Alignment.CenterVertically
            ) {
                Button(onClick = onBack) {
                    Text("Back")
                }
                Text(
                    text = details.puzzle.title,
                    style = MaterialTheme.typography.titleLarge,
                    maxLines = 1,
                    overflow = TextOverflow.Ellipsis
                )
            }

            Text(
                text = "Session: ${details.session.id}",
                style = MaterialTheme.typography.bodySmall
            )

            CrosswordGrid(
                data = data,
                blockSet = blockSet,
                entries = entries,
                selectedCell = selectedCell,
                highlighted = highlightedCells,
                incorrect = incorrectFlash,
                correct = correctFlash,
                labels = labelMap,
                onSelect = ::selectCell
            )

            Card(modifier = Modifier.fillMaxWidth()) {
                val clueLabel =
                    selectedClue?.let { clue ->
                        "$activeClueText - ${clue.number} ${clue.direction.name.uppercase()}"
                    } ?: "Select a cell to view clue."

                Column(
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(horizontal = 12.dp, vertical = 10.dp),
                    verticalArrangement = Arrangement.spacedBy(10.dp)
                ) {
                    Text(
                        text = clueLabel,
                        style = MaterialTheme.typography.bodyMedium,
                        fontWeight = FontWeight.SemiBold
                    )

                    Row(
                        horizontalArrangement = Arrangement.spacedBy(8.dp),
                        verticalAlignment = Alignment.CenterVertically
                    ) {
                        Button(
                            onClick = {
                                inputValue = ""
                                applyValue("")
                            },
                            enabled = selectedCell != null
                        ) {
                            Text("Clear cell")
                        }
                        Button(
                            onClick = ::runCheckWord,
                            enabled = selectedClue != null && !checkingWord && repository != null
                        ) {
                            Text(if (checkingWord) "Checking..." else "Check word")
                        }
                    }

                    if (statusMessage.isNotBlank()) {
                        Text(
                            text = statusMessage,
                            style = MaterialTheme.typography.bodySmall
                        )
                    }
                }
            }

            if (!syncErrorMessage.isNullOrBlank()) {
                Text(
                    text = syncErrorMessage.orEmpty(),
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.error
                )
            }

            Row(
                modifier = Modifier
                    .fillMaxWidth()
                    .weight(1f),
                horizontalArrangement = Arrangement.spacedBy(10.dp)
            ) {
                ClueList(
                    title = "Across",
                    clues = data.clues.across,
                    direction = WordDirection.Across,
                    active = selectedClue,
                    secondaryNumber =
                        if (selectedClue?.direction == WordDirection.Down) selectedClue?.number else null,
                    completed = solvedClues,
                    onSelect = { number -> selectClue(WordDirection.Across, number) },
                    modifier = Modifier.weight(1f)
                )
                ClueList(
                    title = "Down",
                    clues = data.clues.down,
                    direction = WordDirection.Down,
                    active = selectedClue,
                    secondaryNumber =
                        if (selectedClue?.direction == WordDirection.Across) selectedClue?.number else null,
                    completed = solvedClues,
                    onSelect = { number -> selectClue(WordDirection.Down, number) },
                    modifier = Modifier.weight(1f)
                )
            }
        }

        BasicTextField(
            value = inputValue,
            onValueChange = { newValue ->
                val cleaned = newValue.uppercase().filter { it in 'A'..'Z' }
                val value = cleaned.takeLast(1)
                if (value.isNotEmpty()) {
                    inputValue = value
                    applyValue(value, advance = true)
                    return@BasicTextField
                }

                inputValue = ""
                val selected = selectedCell ?: return@BasicTextField
                val currentValue = entries[keyOf(selected.row, selected.col)].orEmpty()
                if (newValue.isEmpty() && currentValue.isNotEmpty()) {
                    applyValue("")
                }
            },
            modifier = Modifier
                .align(Alignment.TopStart)
                .focusRequester(focusRequester)
                .size(1.dp)
                .alpha(0.01f),
            keyboardOptions =
                KeyboardOptions(
                    capitalization = KeyboardCapitalization.Characters,
                    keyboardType = KeyboardType.Ascii,
                    autoCorrectEnabled = false
                ),
            singleLine = true
        )
    }
}

@SuppressLint("UnusedBoxWithConstraintsScope")
@Composable
private fun CrosswordGrid(
    data: PuzzlePublicDataDto,
    blockSet: Set<String>,
    entries: Map<String, String>,
    selectedCell: CellPosition?,
    highlighted: Set<String>,
    incorrect: Set<String>,
    correct: Set<String>,
    labels: Map<String, Int>,
    onSelect: (row: Int, col: Int) -> Unit
) {
    val width = if (data.width <= 0) 1 else data.width
    val height = if (data.height <= 0) 1 else data.height
    val gap = 2.dp

    BoxWithConstraints(
        modifier = Modifier
            .fillMaxWidth()
            .aspectRatio(width.toFloat() / height.toFloat())
    ) {
        val hGaps = gap * (width - 1).toFloat()
        val vGaps = gap * (height - 1).toFloat()
        val usableWidth = (maxWidth - hGaps).coerceAtLeast(0.dp)
        val usableHeight = (maxHeight - vGaps).coerceAtLeast(0.dp)
        val cellSize =
            minOf(
                usableWidth / width.toFloat(),
                usableHeight / height.toFloat()
            ).coerceAtLeast(8.dp)
        val density = LocalDensity.current
        val valueFont = with(density) { (cellSize * 0.52f).coerceIn(11.dp, 26.dp).toSp() }
        val labelFont = with(density) { (cellSize * 0.26f).coerceIn(7.dp, 11.dp).toSp() }
        val labelPadStart = (cellSize * 0.09f).coerceIn(1.dp, 3.dp)
        val labelPadTop = (cellSize * 0.06f).coerceIn(1.dp, 2.dp)

        Column(
            modifier = Modifier.fillMaxSize(),
            verticalArrangement = Arrangement.spacedBy(gap)
        ) {
            for (row in 0 until height) {
                Row(
                    modifier = Modifier
                        .weight(1f)
                        .fillMaxWidth(),
                    horizontalArrangement = Arrangement.spacedBy(gap)
                ) {
                    for (col in 0 until width) {
                        val cellKey = keyOf(row, col)
                        val isBlock = blockSet.contains(cellKey)
                        val isSelected = selectedCell?.row == row && selectedCell?.col == col
                        val isHighlighted = highlighted.contains(cellKey)
                        val isIncorrectFlash = incorrect.contains(cellKey)
                        val isCorrectFlash = correct.contains(cellKey)
                        val baseBackground =
                            when {
                                isBlock -> Color(0xFF111827)
                                isSelected -> Color(0xFFFFE0B2)
                                isHighlighted -> Color(0xFFFFF3E0)
                                else -> Color.White
                            }
                        val flashColor by
                            animateColorAsState(
                                targetValue =
                                    when {
                                        isIncorrectFlash -> Color(0xFFFECACA)
                                        isCorrectFlash -> Color(0xFFBBF7D0)
                                        else -> Color(0x00FFFFFF) // transparent white, not black
                                    },
                                animationSpec = tween(durationMillis = 220),
                                label = "cellFlashColor"
                            )
                        val border =
                            when {
                                isSelected -> Color(0xFFFB8C00)
                                isHighlighted -> Color(0xFFF59E0B)
                                else -> Color(0xFF1F2937)
                            }

                        Box(
                            modifier =
                                Modifier
                                    .weight(1f)
                                    .fillMaxHeight()
                                    .clip(RoundedCornerShape(5.dp))
                                    .border(1.dp, border, RoundedCornerShape(5.dp))
                                    .background(baseBackground, RoundedCornerShape(5.dp))
                                    .clickable(enabled = !isBlock) { onSelect(row, col) },
                            contentAlignment = Alignment.Center
                        ) {
                            if (flashColor.alpha > 0f) {
                                Box(
                                    modifier =
                                        Modifier
                                            .matchParentSize()
                                            .background(flashColor, RoundedCornerShape(5.dp))
                                )
                            }

                            if (!isBlock) {
                                val label = labels[cellKey]
                                if (label != null) {
                                    Text(
                                        text = label.toString(),
                                        fontSize = labelFont,
                                        color = Color(0xFF64748B),
                                        modifier = Modifier
                                            .align(Alignment.TopStart)
                                            .padding(start = labelPadStart, top = labelPadTop)
                                    )
                                }

                                val value = entries[cellKey].orEmpty()
                                Text(
                                    text = value,
                                    fontSize = valueFont,
                                    lineHeight = valueFont,
                                    fontWeight = FontWeight.Bold,
                                    color = Color(0xFF111827)
                                )
                            }
                        }
                    }
                }
            }
        }
    }
}

@Composable
private fun ClueList(
    title: String,
    clues: List<PuzzleClueDto>,
    direction: WordDirection,
    active: ClueSelection?,
    secondaryNumber: Int?,
    completed: Set<String>,
    onSelect: (number: Int) -> Unit,
    modifier: Modifier = Modifier
) {
    Card(modifier = modifier.fillMaxHeight()) {
        Column(
            modifier = Modifier
                .fillMaxWidth()
                .fillMaxHeight()
                .padding(10.dp)
        ) {
            Text(
                text = title,
                style = MaterialTheme.typography.titleMedium
            )
            LazyColumn(
                modifier = Modifier
                    .fillMaxWidth()
                    .weight(1f),
                verticalArrangement = Arrangement.spacedBy(2.dp)
            ) {
                items(items = clues, key = { clue -> clue.number }) { clue ->
                    val isActive = active?.direction == direction && active.number == clue.number
                    val isSecondary = !isActive && secondaryNumber == clue.number
                    val isCompleted = completed.contains("${direction.toApiDirection()}:${clue.number}")
                    val background =
                        when {
                            isActive -> MaterialTheme.colorScheme.secondaryContainer
                            isSecondary -> MaterialTheme.colorScheme.secondaryContainer.copy(alpha = 0.35f)
                            else -> Color.Transparent
                        }
                    val alpha = if (isCompleted) 0.58f else 1f
                    val decoration = if (isCompleted) TextDecoration.LineThrough else TextDecoration.None

                    Row(
                        modifier =
                            Modifier
                                .fillMaxWidth()
                                .background(background)
                                .alpha(alpha)
                                .clickable { onSelect(clue.number) }
                                .padding(horizontal = 8.dp, vertical = 6.dp),
                        horizontalArrangement = Arrangement.spacedBy(6.dp)
                    ) {
                        Text(
                            text = "${clue.number}.",
                            style = MaterialTheme.typography.bodySmall,
                            fontWeight = FontWeight.SemiBold
                        )
                        Text(
                            text = clue.text,
                            style = MaterialTheme.typography.bodySmall,
                            textDecoration = decoration,
                            maxLines = 2,
                            overflow = TextOverflow.Ellipsis
                        )
                    }
                }
            }
        }
    }
}

@Composable
private fun SolveError(
    message: String,
    onRetry: () -> Unit,
    onBack: () -> Unit,
    modifier: Modifier = Modifier
) {
    Column(
        modifier = modifier,
        horizontalAlignment = Alignment.CenterHorizontally,
        verticalArrangement = Arrangement.spacedBy(12.dp)
    ) {
        Text(text = message, style = MaterialTheme.typography.bodyMedium)
        Row(horizontalArrangement = Arrangement.spacedBy(10.dp)) {
            Button(onClick = onRetry) {
                Text("Retry")
            }
            Button(onClick = onBack) {
                Text("Back")
            }
        }
    }
}

private fun keyOf(row: Int, col: Int): String = "$row,$col"

private fun buildEntriesMap(entries: List<EntryDto>): Map<String, String> =
    buildMap {
        for (entry in entries) {
            val normalized = entry.value.trim().uppercase()
            if (normalized.isNotEmpty()) {
                put(keyOf(entry.row, entry.col), normalized.take(1))
            }
        }
    }

private fun buildLabelMap(wordIndex: PuzzleWordIndexDto?): Map<String, Int> {
    if (wordIndex == null) return emptyMap()

    val labels = mutableMapOf<String, Int>()

    for ((numberKey, entry) in wordIndex.across) {
        val number = numberKey.toIntOrNull() ?: continue
        val key = keyOf(entry.row, entry.col)
        val current = labels[key]
        if (current == null || number < current) {
            labels[key] = number
        }
    }

    for ((numberKey, entry) in wordIndex.down) {
        val number = numberKey.toIntOrNull() ?: continue
        val key = keyOf(entry.row, entry.col)
        val current = labels[key]
        if (current == null || number < current) {
            labels[key] = number
        }
    }

    return labels
}

private fun findFirstOpenCell(width: Int, height: Int, blockSet: Set<String>): CellPosition? {
    for (row in 0 until height) {
        for (col in 0 until width) {
            if (!blockSet.contains(keyOf(row, col))) {
                return CellPosition(row, col)
            }
        }
    }
    return null
}

private fun toggleDirection(direction: WordDirection): WordDirection =
    if (direction == WordDirection.Across) WordDirection.Down else WordDirection.Across

private fun WordDirection.toApiDirection(): String =
    if (this == WordDirection.Across) "across" else "down"

private fun findPreviousCell(
    row: Int,
    col: Int,
    direction: WordDirection,
    width: Int,
    height: Int,
    blockSet: Set<String>
): CellPosition? {
    var currentRow = row + if (direction == WordDirection.Down) -1 else 0
    var currentCol = col + if (direction == WordDirection.Across) -1 else 0

    while (currentRow in 0 until height && currentCol in 0 until width) {
        if (!blockSet.contains(keyOf(currentRow, currentCol))) {
            return CellPosition(currentRow, currentCol)
        }
        currentRow += if (direction == WordDirection.Down) -1 else 0
        currentCol += if (direction == WordDirection.Across) -1 else 0
    }

    return null
}

private fun resolveClueForCell(
    row: Int,
    col: Int,
    direction: WordDirection,
    data: PuzzlePublicDataDto,
    blockSet: Set<String>
): ClueSelection? {
    val wordIndex = data.wordIndex ?: return null
    val start = findWordStart(row, col, direction, blockSet)
    val map = if (direction == WordDirection.Across) wordIndex.across else wordIndex.down
    val number =
        map.entries.firstOrNull { (_, entry) ->
            entry.row == start.row && entry.col == start.col
        }?.key?.toIntOrNull()

    return if (number == null) null else ClueSelection(direction, number)
}

private fun findWordStart(
    row: Int,
    col: Int,
    direction: WordDirection,
    blockSet: Set<String>
): CellPosition {
    var currentRow = row
    var currentCol = col

    if (direction == WordDirection.Across) {
        while (currentCol > 0 && !blockSet.contains(keyOf(currentRow, currentCol - 1))) {
            currentCol -= 1
        }
    } else {
        while (currentRow > 0 && !blockSet.contains(keyOf(currentRow - 1, currentCol))) {
            currentRow -= 1
        }
    }

    return CellPosition(currentRow, currentCol)
}

private fun getWordEntry(
    clue: ClueSelection,
    wordIndex: PuzzleWordIndexDto?
): PuzzleWordIndexEntryDto? {
    val index = wordIndex ?: return null
    val map = if (clue.direction == WordDirection.Across) index.across else index.down

    return map[clue.number.toString()]
        ?: map.entries.firstOrNull { it.key.toIntOrNull() == clue.number }?.value
}

private fun buildHighlighted(
    clue: ClueSelection?,
    wordIndex: PuzzleWordIndexDto?
): Set<String> {
    val selected = clue ?: return emptySet()
    val entry = getWordEntry(selected, wordIndex) ?: return emptySet()

    val cells = mutableSetOf<String>()
    for (index in 0 until entry.length) {
        val row = entry.row + if (selected.direction == WordDirection.Down) index else 0
        val col = entry.col + if (selected.direction == WordDirection.Across) index else 0
        cells += keyOf(row, col)
    }
    return cells
}

private fun clueText(clue: ClueSelection, clues: PuzzleCluesDto): String {
    val list = if (clue.direction == WordDirection.Across) clues.across else clues.down
    return list.firstOrNull { it.number == clue.number }?.text.orEmpty()
}

private fun previewSolveSessionDetails(): SolveSessionDetailsDto {
    val puzzleData =
        PuzzlePublicDataDto(
            version = 1,
            width = 5,
            height = 5,
            blockCells =
                listOf(
                    PuzzleBlockCellDto(row = 0, col = 3),
                    PuzzleBlockCellDto(row = 1, col = 1),
                    PuzzleBlockCellDto(row = 2, col = 2),
                    PuzzleBlockCellDto(row = 3, col = 3),
                    PuzzleBlockCellDto(row = 4, col = 1)
                ),
            clues =
                PuzzleCluesDto(
                    across =
                        listOf(
                            PuzzleClueDto(number = 1, text = "Morning drink"),
                            PuzzleClueDto(number = 4, text = "Flying mammal")
                        ),
                    down =
                        listOf(
                            PuzzleClueDto(number = 1, text = "Musical beat"),
                            PuzzleClueDto(number = 2, text = "Ocean movement")
                        )
                ),
            wordIndex =
                PuzzleWordIndexDto(
                    across =
                        mapOf(
                            "1" to PuzzleWordIndexEntryDto(row = 0, col = 0, length = 3),
                            "4" to PuzzleWordIndexEntryDto(row = 2, col = 3, length = 2)
                        ),
                    down =
                        mapOf(
                            "1" to PuzzleWordIndexEntryDto(row = 0, col = 0, length = 5),
                            "2" to PuzzleWordIndexEntryDto(row = 0, col = 2, length = 2)
                        )
                ),
            meta = PuzzleMetaDto(author = "Preview Author", source = "Preview Source")
        )

    return SolveSessionDetailsDto(
        session =
            SolveSessionDto(
                id = "preview-session-id",
                puzzleId = "preview-puzzle-id",
                playerId = "preview-player",
                startedAt = "2026-02-19T00:00:00Z",
                updatedAt = "2026-02-19T00:10:00Z",
                completedAt = null
            ),
        puzzle =
            PuzzlePublicDto(
                id = "preview-puzzle-id",
                title = "Preview Crossword",
                isDaily = true,
                importedAt = "2026-02-19T00:00:00Z",
                data = puzzleData
            ),
        entries =
            listOf(
                EntryDto(row = 0, col = 0, value = "C", updatedAt = "2026-02-19T00:01:00Z"),
                EntryDto(row = 0, col = 1, value = "O", updatedAt = "2026-02-19T00:01:05Z"),
                EntryDto(row = 0, col = 2, value = "F", updatedAt = "2026-02-19T00:01:10Z"),
                EntryDto(row = 1, col = 0, value = "A", updatedAt = "2026-02-19T00:01:15Z"),
                EntryDto(row = 2, col = 0, value = "D", updatedAt = "2026-02-19T00:01:20Z")
            )
    )
}

@Preview(showBackground = true, widthDp = 400, heightDp = 900)
@Composable
private fun SolveContentPreview() {
    AndroidnativeTheme {
        SolveContent(
            details = previewSolveSessionDetails(),
            repository = null,
            onBack = {}
        )
    }
}

@Preview(showBackground = true, widthDp = 400, heightDp = 300)
@Composable
private fun SolveErrorPreview() {
    AndroidnativeTheme {
        SolveError(
            message = "Session not found.",
            onRetry = {},
            onBack = {}
        )
    }
}
