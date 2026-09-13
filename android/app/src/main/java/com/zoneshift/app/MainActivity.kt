package com.zoneshift.app

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.RowScope
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxHeight
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.Button
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.FilterChip
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.ModalBottomSheet
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Scaffold
import androidx.compose.material3.SnackbarHost
import androidx.compose.material3.SnackbarHostState
import androidx.compose.material3.Switch
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.material3.TopAppBar
import androidx.compose.runtime.Composable
import androidx.compose.runtime.DisposableEffect
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalClipboardManager
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.text.AnnotatedString
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.lifecycle.Lifecycle
import androidx.lifecycle.LifecycleEventObserver
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.lifecycle.compose.LocalLifecycleOwner
import androidx.lifecycle.viewmodel.compose.viewModel
import com.zoneshift.app.domain.ConversionDirection
import com.zoneshift.app.domain.ConverterUiState
import com.zoneshift.app.domain.TimeConversionService
import com.zoneshift.app.domain.ZoneCatalog
import com.zoneshift.app.domain.ZoneConversionResult
import com.zoneshift.app.domain.ZoneOption
import com.zoneshift.app.ui.theme.ZoneShiftTheme
import java.time.LocalDate
import java.time.LocalTime
import java.time.ZoneId
import java.time.format.DateTimeFormatter
import java.util.Locale

class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        enableEdgeToEdge()
        setContent {
            val viewModel: ConverterViewModel = viewModel()
            val state by viewModel.state.collectAsStateWithLifecycle()
            ZoneShiftTheme(state.preferences.theme) {
                ZoneShiftScreen(state, viewModel)
            }
        }
    }
}

private enum class ZonePickerMode { ADD_TARGET, REVERSE_SOURCE }

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun ZoneShiftScreen(state: ConverterUiState, viewModel: ConverterViewModel) {
    val clipboard = LocalClipboardManager.current
    val context = LocalContext.current
    val lifecycleOwner = LocalLifecycleOwner.current
    val snackbar = remember { SnackbarHostState() }
    var pickerMode by rememberSaveable { mutableStateOf<ZonePickerMode?>(null) }
    var settingsVisible by rememberSaveable { mutableStateOf(false) }
    var snackbarMessage by remember { mutableStateOf<String?>(null) }

    DisposableEffect(lifecycleOwner) {
        val observer = LifecycleEventObserver { _, event ->
            if (event == Lifecycle.Event.ON_RESUME) viewModel.refresh()
        }
        lifecycleOwner.lifecycle.addObserver(observer)
        onDispose { lifecycleOwner.lifecycle.removeObserver(observer) }
    }

    LaunchedEffect(snackbarMessage) {
        snackbarMessage?.let {
            snackbar.showSnackbar(it)
            snackbarMessage = null
        }
    }

    Scaffold(
        snackbarHost = { SnackbarHost(snackbar) },
        topBar = {
            TopAppBar(
                title = {
                    Column {
                        Text("ZoneShift", fontWeight = FontWeight.Bold)
                        Text(
                            "Time together, anywhere",
                            style = MaterialTheme.typography.labelSmall,
                            color = MaterialTheme.colorScheme.onSurfaceVariant,
                        )
                    }
                },
                actions = {
                    TextButton(onClick = { settingsVisible = true }) { Text("Settings") }
                },
            )
        },
    ) { scaffoldPadding ->
        if (!state.preferencesLoaded || state.snapshot == null) {
            Box(
                modifier = Modifier.fillMaxSize().padding(scaffoldPadding),
                contentAlignment = Alignment.Center,
            ) { CircularProgressIndicator() }
        } else {
            val snapshot = state.snapshot
            LazyColumn(
                modifier = Modifier.fillMaxSize().padding(scaffoldPadding),
                contentPadding = PaddingValues(horizontal = 16.dp, vertical = 12.dp),
                verticalArrangement = Arrangement.spacedBy(12.dp),
            ) {
                item {
                    SectionLabel("WHEN")
                    ChipRow {
                        FilterChip(
                            selected = state.preferences.liveMode,
                            onClick = { viewModel.setLiveMode(true) },
                            label = { Text("Live now") },
                        )
                        FilterChip(
                            selected = !state.preferences.liveMode,
                            onClick = { viewModel.setLiveMode(false) },
                            label = { Text("Custom") },
                        )
                    }
                }
                item {
                    SectionLabel("DIRECTION")
                    ChipRow {
                        FilterChip(
                            selected = state.preferences.direction == ConversionDirection.FROM_LOCAL,
                            onClick = { viewModel.setDirection(ConversionDirection.FROM_LOCAL) },
                            label = { Text("From my zone") },
                        )
                        FilterChip(
                            selected = state.preferences.direction == ConversionDirection.TO_LOCAL,
                            onClick = { viewModel.setDirection(ConversionDirection.TO_LOCAL) },
                            label = { Text("To my zone") },
                        )
                    }
                }
                if (state.preferences.direction == ConversionDirection.TO_LOCAL) {
                    item {
                        SourceZoneButton(
                            zoneId = state.preferences.reverseSourceZoneId,
                            onClick = { pickerMode = ZonePickerMode.REVERSE_SOURCE },
                        )
                    }
                }
                item {
                    PrimaryTimeCard(state, viewModel)
                }
                snapshot.warning?.let { warning ->
                    item { WarningCard(warning) }
                }
                item {
                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        verticalAlignment = Alignment.CenterVertically,
                    ) {
                        SectionLabel("OTHER ZONES", Modifier.weight(1f))
                        Text(
                            "${snapshot.targets.size}/8",
                            style = MaterialTheme.typography.labelMedium,
                            color = MaterialTheme.colorScheme.onSurfaceVariant,
                        )
                    }
                }
                items(snapshot.targets, key = { it.zoneId.id }) { result ->
                    TargetZoneCard(
                        result = result,
                        use24Hour = state.preferences.use24Hour,
                        live = state.preferences.liveMode,
                        favorite = result.zoneId.id in state.preferences.favoriteZoneIds,
                        first = state.preferences.targetZoneIds.indexOf(result.zoneId.id) == 0,
                        last = state.preferences.targetZoneIds.indexOf(result.zoneId.id) ==
                            state.preferences.targetZoneIds.lastIndex,
                        canRemove = state.preferences.targetZoneIds.size > 1,
                        onFavorite = { viewModel.toggleFavorite(result.zoneId.id) },
                        onMoveUp = { viewModel.moveTarget(result.zoneId.id, -1) },
                        onMoveDown = { viewModel.moveTarget(result.zoneId.id, 1) },
                        onRemove = { viewModel.removeTarget(result.zoneId.id) },
                    )
                }
                item {
                    OutlinedButton(
                        modifier = Modifier.fillMaxWidth(),
                        enabled = state.preferences.targetZoneIds.size < 8,
                        onClick = { pickerMode = ZonePickerMode.ADD_TARGET },
                    ) { Text(if (state.preferences.targetZoneIds.size < 8) "+ Add timezone" else "Maximum 8 zones") }
                }
                item {
                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        horizontalArrangement = Arrangement.spacedBy(8.dp),
                    ) {
                        Button(
                            modifier = Modifier.weight(1f),
                            onClick = {
                                clipboard.setText(
                                    AnnotatedString(
                                        TimeConversionService.formatCopyMultiline(
                                            snapshot,
                                            state.preferences.use24Hour,
                                            state.preferences.liveMode,
                                        )
                                    )
                                )
                                snackbarMessage = "Conversion copied"
                            },
                        ) { Text("Copy") }
                        OutlinedButton(
                            modifier = Modifier.weight(1f),
                            onClick = {
                                val text = TimeConversionService.formatCopyOneLine(
                                    snapshot,
                                    state.preferences.use24Hour,
                                )
                                val share = android.content.Intent(android.content.Intent.ACTION_SEND).apply {
                                    type = "text/plain"
                                    putExtra(android.content.Intent.EXTRA_TEXT, text)
                                }
                                context.startActivity(
                                    android.content.Intent.createChooser(share, "Share conversion")
                                )
                            },
                        ) { Text("Share") }
                    }
                }
                item { Spacer(Modifier.height(20.dp)) }
            }
        }
    }

    pickerMode?.let { mode ->
        ZonePickerSheet(
            title = if (mode == ZonePickerMode.ADD_TARGET) "Add timezone" else "Convert from",
            favorites = state.preferences.favoriteZoneIds,
            excluded = if (mode == ZonePickerMode.ADD_TARGET) state.preferences.targetZoneIds.toSet() else emptySet(),
            onToggleFavorite = viewModel::toggleFavorite,
            onSelect = { option ->
                if (mode == ZonePickerMode.ADD_TARGET) viewModel.addTarget(option.id)
                else viewModel.setReverseSource(option.id)
                pickerMode = null
            },
            onDismiss = { pickerMode = null },
        )
    }

    if (settingsVisible) {
        SettingsSheet(
            state = state,
            onUse24Hour = viewModel::setUse24Hour,
            onTheme = viewModel::setTheme,
            onDismiss = { settingsVisible = false },
        )
    }

    if (state.preferencesLoaded && !state.preferences.hasSeenOnboarding) {
        AlertDialog(
            onDismissRequest = viewModel::markOnboardingSeen,
            title = { Text("Welcome to ZoneShift") },
            text = {
                Text(
                    "Use Live now for an at-a-glance world clock, or choose Custom to plan a time. " +
                        "Tap a star to keep a timezone at the top of search."
                )
            },
            confirmButton = {
                Button(onClick = viewModel::markOnboardingSeen) { Text("Get started") }
            },
        )
    }
}

@Composable
private fun PrimaryTimeCard(state: ConverterUiState, viewModel: ConverterViewModel) {
    val snapshot = requireNotNull(state.snapshot)
    val context = LocalContext.current
    val primary = snapshot.primary
    val source = ZoneCatalog.optionFor(snapshot.sourceZoneId.id)
    val dateFormatter = DateTimeFormatter.ofPattern("EEE, d MMM yyyy", Locale.getDefault())
    Card(
        colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.primaryContainer),
        modifier = Modifier.fillMaxWidth(),
    ) {
        Column(Modifier.padding(18.dp), verticalArrangement = Arrangement.spacedBy(6.dp)) {
            Row(Modifier.fillMaxWidth(), verticalAlignment = Alignment.CenterVertically) {
                Column(Modifier.weight(1f)) {
                    Text(
                        if (state.preferences.direction == ConversionDirection.FROM_LOCAL) "My local time" else "My converted time",
                        style = MaterialTheme.typography.labelLarge,
                    )
                    Text(
                        "${ZoneCatalog.optionFor(ZoneId.systemDefault().id).label} · ${TimeConversionService.formatOffset(primary.utcOffset)}",
                        style = MaterialTheme.typography.bodySmall,
                    )
                }
                if (state.preferences.liveMode) {
                    Text("● LIVE", color = MaterialTheme.colorScheme.primary, fontWeight = FontWeight.Bold)
                }
            }
            Text(
                TimeConversionService.formatDigital(
                    primary.localDateTime,
                    state.preferences.use24Hour,
                    state.preferences.liveMode,
                ),
                fontFamily = FontFamily.Monospace,
                fontWeight = FontWeight.Bold,
                fontSize = 38.sp,
                color = MaterialTheme.colorScheme.primary,
            )
            Text(primary.localDateTime.format(dateFormatter), style = MaterialTheme.typography.bodyMedium)
            if (state.preferences.direction == ConversionDirection.TO_LOCAL) {
                Text(
                    "Input: ${source.label} · ${TimeConversionService.formatDigital(snapshot.inputWallTime, state.preferences.use24Hour, state.preferences.liveMode)}",
                    style = MaterialTheme.typography.bodySmall,
                )
            }
            if (!state.preferences.liveMode) {
                HorizontalDivider(Modifier.padding(vertical = 4.dp))
                Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                    OutlinedButton(
                        modifier = Modifier.weight(1f),
                        onClick = {
                            val date = state.customDateTime.toLocalDate()
                            android.app.DatePickerDialog(
                                context,
                                { _, year, month, day -> viewModel.setCustomDate(LocalDate.of(year, month + 1, day)) },
                                date.year,
                                date.monthValue - 1,
                                date.dayOfMonth,
                            ).show()
                        },
                    ) { Text(state.customDateTime.toLocalDate().format(DateTimeFormatter.ofPattern("d MMM yyyy"))) }
                    OutlinedButton(
                        modifier = Modifier.weight(1f),
                        onClick = {
                            val time = state.customDateTime.toLocalTime()
                            android.app.TimePickerDialog(
                                context,
                                { _, hour, minute -> viewModel.setCustomTime(LocalTime.of(hour, minute)) },
                                time.hour,
                                time.minute,
                                state.preferences.use24Hour,
                            ).show()
                        },
                    ) { Text(TimeConversionService.formatDigital(state.customDateTime, state.preferences.use24Hour, false)) }
                }
                Text("Interpreted in ${source.label}", style = MaterialTheme.typography.labelSmall)
            }
        }
    }
}

@Composable
private fun TargetZoneCard(
    result: ZoneConversionResult,
    use24Hour: Boolean,
    live: Boolean,
    favorite: Boolean,
    first: Boolean,
    last: Boolean,
    canRemove: Boolean,
    onFavorite: () -> Unit,
    onMoveUp: () -> Unit,
    onMoveDown: () -> Unit,
    onRemove: () -> Unit,
) {
    val date = result.localDateTime.format(DateTimeFormatter.ofPattern("EEE d MMM", Locale.getDefault()))
    Card(modifier = Modifier.fillMaxWidth()) {
        Column(Modifier.padding(horizontal = 14.dp, vertical = 12.dp)) {
            Row(verticalAlignment = Alignment.CenterVertically) {
                Column(Modifier.weight(1f)) {
                    Text(result.label, fontWeight = FontWeight.SemiBold)
                    Text(
                        "${result.abbreviation} · ${TimeConversionService.formatOffset(result.utcOffset)}",
                        style = MaterialTheme.typography.bodySmall,
                        color = MaterialTheme.colorScheme.onSurfaceVariant,
                    )
                }
                TextButton(
                    modifier = Modifier.semantics {
                        contentDescription = if (favorite) "Remove ${result.label} from favourites" else "Add ${result.label} to favourites"
                    },
                    onClick = onFavorite,
                ) { Text(if (favorite) "★" else "☆", fontSize = 24.sp) }
                Column(horizontalAlignment = Alignment.End) {
                    Text(
                        TimeConversionService.formatDigital(result.localDateTime, use24Hour, live),
                        fontFamily = FontFamily.Monospace,
                        fontWeight = FontWeight.Bold,
                        fontSize = 22.sp,
                        color = MaterialTheme.colorScheme.primary,
                    )
                    Text(
                        listOf(TimeConversionService.formatDayDelta(result.dayDeltaFromPrimary), date)
                            .filter { it.isNotEmpty() }.joinToString(" · "),
                        style = MaterialTheme.typography.labelSmall,
                    )
                }
            }
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.End,
            ) {
                TextButton(enabled = !first, onClick = onMoveUp) { Text("Up") }
                TextButton(enabled = !last, onClick = onMoveDown) { Text("Down") }
                TextButton(enabled = canRemove, onClick = onRemove) { Text("Remove") }
            }
        }
    }
}

@Composable
private fun SourceZoneButton(zoneId: String, onClick: () -> Unit) {
    val option = ZoneCatalog.optionFor(zoneId)
    OutlinedButton(modifier = Modifier.fillMaxWidth(), onClick = onClick) {
        Column(Modifier.fillMaxWidth()) {
            Text("Input timezone", style = MaterialTheme.typography.labelSmall)
            Text("${option.label} · ${option.id}", maxLines = 1, overflow = TextOverflow.Ellipsis)
        }
    }
}

@Composable
private fun WarningCard(warning: String) {
    Card(
        modifier = Modifier.fillMaxWidth(),
        colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.errorContainer),
    ) {
        Column(Modifier.padding(14.dp)) {
            Text("Daylight-saving adjustment", fontWeight = FontWeight.Bold)
            Text(warning, style = MaterialTheme.typography.bodyMedium)
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun ZonePickerSheet(
    title: String,
    favorites: Set<String>,
    excluded: Set<String>,
    onToggleFavorite: (String) -> Unit,
    onSelect: (ZoneOption) -> Unit,
    onDismiss: () -> Unit,
) {
    var query by rememberSaveable { mutableStateOf("") }
    val results = remember(query, favorites, excluded) {
        ZoneCatalog.search(query, favorites).filterNot { it.id in excluded }
    }
    ModalBottomSheet(onDismissRequest = onDismiss) {
        Column(
            modifier = Modifier.fillMaxWidth().fillMaxHeight(0.9f).padding(horizontal = 16.dp),
        ) {
            Text(title, style = MaterialTheme.typography.headlineSmall, fontWeight = FontWeight.Bold)
            Spacer(Modifier.height(10.dp))
            OutlinedTextField(
                value = query,
                onValueChange = { query = it },
                modifier = Modifier.fillMaxWidth(),
                singleLine = true,
                label = { Text("Search city, region, or abbreviation") },
            )
            Spacer(Modifier.height(8.dp))
            LazyColumn(modifier = Modifier.fillMaxSize()) {
                items(results, key = { it.id }) { option ->
                    Row(
                        modifier = Modifier.fillMaxWidth().clickable { onSelect(option) }.padding(vertical = 10.dp),
                        verticalAlignment = Alignment.CenterVertically,
                    ) {
                        Column(Modifier.weight(1f)) {
                            Text(option.label, fontWeight = FontWeight.Medium)
                            Text(
                                "${option.shortLabel} · ${option.id}",
                                style = MaterialTheme.typography.bodySmall,
                                color = MaterialTheme.colorScheme.onSurfaceVariant,
                            )
                        }
                        TextButton(
                            modifier = Modifier.semantics {
                                contentDescription = if (option.id in favorites) "Remove favourite" else "Add favourite"
                            },
                            onClick = { onToggleFavorite(option.id) },
                        ) { Text(if (option.id in favorites) "★" else "☆", fontSize = 24.sp) }
                    }
                    HorizontalDivider()
                }
            }
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun SettingsSheet(
    state: ConverterUiState,
    onUse24Hour: (Boolean) -> Unit,
    onTheme: (String) -> Unit,
    onDismiss: () -> Unit,
) {
    ModalBottomSheet(onDismissRequest = onDismiss) {
        Column(
            modifier = Modifier.fillMaxWidth().padding(horizontal = 20.dp, vertical = 8.dp),
            verticalArrangement = Arrangement.spacedBy(16.dp),
        ) {
            Text("Settings", style = MaterialTheme.typography.headlineSmall, fontWeight = FontWeight.Bold)
            Row(Modifier.fillMaxWidth(), verticalAlignment = Alignment.CenterVertically) {
                Column(Modifier.weight(1f)) {
                    Text("24-hour time", fontWeight = FontWeight.Medium)
                    Text("Use 18:30 instead of 06:30 PM", style = MaterialTheme.typography.bodySmall)
                }
                Switch(checked = state.preferences.use24Hour, onCheckedChange = onUse24Hour)
            }
            Column {
                Text("Theme", fontWeight = FontWeight.Medium)
                ChipRow {
                    listOf("Studio", "Classic", "Night Ops", "Meridian").forEach { theme ->
                        FilterChip(
                            selected = state.preferences.theme == theme,
                            onClick = { onTheme(theme) },
                            label = { Text(theme) },
                        )
                    }
                }
            }
            Text(
                "ZoneShift works fully offline. No location permission or account is required.",
                style = MaterialTheme.typography.bodySmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant,
            )
            Spacer(Modifier.height(24.dp))
        }
    }
}

@Composable
private fun ChipRow(content: @Composable RowScope.() -> Unit) {
    Row(
        modifier = Modifier.fillMaxWidth(),
        horizontalArrangement = Arrangement.spacedBy(8.dp),
        content = content,
    )
}

@Composable
private fun SectionLabel(text: String, modifier: Modifier = Modifier) {
    Text(
        text,
        modifier = modifier,
        style = MaterialTheme.typography.labelMedium,
        fontWeight = FontWeight.Bold,
        color = MaterialTheme.colorScheme.primary,
    )
}
