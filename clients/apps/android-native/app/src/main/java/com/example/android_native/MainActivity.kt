package com.example.android_native

import android.os.Bundle
import android.os.Build
import androidx.activity.ComponentActivity
import androidx.activity.SystemBarStyle
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.activity.viewModels
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.safeDrawing
import androidx.compose.foundation.layout.only
import androidx.compose.material3.Scaffold
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Surface
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.toArgb
import androidx.compose.foundation.layout.WindowInsets
import androidx.compose.foundation.layout.WindowInsetsSides
import com.example.android_native.data.api.ApiClient
import com.example.android_native.data.local.PlayerStore
import com.example.android_native.data.repository.HomeRepository
import com.example.android_native.feature.home.HomeScreen
import com.example.android_native.feature.home.HomeViewModel
import com.example.android_native.feature.home.HomeViewModelFactory
import com.example.android_native.feature.solve.SolveScreen
import com.example.android_native.ui.theme.AndroidnativeTheme

class MainActivity : ComponentActivity() {
    private val homeRepository by lazy { HomeRepository(ApiClient.api) }
    private val playerStore by lazy { PlayerStore(applicationContext) }

    private val homeViewModel: HomeViewModel by
        viewModels {
            HomeViewModelFactory(
                repository = homeRepository,
                playerStore = playerStore
            )
        }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.Q) {
            window.isNavigationBarContrastEnforced = false
        }
        setContent {
            AndroidnativeTheme {
                val transparent = android.graphics.Color.TRANSPARENT
                val lightScrim = MaterialTheme.colorScheme.surface.toArgb()
                val darkScrim = MaterialTheme.colorScheme.surface.toArgb()

                LaunchedEffect(lightScrim, darkScrim) {
                    enableEdgeToEdge(
                        statusBarStyle = SystemBarStyle.auto(lightScrim, darkScrim),
                        navigationBarStyle = SystemBarStyle.auto(transparent, transparent)
                    )
                }

                val activeSessionIdState = rememberSaveable { mutableStateOf<String?>(null) }
                val uiState by homeViewModel.uiState.collectAsState()

                LaunchedEffect(uiState.startedSessionId) {
                    val sessionId = uiState.startedSessionId ?: return@LaunchedEffect
                    activeSessionIdState.value = sessionId
                    homeViewModel.consumeStartedSession()
                }

                Surface(
                    modifier = Modifier.fillMaxSize(),
                    color = MaterialTheme.colorScheme.background
                ) {
                    Scaffold(
                        modifier = Modifier.fillMaxSize(),
                        containerColor = MaterialTheme.colorScheme.background,
                        contentWindowInsets =
                            WindowInsets.safeDrawing.only(
                                WindowInsetsSides.Top + WindowInsetsSides.Horizontal
                            )
                    ) { innerPadding ->
                        val sessionId = activeSessionIdState.value
                        if (sessionId == null) {
                            HomeScreen(
                                state = uiState,
                                onRetry = homeViewModel::refresh,
                                onStartSession = homeViewModel::startSession,
                                modifier = Modifier.padding(innerPadding)
                            )
                        } else {
                            SolveScreen(
                                sessionId = sessionId,
                                repository = homeRepository,
                                onBack = { activeSessionIdState.value = null },
                                modifier = Modifier.padding(innerPadding)
                            )
                        }
                    }
                }
            }
        }
    }
}
