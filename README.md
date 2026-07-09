# ZoneShift

A polished Windows desktop app that converts a time in **your PC timezone** into up to **5 other timezones** — or the other way around — with digital clock readouts and persistent preferences.

## Features

- **Auto-detects** your Windows local timezone
- Enter a **date** and **time**
- **Add or remove** target timezones (1–8) — keep only what you need
- Convert **From my zone** or **To my zone**
- **Digital clock** display with **12-hour / 24-hour** toggle
- **Desktop overlay** — sticky always-on-top mini view of your time + conversions
- **Remembers** zones, format, and overlay position between launches
- **Lives in the system tray** — close or minimize keeps it running; right-click tray icon → **Exit** to quit
- Daylight Saving Time handled by Windows
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

Creates a Windows setup program (self-contained — no .NET install required on the target PC):

```powershell
cd C:\Users\davej\source\TimezoneConverter
powershell -ExecutionPolicy Bypass -File .\pack-installer.ps1
```

Output:

`C:\Users\davej\source\TimezoneConverter\dist\ZoneShift-Setup-1.1.0.exe`

The installer:
- Installs under `Program Files` or per-user (standard wizard)
- Optional **desktop shortcut**
- Optional **start with Windows**
- Adds **Start Menu** entry and uninstaller
- Bundles the full app (~64 MB setup)

Requires [Inno Setup 6](https://jrsoftware.org/isinfo.php) to rebuild (`winget install JRSoftware.InnoSetup`).

## Preferences

Settings are stored at:

`%AppData%\ZoneShift\settings.json`

(If you used the older **Timezone Converter** name, settings are migrated automatically.)

They update automatically when you change target zones, direction, or the 12h/24h format.

## Notes

- **CST** means US Central Time. China Standard Time is listed as **CST-CN**.
- Timezone data comes from Windows (`TimeZoneInfo`).
