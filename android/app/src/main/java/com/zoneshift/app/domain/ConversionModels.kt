package com.zoneshift.app.domain

import java.time.Instant
import java.time.LocalDate
import java.time.LocalDateTime
import java.time.ZoneId
import java.time.ZoneOffset

enum class ConversionDirection {
    FROM_LOCAL,
    TO_LOCAL,
}

enum class LocalTimeKind {
    VALID,
    INVALID,
    AMBIGUOUS,
}

data class ZoneOption(
    val id: String,
    val label: String,
    val shortLabel: String,
    val searchTerms: String = "",
) {
    val zoneId: ZoneId get() = ZoneId.of(id)
}

data class ZoneConversionResult(
    val zoneId: ZoneId,
    val label: String,
    val abbreviation: String,
    val localDateTime: LocalDateTime,
    val utcOffset: ZoneOffset,
    val dayDeltaFromPrimary: Long,
)

data class ConversionSnapshot(
    val inputWallTime: LocalDateTime,
    val instant: Instant,
    val sourceZoneId: ZoneId,
    val primary: ZoneConversionResult,
    val targets: List<ZoneConversionResult>,
    val warning: String? = null,
)

data class UserPreferences(
    val use24Hour: Boolean = false,
    val direction: ConversionDirection = ConversionDirection.FROM_LOCAL,
    val reverseSourceZoneId: String = "America/New_York",
    val targetZoneIds: List<String> = ZoneCatalog.defaultTargetIds,
    val favoriteZoneIds: Set<String> = emptySet(),
    val liveMode: Boolean = true,
    val theme: String = "Studio",
    val hasSeenOnboarding: Boolean = false,
)

data class ConverterUiState(
    val preferencesLoaded: Boolean = false,
    val preferences: UserPreferences = UserPreferences(),
    val customDateTime: LocalDateTime = LocalDateTime.now().withSecond(0).withNano(0),
    val snapshot: ConversionSnapshot? = null,
)
