package com.zoneshift.app.ui.theme

import android.app.Activity
import androidx.compose.material3.ColorScheme
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.darkColorScheme
import androidx.compose.material3.lightColorScheme
import androidx.compose.runtime.Composable
import androidx.compose.runtime.SideEffect
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.toArgb
import androidx.compose.ui.platform.LocalView
import androidx.core.view.WindowCompat

private val Studio = darkColorScheme(
    primary = Color(0xFFFFB44A),
    onPrimary = Color(0xFF281800),
    primaryContainer = Color(0xFF493517),
    onPrimaryContainer = Color(0xFFFFDDAF),
    secondary = Color(0xFF9CCBFF),
    background = Color(0xFF15191D),
    onBackground = Color(0xFFE6E9ED),
    surface = Color(0xFF20252B),
    onSurface = Color(0xFFE6E9ED),
    surfaceVariant = Color(0xFF2A3037),
    onSurfaceVariant = Color(0xFFC4C9D0),
    outline = Color(0xFF69727C),
    error = Color(0xFFFFB4AB),
)

private val Classic = lightColorScheme(
    primary = Color(0xFF4657B8),
    onPrimary = Color.White,
    primaryContainer = Color(0xFFDDE1FF),
    onPrimaryContainer = Color(0xFF07164F),
    secondary = Color(0xFF007B62),
    background = Color(0xFFF4F6FA),
    onBackground = Color(0xFF1A1B20),
    surface = Color(0xFFFFFFFF),
    onSurface = Color(0xFF1A1B20),
    surfaceVariant = Color(0xFFE4E5EC),
    onSurfaceVariant = Color(0xFF46464F),
    outline = Color(0xFF777680),
)

private val NightOps = darkColorScheme(
    primary = Color(0xFF70F08B),
    onPrimary = Color(0xFF003913),
    primaryContainer = Color(0xFF0C4920),
    onPrimaryContainer = Color(0xFF96F9A7),
    secondary = Color(0xFF8CD5A0),
    background = Color(0xFF07120B),
    onBackground = Color(0xFFD7E8D9),
    surface = Color(0xFF101C14),
    onSurface = Color(0xFFD7E8D9),
    surfaceVariant = Color(0xFF17271B),
    onSurfaceVariant = Color(0xFFB7CABB),
    outline = Color(0xFF5C7863),
    error = Color(0xFFFFB4AB),
)

private val Meridian = lightColorScheme(
    primary = Color(0xFF006C68),
    onPrimary = Color.White,
    primaryContainer = Color(0xFF9CF1EB),
    onPrimaryContainer = Color(0xFF00201E),
    secondary = Color(0xFFA33E49),
    background = Color(0xFFFFF8F4),
    onBackground = Color(0xFF211A18),
    surface = Color(0xFFFFFBFF),
    onSurface = Color(0xFF211A18),
    surfaceVariant = Color(0xFFF3DFDC),
    onSurfaceVariant = Color(0xFF524441),
    outline = Color(0xFF857370),
)

fun schemeFor(name: String): ColorScheme = when (name) {
    "Classic" -> Classic
    "Night Ops" -> NightOps
    "Meridian" -> Meridian
    else -> Studio
}

@Composable
fun ZoneShiftTheme(themeName: String, content: @Composable () -> Unit) {
    val scheme = schemeFor(themeName)
    val view = LocalView.current
    if (!view.isInEditMode) {
        SideEffect {
            val window = (view.context as Activity).window
            window.statusBarColor = scheme.background.toArgb()
            window.navigationBarColor = scheme.background.toArgb()
            WindowCompat.getInsetsController(window, view).apply {
                isAppearanceLightStatusBars = themeName == "Classic" || themeName == "Meridian"
                isAppearanceLightNavigationBars = themeName == "Classic" || themeName == "Meridian"
            }
        }
    }
    MaterialTheme(colorScheme = scheme, content = content)
}
