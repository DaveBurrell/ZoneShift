package com.zoneshift.app.domain

import java.time.DateTimeException
import java.time.ZoneId
import java.util.Locale

object ZoneCatalog {
    private val curated = listOf(
        ZoneOption("UTC", "Coordinated Universal Time", "UTC", "GMT Zulu"),
        ZoneOption("Europe/London", "London", "GMT", "United Kingdom UK BST"),
        ZoneOption("Europe/Paris", "Central Europe", "CET", "Paris France CEST"),
        ZoneOption("Europe/Bucharest", "Eastern Europe", "EET", "Romania EEST"),
        ZoneOption("Asia/Kolkata", "India", "IST", "Delhi Mumbai Calcutta"),
        ZoneOption("Asia/Karachi", "Pakistan", "PKT"),
        ZoneOption("America/Chicago", "US Central", "CST", "CDT United States Canada"),
        ZoneOption("America/New_York", "US Eastern", "EST", "EDT United States Canada"),
        ZoneOption("America/Denver", "US Mountain", "MST", "MDT United States Canada"),
        ZoneOption("America/Los_Angeles", "US Pacific", "PST", "PDT United States Canada"),
        ZoneOption("America/Halifax", "Atlantic Canada", "AST", "ADT"),
        ZoneOption("America/Anchorage", "Alaska", "AKST", "AKDT"),
        ZoneOption("Pacific/Honolulu", "Hawaii", "HST"),
        ZoneOption("Asia/Shanghai", "China", "CST-CN", "Beijing"),
        ZoneOption("Asia/Tokyo", "Japan", "JST"),
        ZoneOption("Asia/Seoul", "South Korea", "KST"),
        ZoneOption("Asia/Singapore", "Singapore", "SGT"),
        ZoneOption("Australia/Sydney", "Sydney", "AEST", "AEDT Australia Eastern"),
        ZoneOption("Australia/Perth", "Perth", "AWST", "Australia Western"),
        ZoneOption("Pacific/Auckland", "New Zealand", "NZST", "NZDT"),
        ZoneOption("Africa/Johannesburg", "South Africa", "SAST"),
        ZoneOption("Europe/Moscow", "Moscow", "MSK", "Russia"),
        ZoneOption("Asia/Dubai", "Dubai", "GST", "UAE Gulf"),
        ZoneOption("America/Sao_Paulo", "São Paulo", "BRT", "Brazil Brasilia"),
        ZoneOption("America/Argentina/Buenos_Aires", "Buenos Aires", "ART", "Argentina"),
    )

    val defaultTargetIds = listOf(
        "Asia/Kolkata",
        "America/Chicago",
        "America/New_York",
        "America/Los_Angeles",
        "Europe/London",
    )

    val all: List<ZoneOption> by lazy {
        val available = ZoneId.getAvailableZoneIds()
        val curatedAvailable = curated.filter { it.id == "UTC" || it.id in available }
        val curatedIds = curatedAvailable.mapTo(mutableSetOf()) { it.id }
        val remaining = available
            .asSequence()
            .filterNot { it in curatedIds || it.startsWith("Etc/") || it.startsWith("SystemV/") }
            .map { id ->
                val city = id.substringAfterLast('/').replace('_', ' ')
                val region = id.substringBefore('/').replace('_', ' ')
                ZoneOption(id, city, city, "$region $id")
            }
            .sortedWith(compareBy(String.CASE_INSENSITIVE_ORDER) { it.label })
            .toList()
        curatedAvailable + remaining
    }

    private val byId by lazy { all.associateBy { it.id } }

    fun find(id: String): ZoneOption? = byId[id]

    fun optionFor(id: String): ZoneOption = find(id) ?: run {
        try {
            ZoneId.of(id)
            val city = id.substringAfterLast('/').replace('_', ' ')
            ZoneOption(id, city, city, id)
        } catch (_: DateTimeException) {
            ZoneOption("UTC", "Coordinated Universal Time", "UTC")
        }
    }

    fun search(query: String, favorites: Set<String>): List<ZoneOption> {
        val normalized = query.trim().lowercase(Locale.ROOT)
        return all
            .asSequence()
            .filter { option ->
                normalized.isEmpty() || listOf(
                    option.id,
                    option.label,
                    option.shortLabel,
                    option.searchTerms,
                ).any { normalized in it.lowercase(Locale.ROOT) }
            }
            .sortedWith(
                compareByDescending<ZoneOption> { it.id in favorites }
                    .thenBy(String.CASE_INSENSITIVE_ORDER) { it.label }
            )
            .toList()
    }
}
