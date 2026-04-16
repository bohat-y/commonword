package com.example.android_native.data.repository

import com.example.android_native.data.api.CommonwordApi
import com.example.android_native.data.api.CheckWordRequest
import com.example.android_native.data.api.CheckWordResponse
import com.example.android_native.data.api.EntryDto
import com.example.android_native.data.api.PuzzlePublicDto
import com.example.android_native.data.api.SolveSessionDetailsDto
import com.example.android_native.data.api.StartSessionRequest
import com.example.android_native.data.api.StartSessionResponse
import com.example.android_native.data.api.UpsertEntryRequest
import retrofit2.HttpException

class HomeRepository(private val api: CommonwordApi) {
    suspend fun getTodayPuzzle(): PuzzlePublicDto? =
        try {
            api.getTodayPuzzle()
        } catch (err: HttpException) {
            if (err.code() == 404) {
                null
            } else {
                throw err
            }
        }

    suspend fun startSession(puzzleId: String, playerId: String): StartSessionResponse =
        api.startSession(StartSessionRequest(puzzleId = puzzleId, playerId = playerId))

    suspend fun getSessionDetails(sessionId: String): SolveSessionDetailsDto =
        api.getSession(sessionId)

    suspend fun setCell(
        sessionId: String,
        row: Int,
        col: Int,
        value: String?
    ): EntryDto = api.setCell(sessionId, row, col, UpsertEntryRequest(value = value))

    suspend fun checkWord(
        sessionId: String,
        direction: String,
        number: Int
    ): CheckWordResponse =
        api.checkWord(
            sessionId = sessionId,
            request = CheckWordRequest(direction = direction, number = number)
        )

    suspend fun sessionExists(sessionId: String): Boolean {
        return try {
            api.getSession(sessionId)
            true
        } catch (err: HttpException) {
            if (err.code() == 404) {
                false
            } else {
                throw err
            }
        }
    }
}
