package com.zoneshift.app.domain

import java.time.LocalTime
import org.junit.Assert.assertEquals
import org.junit.Assert.assertNull
import org.junit.Test

class TimeParserTest {
    @Test
    fun parsesDesktopCompatibleInputs() {
        mapOf(
            "7:00 AM" to LocalTime.of(7, 0),
            "7am" to LocalTime.of(7, 0),
            "7:30 PM" to LocalTime.of(19, 30),
            "19:00" to LocalTime.of(19, 0),
            "00:00" to LocalTime.MIDNIGHT,
            "12:00 PM" to LocalTime.NOON,
            "12:00 AM" to LocalTime.MIDNIGHT,
            "0730" to LocalTime.of(7, 30),
            "1930" to LocalTime.of(19, 30),
            "7.45pm" to LocalTime.of(19, 45),
        ).forEach { (text, expected) ->
            assertEquals(text, expected, TimeParser.parseOrNull(text))
        }
    }

    @Test
    fun rejectsInvalidInputs() {
        listOf("", "   ", "not-a-time", "25:00", "1260").forEach { text ->
            assertNull(text, TimeParser.parseOrNull(text))
        }
    }
}
