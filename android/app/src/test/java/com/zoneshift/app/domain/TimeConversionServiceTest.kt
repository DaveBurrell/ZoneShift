package com.zoneshift.app.domain

import java.time.Clock
import java.time.Instant
import java.time.LocalDateTime
import java.time.ZoneId
import java.time.ZoneOffset
import org.junit.Assert.assertEquals
import org.junit.Assert.assertNotNull
import org.junit.Assert.assertTrue
import org.junit.Test

class TimeConversionServiceTest {
    @Test
    fun sameZonePreservesWallTime() {
        val wall = LocalDateTime.of(2024, 6, 15, 14, 30)
        val snapshot = TimeConversionService.convert(wall, UTC, UTC, emptyList())

        assertEquals(wall, snapshot.primary.localDateTime)
        assertEquals(null, snapshot.warning)
    }

    @Test
    fun utcToIndiaUsesFiveThirtyOffset() {
        val wall = LocalDateTime.of(2024, 1, 10, 0, 0)
        val india = ZoneId.of("Asia/Kolkata")
        val snapshot = TimeConversionService.convert(wall, UTC, UTC, listOf(india))

        assertEquals(LocalDateTime.of(2024, 1, 10, 5, 30), snapshot.targets.single().localDateTime)
        assertEquals(ZoneOffset.ofHoursMinutes(5, 30), snapshot.targets.single().utcOffset)
    }

    @Test
    fun springForwardGapAdvancesToNextValidTime() {
        val newYork = ZoneId.of("America/New_York")
        val gap = LocalDateTime.of(2024, 3, 10, 2, 30)

        assertEquals(LocalTimeKind.INVALID, TimeConversionService.classifyLocalTime(newYork, gap))
        val snapshot = TimeConversionService.convert(gap, newYork, UTC, emptyList())

        assertEquals(LocalDateTime.of(2024, 3, 10, 3, 30), snapshot.inputWallTime)
        assertTrue(snapshot.warning.orEmpty().contains("does not exist"))
    }

    @Test
    fun fallBackOverlapUsesEarlierDaylightOccurrence() {
        val newYork = ZoneId.of("America/New_York")
        val overlap = LocalDateTime.of(2024, 11, 3, 1, 30)

        assertEquals(LocalTimeKind.AMBIGUOUS, TimeConversionService.classifyLocalTime(newYork, overlap))
        val snapshot = TimeConversionService.convert(overlap, newYork, UTC, emptyList())

        assertEquals(Instant.parse("2024-11-03T05:30:00Z"), snapshot.instant)
        assertTrue(snapshot.warning.orEmpty().contains("occurs twice"))
    }

    @Test
    fun liveConversionUsesInjectedClock() {
        val instant = Instant.parse("2026-07-15T12:00:00Z")
        val clock = Clock.fixed(instant, UTC)
        val snapshot = TimeConversionService.convertLiveNow(
            UTC,
            ZoneId.of("Europe/London"),
            listOf(ZoneId.of("Asia/Kathmandu")),
            clock,
        )

        assertEquals(instant, snapshot.instant)
        assertEquals(13, snapshot.primary.localDateTime.hour)
        assertEquals(17, snapshot.targets.single().localDateTime.hour)
        assertEquals(45, snapshot.targets.single().localDateTime.minute)
    }

    @Test
    fun offsetDayDeltaAndCopyFormatsAreStable() {
        assertEquals("UTC+5:30", TimeConversionService.formatOffset(ZoneOffset.ofHoursMinutes(5, 30)))
        assertEquals("UTC-5", TimeConversionService.formatOffset(ZoneOffset.ofHours(-5)))
        assertEquals("Tomorrow", TimeConversionService.formatDayDelta(1))
        assertEquals("Yesterday", TimeConversionService.formatDayDelta(-1))

        val snapshot = TimeConversionService.convert(
            LocalDateTime.of(2024, 6, 1, 12, 0),
            UTC,
            UTC,
            listOf(ZoneId.of("Asia/Tokyo")),
        )
        val multiline = TimeConversionService.formatCopyMultiline(snapshot, true, false)
        val oneLine = TimeConversionService.formatCopyOneLine(snapshot, true)
        assertTrue(multiline.contains("ZoneShift"))
        assertTrue(multiline.contains("Japan"))
        assertTrue(oneLine.contains(" | "))
        assertNotNull(snapshot.targets.single())
    }

    private companion object {
        val UTC: ZoneId = ZoneOffset.UTC
    }
}
