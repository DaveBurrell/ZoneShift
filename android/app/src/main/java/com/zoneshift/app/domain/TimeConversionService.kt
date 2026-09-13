package com.zoneshift.app.domain

import java.time.Clock
import java.time.Instant
import java.time.LocalDateTime
import java.time.ZoneId
import java.time.ZoneOffset
import java.time.ZonedDateTime
import java.time.format.DateTimeFormatter
import java.time.temporal.ChronoUnit
import java.util.Locale
import kotlin.math.abs

object TimeConversionService {
    fun classifyLocalTime(zoneId: ZoneId, wallTime: LocalDateTime): LocalTimeKind {
        return when (zoneId.rules.getValidOffsets(wallTime).size) {
            0 -> LocalTimeKind.INVALID
            1 -> LocalTimeKind.VALID
            else -> LocalTimeKind.AMBIGUOUS
        }
    }

    fun convert(
        inputWallTime: LocalDateTime,
        inputZoneId: ZoneId,
        primaryZoneId: ZoneId,
        targetZoneIds: List<ZoneId>,
    ): ConversionSnapshot {
        val offsets = inputZoneId.rules.getValidOffsets(inputWallTime)
        val warning: String?
        val resolved: ZonedDateTime

        when (offsets.size) {
            0 -> {
                val transition = requireNotNull(inputZoneId.rules.getTransition(inputWallTime))
                val adjusted = inputWallTime.plus(transition.duration)
                resolved = ZonedDateTime.ofLocal(adjusted, inputZoneId, transition.offsetAfter)
                warning = "${formatWall(inputWallTime)} does not exist in ${friendly(inputZoneId)} " +
                    "because the clocks move forward. Using ${formatWall(adjusted)} instead."
            }

            1 -> {
                resolved = ZonedDateTime.ofLocal(inputWallTime, inputZoneId, offsets.single())
                warning = null
            }

            else -> {
                val preferred = offsets.maxBy { it.totalSeconds }
                resolved = ZonedDateTime.ofLocal(inputWallTime, inputZoneId, preferred)
                warning = "${formatWall(inputWallTime)} occurs twice in ${friendly(inputZoneId)}. " +
                    "Using the ${formatOffset(preferred)} occurrence."
            }
        }

        return snapshot(
            inputWallTime = resolved.toLocalDateTime(),
            instant = resolved.toInstant(),
            sourceZoneId = inputZoneId,
            primaryZoneId = primaryZoneId,
            targetZoneIds = targetZoneIds,
            warning = warning,
        )
    }

    fun convertLiveNow(
        inputZoneId: ZoneId,
        primaryZoneId: ZoneId,
        targetZoneIds: List<ZoneId>,
        clock: Clock = Clock.systemUTC(),
    ): ConversionSnapshot {
        val instant = clock.instant()
        return snapshot(
            inputWallTime = instant.atZone(inputZoneId).toLocalDateTime(),
            instant = instant,
            sourceZoneId = inputZoneId,
            primaryZoneId = primaryZoneId,
            targetZoneIds = targetZoneIds,
            warning = null,
        )
    }

    private fun snapshot(
        inputWallTime: LocalDateTime,
        instant: Instant,
        sourceZoneId: ZoneId,
        primaryZoneId: ZoneId,
        targetZoneIds: List<ZoneId>,
        warning: String?,
    ): ConversionSnapshot {
        val primaryDateTime = instant.atZone(primaryZoneId)
        val primary = result(primaryZoneId, primaryDateTime, primaryDateTime)
        val targets = targetZoneIds.distinct().map { zoneId ->
            result(zoneId, instant.atZone(zoneId), primaryDateTime)
        }
        return ConversionSnapshot(
            inputWallTime = inputWallTime,
            instant = instant,
            sourceZoneId = sourceZoneId,
            primary = primary,
            targets = targets,
            warning = warning,
        )
    }

    private fun result(
        zoneId: ZoneId,
        zonedDateTime: ZonedDateTime,
        primaryDateTime: ZonedDateTime,
    ): ZoneConversionResult {
        val option = ZoneCatalog.optionFor(zoneId.id)
        val abbreviation = DateTimeFormatter.ofPattern("z", Locale.getDefault()).format(zonedDateTime)
        return ZoneConversionResult(
            zoneId = zoneId,
            label = option.label,
            abbreviation = abbreviation,
            localDateTime = zonedDateTime.toLocalDateTime(),
            utcOffset = zonedDateTime.offset,
            dayDeltaFromPrimary = ChronoUnit.DAYS.between(
                primaryDateTime.toLocalDate(),
                zonedDateTime.toLocalDate(),
            ),
        )
    }

    fun formatOffset(offset: ZoneOffset): String {
        val totalMinutes = offset.totalSeconds / 60
        val sign = if (totalMinutes < 0) "-" else "+"
        val absolute = abs(totalMinutes)
        val hours = absolute / 60
        val minutes = absolute % 60
        return if (minutes == 0) "UTC$sign$hours" else "UTC$sign$hours:${minutes.toString().padStart(2, '0')}"
    }

    fun formatDayDelta(days: Long): String = when {
        days == 0L -> ""
        days == 1L -> "Tomorrow"
        days == -1L -> "Yesterday"
        days > 1L -> "+${days} days"
        else -> "${days} days"
    }

    fun formatDigital(time: LocalDateTime, use24Hour: Boolean, includeSeconds: Boolean): String {
        val pattern = when {
            use24Hour && includeSeconds -> "HH:mm:ss"
            use24Hour -> "HH:mm"
            includeSeconds -> "hh:mm:ss a"
            else -> "hh:mm a"
        }
        return time.format(DateTimeFormatter.ofPattern(pattern, Locale.getDefault()))
    }

    fun formatCopyMultiline(snapshot: ConversionSnapshot, use24Hour: Boolean, live: Boolean): String {
        val date = snapshot.primary.localDateTime.toLocalDate()
        val lines = mutableListOf(
            "ZoneShift ($date${if (live) ", live" else ""})",
            "Local: ${formatDigital(snapshot.primary.localDateTime, use24Hour, live)} " +
                "(${formatOffset(snapshot.primary.utcOffset)})",
        )
        snapshot.targets.forEach { target ->
            val day = formatDayDelta(target.dayDeltaFromPrimary)
            val daySuffix = if (day.isEmpty()) "" else ", $day"
            lines += "${target.label}: ${formatDigital(target.localDateTime, use24Hour, live)} " +
                "(${formatOffset(target.utcOffset)}$daySuffix)"
        }
        snapshot.warning?.let { lines += "Note: $it" }
        return lines.joinToString("\n")
    }

    fun formatCopyOneLine(snapshot: ConversionSnapshot, use24Hour: Boolean): String {
        val parts = mutableListOf(
            "Local ${formatDigital(snapshot.primary.localDateTime, use24Hour, false)}"
        )
        snapshot.targets.forEach { target ->
            parts += "${target.label} ${formatDigital(target.localDateTime, use24Hour, false)}"
        }
        return parts.joinToString(" | ") + if (snapshot.warning == null) "" else " (DST note)"
    }

    private fun formatWall(time: LocalDateTime): String =
        time.format(DateTimeFormatter.ofPattern("h:mm a", Locale.getDefault()))

    private fun friendly(zoneId: ZoneId): String = ZoneCatalog.optionFor(zoneId.id).label
}
