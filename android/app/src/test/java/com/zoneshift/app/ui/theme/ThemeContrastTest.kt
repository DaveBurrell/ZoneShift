package com.zoneshift.app.ui.theme

import androidx.compose.ui.graphics.Color
import org.junit.Assert.assertTrue
import org.junit.Test

class ThemeContrastTest {
    @Test
    fun primaryTextAndMainSurfacesMeetWcagAa() {
        listOf("Studio", "Classic", "Night Ops", "Meridian").forEach { name ->
            val scheme = schemeFor(name)
            assertContrast(name, "background", scheme.onBackground, scheme.background)
            assertContrast(name, "surface", scheme.onSurface, scheme.surface)
            assertContrast(name, "primary", scheme.onPrimary, scheme.primary)
        }
    }

    private fun assertContrast(theme: String, role: String, foreground: Color, background: Color) {
        val lighter = maxOf(luminance(foreground), luminance(background))
        val darker = minOf(luminance(foreground), luminance(background))
        val ratio = (lighter + 0.05) / (darker + 0.05)
        assertTrue("$theme $role contrast was $ratio", ratio >= 4.5)
    }

    private fun luminance(color: Color): Double {
        fun channel(value: Float): Double {
            val normalized = value.toDouble()
            return if (normalized <= 0.04045) normalized / 12.92
            else Math.pow((normalized + 0.055) / 1.055, 2.4)
        }
        return 0.2126 * channel(color.red) +
            0.7152 * channel(color.green) +
            0.0722 * channel(color.blue)
    }
}
