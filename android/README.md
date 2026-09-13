# ZoneShift for Android

Native Android version of ZoneShift, built with Kotlin and Jetpack Compose. It lives beside the
Windows application and does not change the existing .NET solution.

## Included features

- Live-now and custom date/time conversion
- Convert from the phone's timezone or from another selected timezone
- One to eight ordered target timezones
- Searchable IANA timezone catalog with curated common zones
- Favourites pinned to the top of timezone search
- Explicit DST gap and overlap handling with user-facing warnings
- Previous-day and next-day labels
- 12-hour and 24-hour formats
- Multiline clipboard output and compact Android sharing
- Studio, Classic, Night Ops, and Meridian themes
- Preferences stored locally with Android DataStore
- Offline operation with no account, network, or location permission

Desktop-only tray, window-placement, updater, and always-on-top overlay settings are intentionally
not part of the Android app. Google Play supplies updates. A home-screen widget should be evaluated
separately because Android limits ordinary periodic widget refreshes.

## Requirements

- Android Studio with JDK 17
- Android SDK 35
- Android 8.0/API 26 or newer device or emulator

Android Studio normally configures the SDK path automatically. For command-line builds, set
`ANDROID_HOME` or create an uncommitted `local.properties` containing your SDK path.

## Open and run

1. Open the `android` directory in Android Studio.
2. Allow the Gradle sync to complete.
3. Select the `app` run configuration.
4. Choose an API 26+ emulator or connected phone and press Run.

## Command-line verification

From the `android` directory:

```powershell
$env:JAVA_HOME = 'C:\Program Files\Android\Android Studio\jbr'
$env:ANDROID_HOME = "$env:LOCALAPPDATA\Android\Sdk"
.\gradlew.bat testDebugUnitTest lintDebug assembleDebug
```

The debug APK is written to:

```text
app/build/outputs/apk/debug/app-debug.apk
```

For the unsigned, minified release bundle:

```powershell
.\gradlew.bat bundleRelease
```

Configure a private release signing key before uploading the resulting app bundle to Google Play.
Do not commit signing passwords or keystore files.

## Project structure

```text
app/src/main/java/com/zoneshift/app/
├── MainActivity.kt             Compose screen and mobile interactions
├── ConverterViewModel.kt       Screen state and one-second live refresh
├── data/
│   └── SettingsRepository.kt   DataStore-backed preferences
├── domain/
│   ├── ConversionModels.kt
│   ├── TimeConversionService.kt
│   ├── TimeParser.kt
│   └── ZoneCatalog.kt
└── ui/theme/
    └── ZoneShiftTheme.kt
```

Conversion code uses `Instant` for the canonical moment and IANA `ZoneId` values such as
`Europe/London`. User-entered custom values remain `LocalDateTime` until the selected source
timezone resolves them. DST gaps advance to the next valid wall time; overlaps select the earlier,
typically daylight-saving occurrence, matching the Windows app's established behavior.

## Before Play Store release

- Replace `com.zoneshift.app` if a different permanent application ID is required.
- Create a release keystore and configure secure signing outside source control.
- Build an Android App Bundle with `bundleRelease`.
- Supply final phone/tablet screenshots, store copy, feature graphic, privacy policy, and Data Safety responses.
- Test the signed bundle through Play internal testing on several physical devices.
- Recheck the target API requirement immediately before submission.
