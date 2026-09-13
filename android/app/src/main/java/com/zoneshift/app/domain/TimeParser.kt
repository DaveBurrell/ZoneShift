package com.zoneshift.app.domain

import java.time.LocalTime
import java.time.format.DateTimeFormatter
import java.time.format.DateTimeParseException
import java.util.Locale

object TimeParser {
    private val exactFormats = listOf(
        "h:mm a", "hh:mm a", "h:mm:ss a", "hh:mm:ss a",
        "H:mm", "HH:mm", "H:mm:ss", "HH:mm:ss", "h a", "hh a", "H", "HH",
    ).map { DateTimeFormatter.ofPattern(it, Locale.US) }

    fun parseOrNull(text: String?): LocalTime? {
        if (text.isNullOrBlank()) return null
        val normalized = text.trim().replace('.', ':')
        parseLoose(normalized)?.let { return it }
        for (format in exactFormats) {
            try {
                return LocalTime.parse(normalized.uppercase(Locale.US), format)
            } catch (_: DateTimeParseException) {
                // Try the next accepted input format.
            }
        }
        return null
    }

    private fun parseLoose(text: String): LocalTime? {
        val compact = text.replace(" ", "").uppercase(Locale.US)
        val suffix = when {
            compact.endsWith("AM") -> "AM"
            compact.endsWith("PM") -> "PM"
            else -> null
        }
        val core = (if (suffix == null) compact else compact.dropLast(2)).replace(":", "")
        if (core.any { !it.isDigit() }) return null
        val pair = hourMinute(core) ?: return null
        var hour = pair.first
        val minute = pair.second
        if (suffix != null) {
            if (hour !in 1..12) return null
            hour %= 12
            if (suffix == "PM") hour += 12
        } else if (hour !in 0..23) {
            return null
        }
        return runCatching { LocalTime.of(hour, minute) }.getOrNull()
    }

    private fun hourMinute(digits: String): Pair<Int, Int>? {
        if (digits.length !in 1..4) return null
        val hour: Int
        val minute: Int
        when (digits.length) {
            1, 2 -> {
                hour = digits.toInt()
                minute = 0
            }
            3 -> {
                hour = digits.take(1).toInt()
                minute = digits.takeLast(2).toInt()
            }
            else -> {
                hour = digits.take(2).toInt()
                minute = digits.takeLast(2).toInt()
            }
        }
        return if (minute in 0..59) hour to minute else null
    }
}
