package com.example.android_native.data.api

import retrofit2.http.Body
import retrofit2.http.GET
import retrofit2.http.POST
import retrofit2.http.PUT
import retrofit2.http.Path

interface CommonwordApi {
    @GET("puzzles/today")
    suspend fun getTodayPuzzle(): PuzzlePublicDto

    @POST("sessions")
    suspend fun startSession(@Body request: StartSessionRequest): StartSessionResponse

    @GET("sessions/{id}")
    suspend fun getSession(@Path("id") sessionId: String): SolveSessionDetailsDto

    @PUT("sessions/{id}/cells/{row}/{col}")
    suspend fun setCell(
        @Path("id") sessionId: String,
        @Path("row") row: Int,
        @Path("col") col: Int,
        @Body request: UpsertEntryRequest
    ): EntryDto

    @POST("sessions/{id}/check-word")
    suspend fun checkWord(
        @Path("id") sessionId: String,
        @Body request: CheckWordRequest
    ): CheckWordResponse
}
