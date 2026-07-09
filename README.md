# ZoneShift

A polished Windows desktop app that converts a time in **your PC timezone** into up to **8 other timezones** - or the other way around - with digital clock readouts and persistent preferences.

## Features

- **Auto-detects** your Windows local timezone
- **Live now** or enter a custom **date** and **time**
- **Add or remove** target timezones (1-8) - search, favorites, keep only what you need
- Convert **From my zone** or **To my zone**
- **Digital clock** display with **12-hour / 24-hour** toggle
- **Desktop overlay** - sticky always-on-top mini view (lock, compact, opacity)
- **Copy** multi-line or one-line (chat) conversion results
- **DST warnings** when a local time is invalid (spring-forward) or ambiguous (fall-back)
- First-run **Tips** dialog; optional **close minimizes to tray**
- **Remembers** zones, format, overlay, and window position between launches
- **System tray** - close/minimize can keep it running; right-click tray icon -> **Exit** to quit
- **Auto-update** check from GitHub Releases (x64 / arm64 installers)
- Shows when a converted time falls on the previous/next day

## Requirements

- Windows 10/11
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (to build)

## Run

```powershell
cd C:\Users\davej\source\TimezoneConverter
dotnet run
```

Or launch:

`C:\Users\davej\source\TimezoneConverter\bin\Release\net10.0-windows\ZoneShift.exe`

## Build

```powershell
dotnet publish -c Release -r win-x64 --self-contained false
```

## Installer (single .exe)

Creates a Windows setup program (self-contained - no .NET install required on the target PC):

```powershell
cd C:\Users\davej\source\TimezoneConverter
powershell -ExecutionPolicy Bypass -File .\pack-installer.ps1
```

Output (dual architecture):

- `dist\ZoneShift-Setup-1.6.2-x64.exe`
- `dist\ZoneShift-Setup-1.6.2-arm64.exe`

The installer:
- Per-user install under `%LocalAppData%\Programs\ZoneShift` (no admin required)
- Optional **desktop shortcut** and **start with Windows**
- **Start Menu** entry and uninstaller
- Self-contained (~60+ MB setup; no .NET install needed on the target PC)

Tag releases (`v1.6.2`) also build installers via GitHub Actions (`.github/workflows/release.yml`).

### Themes

**View → Theme**:
- **Studio** — dark newsroom wall, amber LEDs
- **Classic** — original light UI, indigo accents
- **Neon Pulse** — cyberpunk cyan clocks, magenta accents


Requires [Inno Setup 6](https://jrsoftware.org/isinfo.php) to rebuild (`winget install JRSoftware.InnoSetup`).

## Preferences

Settings are stored at:

`%AppData%\ZoneShift\settings.json`

(If you used the older **Timezone Converter** name, settings are migrated automatically.)

They update automatically when you change target zones, direction, or the 12h/24h format.

## Notes

- **CST** means US Central Time. China Standard Time is listed as **CST-CN**.
- Timezone data comes from Windows (`TimeZoneInfo`).


