package com.zoneshift.app

import android.app.Application
import org.junit.Assert.assertTrue
import org.junit.Test

class ConverterViewModelConstructionTest {
    @Test
    fun defaultAndroidFactoryCanFindApplicationOnlyConstructor() {
        val hasExpectedConstructor = ConverterViewModel::class.java.constructors.any { constructor ->
            constructor.parameterTypes.contentEquals(arrayOf(Application::class.java))
        }

        assertTrue(
            "ConverterViewModel must expose an Application-only JVM constructor for viewModel()",
            hasExpectedConstructor,
        )
    }
}
