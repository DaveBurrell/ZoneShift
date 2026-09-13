package com.zoneshift.app

import android.app.Application
import androidx.lifecycle.AndroidViewModel
import androidx.lifecycle.viewModelScope
import com.zoneshift.app.data.SettingsRepository
import com.zoneshift.app.domain.ConversionDirection
import com.zoneshift.app.domain.ConverterUiState
import com.zoneshift.app.domain.TimeConversionService
import com.zoneshift.app.domain.ZoneCatalog
import java.time.Clock
import java.time.DateTimeException
import java.time.LocalDate
import java.time.LocalDateTime
import java.time.LocalTime
import java.time.ZoneId
import kotlinx.coroutines.delay
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.collectLatest
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.isActive
import kotlinx.coroutines.launch

class ConverterViewModel @JvmOverloads constructor(
    application: Application,
    private val clock: Clock = Clock.systemUTC(),
) : AndroidViewModel(application) {
    private val repository = SettingsRepository(application.applicationContext)
    private val mutableState = MutableStateFlow(ConverterUiState())
    val state: StateFlow<ConverterUiState> = mutableState.asStateFlow()

    init {
        viewModelScope.launch {
            repository.preferences.collectLatest { preferences ->
                mutableState.update {
                    recalculate(it.copy(preferencesLoaded = true, preferences = preferences))
                }
            }
        }
        viewModelScope.launch {
            while (isActive) {
                val wait = 1_000L - (System.currentTimeMillis() % 1_000L)
                delay(wait)
                if (mutableState.value.preferences.liveMode) {
                    mutableState.update(::recalculate)
                }
            }
        }
    }

    fun setLiveMode(value: Boolean) {
        viewModelScope.launch { repository.setLiveMode(value) }
    }

    fun setUse24Hour(value: Boolean) {
        viewModelScope.launch { repository.setUse24Hour(value) }
    }

    fun setDirection(value: ConversionDirection) {
        viewModelScope.launch { repository.setDirection(value) }
    }

    fun setReverseSource(id: String) {
        viewModelScope.launch { repository.setReverseSource(id) }
    }

    fun setTheme(theme: String) {
        viewModelScope.launch { repository.setTheme(theme) }
    }

    fun setCustomDate(date: LocalDate) {
        mutableState.update { current ->
            recalculate(current.copy(customDateTime = LocalDateTime.of(date, current.customDateTime.toLocalTime())))
        }
    }

    fun setCustomTime(time: LocalTime) {
        mutableState.update { current ->
            recalculate(current.copy(customDateTime = LocalDateTime.of(current.customDateTime.toLocalDate(), time)))
        }
    }

    fun addTarget(id: String) {
        val targets = mutableState.value.preferences.targetZoneIds
        if (id in targets || targets.size >= MAX_TARGETS) return
        viewModelScope.launch { repository.setTargetIds(targets + id) }
    }

    fun removeTarget(id: String) {
        val targets = mutableState.value.preferences.targetZoneIds
        if (targets.size <= 1) return
        viewModelScope.launch { repository.setTargetIds(targets - id) }
    }

    fun moveTarget(id: String, delta: Int) {
        val targets = mutableState.value.preferences.targetZoneIds.toMutableList()
        val from = targets.indexOf(id)
        val to = from + delta
        if (from < 0 || to !in targets.indices) return
        val item = targets.removeAt(from)
        targets.add(to, item)
        viewModelScope.launch { repository.setTargetIds(targets) }
    }

    fun toggleFavorite(id: String) {
        val favorites = mutableState.value.preferences.favoriteZoneIds.toMutableSet()
        if (!favorites.add(id)) favorites.remove(id)
        viewModelScope.launch { repository.setFavorites(favorites) }
    }

    fun markOnboardingSeen() {
        viewModelScope.launch { repository.setOnboardingSeen(true) }
    }

    fun refresh() {
        mutableState.update(::recalculate)
    }

    private fun recalculate(current: ConverterUiState): ConverterUiState {
        if (!current.preferencesLoaded) return current
        val preferences = current.preferences
        val localZone = ZoneId.systemDefault()
        val inputZone = if (preferences.direction == ConversionDirection.FROM_LOCAL) {
            localZone
        } else {
            safeZone(preferences.reverseSourceZoneId)
        }
        val targets = preferences.targetZoneIds
            .map(::safeZone)
        val snapshot = if (preferences.liveMode) {
            TimeConversionService.convertLiveNow(inputZone, localZone, targets, clock)
        } else {
            TimeConversionService.convert(current.customDateTime, inputZone, localZone, targets)
        }
        return current.copy(snapshot = snapshot)
    }

    private fun safeZone(id: String): ZoneId = try {
        ZoneId.of(id)
    } catch (_: DateTimeException) {
        ZoneId.of("UTC")
    }

    private companion object {
        const val MAX_TARGETS = 8
    }
}
