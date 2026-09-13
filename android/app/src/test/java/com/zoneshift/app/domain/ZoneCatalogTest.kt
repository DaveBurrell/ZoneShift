package com.zoneshift.app.domain

import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Test

class ZoneCatalogTest {
    @Test
    fun catalogUsesUniqueIanaIds() {
        assertEquals(ZoneCatalog.all.size, ZoneCatalog.all.map { it.id }.distinct().size)
        assertTrue(ZoneCatalog.defaultTargetIds.all { ZoneCatalog.find(it) != null })
    }

    @Test
    fun favoritesSortBeforeOtherSearchResults() {
        val results = ZoneCatalog.search("America", setOf("America/New_York"))
        assertEquals("America/New_York", results.first().id)
    }
}
