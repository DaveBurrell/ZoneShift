# ZoneShift 1.7.0 — Four-theme consolidation

Date: 2026-07-10

## Problem

ZoneShift 1.6.3 shipped three themes (Studio, Classic, Neon Pulse). An uncommitted working copy
had added four more from a design sheet (`docs/2026-07-10_04-34-41.PNG`), bringing the total to
seven — a set that was broad rather than refined, with overlapping character and no shared rules.

Underneath the theme sprawl sat a structural defect. The palette exposed `ClockFore`, a color
tuned for glowing digits on near-black glass, and four call sites reused it as an ordinary text
color on light card surfaces:

| Call site | What it colors |
| --- | --- |
| `MainForm.cs` brand title | wordmark on `HeaderBack` |
| `MainForm.cs` wall section label | `WORLD CLOCK WALL` on `CardFace` |
| `OverlayForm.cs` local time label | overlay clock digits on `CardFace` |
| `OverlayForm.cs` row time | overlay clock digits on `CardFace` |

On the light Classic theme this rendered emerald `rgb(52,211,153)` on white at **1.92:1** — far
below the 4.5:1 WCAG AA minimum for body text. The overlay is the always-on-top mini clock pinned
during meetings, so its entire content was affected.

## Decision

Consolidate to **four** themes: two light, two dark, four separable accent hues. Every theme keeps
a dark LED glass, so `LedClockDisplay` needs no second render path.

| # | Name | Chrome | Accent | LED digits |
| --- | --- | --- | --- | --- |
| 0 | Studio *(default)* | dark slate | amber | amber |
| 1 | Classic | light cool-gray | indigo | emerald |
| 2 | Night Ops | dark terminal | phosphor green | phosphor green |
| 3 | Meridian | light warm-white | teal | coral |

`Classic` adopts the design sheet's cleanup: a light brand strip rather than a saturated indigo
slab, which is what makes the wordmark legible.

`Meridian` is the renamed `Timeline`. The design sheet's panel 1d depicted an aligned hour-strip
layout with a "now" line; no such layout exists in the code, so shipping a palette under that name
would promise something it does not deliver. Meridians are longitude lines, which suits a timezone
app and describes only what is there.

Retired: `NeonPulse` (shipped in 1.6.x), plus `ClassicRefined`, `ModernMinimal`, and `Timeline`
(never released).

### Rejected alternative

Adopting the design sheet verbatim would have required a flat-clock render path in
`LedClockDisplay` for panel 1b, whose `ClockFore = rgb(28,28,30)` and `ClockCore = rgb(50,50,54)`
invert the module's contract — `UiPaint.DrawGlowText` draws `glow` beneath and `core` on top,
expecting the core to be *brighter*. Rendered as-is, 1b's digits become gray `#323236` with a
near-black smear one pixel below. It would also have dropped Studio, the current default.

## Token roles

Three new palette tokens end the `ClockFore` overloading:

- `BrandText` — wordmark, on `HeaderBack`
- `SectionHeading` — section headings, on `CardFace`
- `ClockTextOnSurface` — clock digits drawn as flat text, on `CardFace`

`ClockFore` / `ClockCore` narrow to their real job: glowing digits on `GlassBack`.

`SectionHeading` currently equals `Accent` in all four palettes, which looks redundant but is not.
`Accent` must be dark enough to carry white text *on top of it*; `SectionHeading` must be dark
enough to read *as text on a light card*. Those constraints usually agree — until a theme wants a
light accent, at which point they must diverge. Keeping them separate means such a theme fails a
test instead of shipping.

A `Description` property also moves onto `ThemePalette`. The theme menu previously held its
tooltips in an array positionally coupled to `ThemePalette.All`, so reordering the list silently
mispaired every tooltip.

## Contrast, enforced by test

`ThemeContrastTests` computes WCAG 2.1 relative luminance and asserts a floor for every
text-on-surface pair across all four palettes.

| Pair | Floor |
| --- | --- |
| `TextPrimary`, `TextSecondary` on `CardFace` / `AppBackground` / `TileBack` | 4.5:1 |
| `SectionHeading` on `CardFace` | 4.5:1 |
| `TextOnAccent` on `Accent`; `SegmentActiveText` on `SegmentActive`; `SegmentIdleText` on `SegmentIdle` | 4.5:1 |
| `ClockFore`, `ClockCore`, `LedCaption` on `GlassBack` | 4.5:1 |
| `ClockTextOnSurface` on `CardFace` | 4.5:1 |
| `BrandText` on `HeaderBack` (15pt semibold — large text) | 3:1 |
| `TextMuted` on any surface (captions, hints) | 3:1 |

Two design-sheet colors failed and were corrected:

- Meridian's bright teal `rgb(0,168,168)` gives white label text only **2.93:1**. Pulled to
  teal-700 `rgb(0,128,128)` for 4.77:1.
- Meridian's muted text `rgb(130,155,162)` on white computes to **2.94:1**, missing the 3:1 floor
  by 0.06. Darkened to `rgb(118,142,150)` for 3.46:1.

Light themes darken the LED hue for flat text: Classic uses emerald-700 `rgb(4,120,87)`, Meridian
burnt coral `rgb(191,64,32)`.

## Migration

Theme preference persists by name, never by ordinal. `FromName` keeps resolving retired names,
pointed at the nearest survivor:

```csharp
"neon" or "neonpulse" or "neon pulse" or "cyber" => NightOps,
```

Neon Pulse was a dark chassis with saturated glowing digits; Night Ops is its closest surviving
relative. A user who chose it gets a dark electric theme on upgrade rather than being reset to
amber Studio.

Enum values were reused when themes were retired (`NightOps = 2` occupies Neon Pulse's old slot),
so `FromName` now rejects all-digit input before `Enum.TryParse` would coerce `"2"` into a theme
that no longer means what it once did.

## Defects fixed during verification

Screenshotting all four themes surfaced bugs that no assertion would have caught:

1. **Toolbar options overlapped.** `LayoutToolbar` branched on `Control.Visible`, whose getter
   returns *effective* visibility — always `false` while the form is being built. Both `Location`
   assignments were skipped, stranding `Overlay` and `Close to tray` on top of each other at the
   toolbar's origin until the first window resize.
2. **Timezone dropdowns rendered white on the dark themes.** An editable `ComboBox` hosts a native
   `EDIT` child that ignores `BackColor`. A combo forwards `WM_CTLCOLOREDIT` to *its parent*, so
   the containers (`ClockTilePanel`, `InputHostPanel`) answer it with the palette's input colors.
3. **Brand labels stamped flat rectangles over the header gradient.** An opaque label can only
   match a `HeaderTop → HeaderBottom` gradient on one scanline. The labels are now transparent.
4. **The hero clock's colon never blinked.** `ApplyColonBlink` returned its input unchanged on both
   paths, so a 500ms timer repainted the clock twice a second to no effect.
5. **The monospace font fallback never ran.** `new Font(name, ...)` silently substitutes a
   proportional face rather than throwing, so the `try`/`catch` chain always returned on its first
   iteration. It now probes with `FontFamily`, which does throw. The colon-blink fix depends on
   this: blanking `':'` only preserves layout in a fixed-advance font.
6. **The test suite destroyed the developer's settings.** `AppSettingsTests` called `Save()`
   against the real `%AppData%\ZoneShift\settings.json` and rotated the backup over it. `AppSettings`
   now honours a `ZONESHIFT_SETTINGS_DIR` override, which also lets the screenshot tooling drive
   each theme in a throwaway profile.

## Verification

- `dotnet test ZoneShift.sln -c Release` — 87 tests, all passing.
- `dotnet publish` for `win-x64` and `win-arm64`, self-contained single-file.
- Each theme launched in an isolated profile and captured via `PrintWindow`; images committed to
  `docs/themes/`. `PrintWindow` renders from the window's own device context, so a capture cannot
  pick up unrelated windows sitting on top of it.

## Known issues

WinForms draws a combo box's border from system colors rather than the control's palette, so the
timezone dropdowns keep a light 1px outline on the dark themes. Fixing it requires owner-drawing
the combo frame, which is out of scope for this change.
