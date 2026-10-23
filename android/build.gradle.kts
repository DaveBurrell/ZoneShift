// AGP 9 provides built-in Kotlin support, so the org.jetbrains.kotlin.android plugin is no longer
// applied. AGP only depends on KGP 2.2.10; pin a newer Kotlin Gradle plugin on the build classpath.
// See https://developer.android.com/build/migrate-to-built-in-kotlin
buildscript {
    dependencies {
        classpath("org.jetbrains.kotlin:kotlin-gradle-plugin:2.4.21")
    }
}

plugins {
    id("com.android.application") version "9.4.1" apply false
    id("org.jetbrains.kotlin.plugin.compose") version "2.4.21" apply false
}
