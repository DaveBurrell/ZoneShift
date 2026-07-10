# Changelog

All notable changes to ZoneShift are documented here.

## 1.7.1

### Fix
- **The saved theme is now applied before any control is built.** Controls declared as fields are constructed before a constructor body runs, so they cached colors from the default palette regardless of which theme was saved. On the light themes this left dark wedges at the segmented-toggle corners and around the hero clock's bezel
- **The date field is themed.** `DateTimePicker` is a native control with no dark mode — unaffected by `Application.SetColorMode` and by `SetWindowTheme` — so it rendered as a white box on Studio and Night Ops. Replaced with `ThemedDateBox`, which paints from the palette and keeps typing, arrow-key stepping, and a calendar drop-down
- **Check box glyphs are painted from the palette.** WinForms draws them from `SystemColors`, which rendered as white boxes on the dark themes. Their tick now uses the theme's accent rather than Windows' blue
- **The timezone field no longer opens showing a full-width selection block**

### Themes
- Native chrome now follows the theme: scroll bars, menus, dialogs, combo borders, and the window title bar. Switching between a light and a dark theme re-applies it live, with no restart
- The `TIME` and `DATE` fields share a muted surface while live mode disables them, instead of one looking editable

### Internal
- `Program` resolves the theme and color mode before constructing `MainForm`; `MainForm` now takes its `AppSettings`
- `SystemTheming` centralises the process color mode and the title-bar attribute
- Tests reach internal UI helpers via `InternalsVisibleTo` rather than widening the public API

## 1.7.0

### Themes
- Consolidated to **four refined themes**: Studio, Classic, Night Ops, Meridian — two light, two dark, each with a distinct accent and LED hue
- **Neon Pulse retired.** Existing Neon Pulse users are migrated to Night Ops, its closest surviving relative
- **Classic** now uses a light brand strip instead of an indigo slab, so the wordmark reads as text
- Theme menu tooltips now come from the palette itself, so they can no longer drift out of sync with the theme list

### Accessibility
- Fixed low-contrast text caused by reusing the LED digit color (`ClockFore`) as a chrome text color. On Classic this rendered the `WORLD CLOCK WALL` heading and the **desktop overlay's clock digits** as emerald on white at **1.92:1**
- Added `BrandText`, `SectionHeading`, and `ClockTextOnSurface` tokens so each text role carries a color contrast-checked for its own surface
- New `ThemeContrastTests` enforces WCAG 2.1 AA across all four palettes on every text-on-surface pairing

### Fix
- **Toolbar options no longer overlap.** `Overlay` and `Close to tray` rendered stacked on each other at the toolbar's top-left corner until the window was first resized. `LayoutToolbar` branched on `Control.Visible`, whose getter reports *effective* visibility — always `false` while the form is still being built — so both `Location` assignments were skipped
- **Timezone dropdowns are now themed on the dark themes.** An editable `ComboBox` hosts a native edit field that ignores `BackColor`; it rendered white on Studio and Night Ops. Its container now answers `WM_CTLCOLOREDIT` with the palette's input colors
- **The brand strip no longer stamps flat rectangles over its gradient.** The wordmark and tagline labels were opaque `HeaderBack`, which can only match a `HeaderTop → HeaderBottom` gradient on a single scanline
- The hero clock's **colon now actually blinks**. `ApplyColonBlink` returned its input unchanged on both paths, so a 500ms timer repainted the clock twice a second with no visible effect
- The LED clock's monospace font fallback never ran: `new Font(name, ...)` silently substitutes a proportional face rather than throwing, so an unknown family was never detected. It now probes with `FontFamily` first
- The live badge in the brand strip no longer starts at a hardcoded x-offset before its resize handler corrects it

### Internal
- `AppSettings` honours a `ZONESHIFT_SETTINGS_DIR` override. The test suite previously called `Save()` against the real `%AppData%\ZoneShift\settings.json`, so running tests destroyed the developer's own preferences and rotated the backup over them
- `UiTheme` fonts are allocated once instead of on every property read; four unused font properties removed
- Brand strip metrics moved into `LayoutMetrics`

### Known issues
- WinForms draws a combo box's border from system colors, so timezone dropdowns keep a light 1px outline on the dark themes

## 1.6.3

### Fix
- Preferences (zones, format, theme) **survive upgrades** — save before silent install, never overwrite disk zones with incomplete UI state, stronger combo restore

## 1.6.2

### Fix
- Silent auto-update now **relaunches ZoneShift** after install (was skipped under `/VERYSILENT`)

## 1.6.1

### UI polish
- Responsive toolbar (Copy pinned; options reflow / hide when narrow)
- Master field rhythm and dual-column date/time
- Theme-aware desktop overlay
- Footer link spacing; tooltips; denser card/tile padding

## 1.6.0

### UI
- Newsroom **clock wall** layout with custom LED modules
- **Theme picker** (View > Theme): Studio, Classic, Neon Pulse
- Contrast and spacing polish across themes
- About / Tips / Check updates in Help menu and footer

### Process
- Theme unit tests

## 1.5.0

### Polish
- First-run **Tips** dialog (also available via Tips link)
- **Close minimizes to tray** checkbox in options
- DST **invalid/ambiguous** local times show a clear status warning
- **Copy** multi-line (click) or one-line for chat (right-click menu)

### Process
- Expanded unit tests (DST classification, setup asset picking, copy formatting)
- CI workflow publishes installers on version tags

## 1.4.1

- Layout fix: leave PerMonitorV2; restore DpiUnawareGdiScaled for stable UI

## 1.4.0

- PerMonitorV2 attempt (reverted in 1.4.1)
- ARM64 + x64 installers
- Auto-update download + silent install

## 1.3.0

- Searchable timezones, favorites, copy results, overlay controls, update check

## 1.2.0

- Single-instance, settings backup/logs, About, conversion services, unit tests, CI

## 1.1.x

- .NET 10 upgrade, encoding fixes, Start Menu + persistence fixes

## 1.0.0

- Initial public release
