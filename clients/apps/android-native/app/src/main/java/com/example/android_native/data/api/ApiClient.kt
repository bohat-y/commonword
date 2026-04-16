package com.example.android_native.data.api

import com.example.android_native.BuildConfig
import okhttp3.OkHttpClient
import okhttp3.logging.HttpLoggingInterceptor
import retrofit2.Retrofit
import retrofit2.converter.gson.GsonConverterFactory
import java.util.concurrent.TimeUnit

object ApiClient {
    private val shouldLogHttp =
        BuildConfig.DEBUG || BuildConfig.BUILD_TYPE.contains("device", ignoreCase = true)

    private val loggingInterceptor =
        HttpLoggingInterceptor().apply {
            level =
                if (shouldLogHttp) {
                    HttpLoggingInterceptor.Level.BASIC
                } else {
                    HttpLoggingInterceptor.Level.NONE
                }
        }

    private val httpClient =
        OkHttpClient.Builder()
            .connectTimeout(4, TimeUnit.SECONDS)
            .readTimeout(8, TimeUnit.SECONDS)
            .writeTimeout(8, TimeUnit.SECONDS)
            .callTimeout(10, TimeUnit.SECONDS)
            .addInterceptor(loggingInterceptor)
            .build()

    private val retrofit =
        Retrofit.Builder()
            .baseUrl(BuildConfig.API_BASE_URL)
            .addConverterFactory(GsonConverterFactory.create())
            .client(httpClient)
            .build()

    val api: CommonwordApi = retrofit.create(CommonwordApi::class.java)
}
