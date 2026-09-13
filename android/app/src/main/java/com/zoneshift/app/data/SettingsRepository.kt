package com.zoneshift.app.data

import android.content.Context
import androidx.datastore.preferences.core.Preferences
import androidx.datastore.preferences.core.booleanPreferencesKey
import androidx.datastore.preferences.core.edit
import androidx.datastore.preferences.core.emptyPreferences
import androidx.datastore.preferences.core.stringPreferencesKey
import androidx.datastore.preferences.core.stringSetPreferencesKey
import androidx.datastore.preferences.preferencesDataStore
import com.zoneshift.app.domain.ConversionDirection
import com.zoneshift.app.domain.UserPreferences
import com.zoneshift.app.domain.ZoneCatalog
import java.io.IOException
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.catch
import kotlinx.coroutines.flow.map

private val Context.zoneShiftDataStore by preferencesDataStore(name = "zoneshift_settings")

class SettingsRepository(private val context: Context) {
    private object Keys {
        val use24Hour = booleanPreferencesKey("use_24_hour")
        val direction = stringPreferencesKey("direction")
        val reverseSource = stringPreferencesKey("reverse_source_iana_id")
        val targets = stringPreferencesKey("target_iana_ids")
        val favorites = stringSetPreferencesKey("favorite_iana_ids")
        val liveMode = booleanPreferencesKey("live_mode")
        val theme = stringPreferencesKey("theme")
        val onboarding = booleanPreferencesKey("has_seen_onboarding")
    }

    val preferences: Flow<UserPreferences> = context.zoneShiftDataStore.data
        .catch { error ->
            if (error is IOException) emit(emptyPreferences()) else throw error
        }
        .map(::decode)

    suspend fun setUse24Hour(value: Boolean) = update(Keys.use24Hour, value)
    suspend fun setDirection(value: ConversionDirection) = update(Keys.direction, value.name)
    suspend fun setReverseSource(id: String) = update(Keys.reverseSource, id)
    suspend fun setTargetIds(ids: List<String>) = update(Keys.targets, ids.joinToString(SEPARATOR))
    suspend fun setFavorites(ids: Set<String>) = update(Keys.favorites, ids)
    suspend fun setLiveMode(value: Boolean) = update(Keys.liveMode, value)
    suspend fun setTheme(value: String) = update(Keys.theme, value)
    suspend fun setOnboardingSeen(value: Boolean) = update(Keys.onboarding, value)

    private suspend fun <T> update(key: Preferences.Key<T>, value: T) {
        context.zoneShiftDataStore.edit { it[key] = value }
    }

    private fun decode(values: Preferences): UserPreferences {
        val targets = values[Keys.targets]
            ?.split(SEPARATOR)
            ?.filter { it.isNotBlank() }
            ?.distinct()
            ?.take(MAX_TARGETS)
            .orEmpty()
            .ifEmpty { ZoneCatalog.defaultTargetIds }
        val direction = runCatching {
            ConversionDirection.valueOf(values[Keys.direction].orEmpty())
        }.getOrDefault(ConversionDirection.FROM_LOCAL)
        val theme = values[Keys.theme].takeIf { it in VALID_THEMES } ?: "Studio"
        return UserPreferences(
            use24Hour = values[Keys.use24Hour] ?: false,
            direction = direction,
            reverseSourceZoneId = values[Keys.reverseSource] ?: "America/New_York",
            targetZoneIds = targets,
            favoriteZoneIds = values[Keys.favorites].orEmpty(),
            liveMode = values[Keys.liveMode] ?: true,
            theme = theme,
            hasSeenOnboarding = values[Keys.onboarding] ?: false,
        )
    }

    private companion object {
        const val SEPARATOR = "\u001F"
        const val MAX_TARGETS = 8
        val VALID_THEMES = setOf("Studio", "Classic", "Night Ops", "Meridian")
    }
}
