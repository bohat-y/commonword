package com.example.android_native.data.api

data class PuzzlePublicDto(
    val id: String,
    val title: String,
    val isDaily: Boolean? = null,
    val importedAt: String? = null,
    val data: PuzzlePublicDataDto
)

data class PuzzlePublicDataDto(
    val version: Int = 1,
    val width: Int,
    val height: Int,
    val blockCells: List<PuzzleBlockCellDto> = emptyList(),
    val clues: PuzzleCluesDto = PuzzleCluesDto(),
    val wordIndex: PuzzleWordIndexDto? = null,
    val meta: PuzzleMetaDto?
)

data class PuzzleBlockCellDto(
    val row: Int,
    val col: Int
)

data class PuzzleCluesDto(
    val across: List<PuzzleClueDto> = emptyList(),
    val down: List<PuzzleClueDto> = emptyList()
)

data class PuzzleClueDto(
    val number: Int,
    val text: String
)

data class PuzzleWordIndexDto(
    val across: Map<String, PuzzleWordIndexEntryDto> = emptyMap(),
    val down: Map<String, PuzzleWordIndexEntryDto> = emptyMap()
)

data class PuzzleWordIndexEntryDto(
    val row: Int,
    val col: Int,
    val length: Int
)

data class PuzzleMetaDto(
    val author: String?,
    val source: String?
)

data class StartSessionRequest(
    val puzzleId: String,
    val playerId: String
)

data class UpsertEntryRequest(
    val value: String?
)

data class CheckWordRequest(
    val direction: String,
    val number: Int
)

data class CellCoordinateDto(
    val row: Int,
    val col: Int
)

data class CheckWordResponse(
    val complete: Boolean,
    val correct: Boolean,
    val incorrectCells: List<CellCoordinateDto>?
)

data class StartSessionResponse(
    val id: String
)

data class SolveSessionDetailsDto(
    val session: SolveSessionDto,
    val puzzle: PuzzlePublicDto,
    val entries: List<EntryDto>
)

data class SolveSessionDto(
    val id: String,
    val puzzleId: String,
    val playerId: String,
    val startedAt: String,
    val updatedAt: String,
    val completedAt: String?
)

data class EntryDto(
    val row: Int,
    val col: Int,
    val value: String,
    val updatedAt: String
)
