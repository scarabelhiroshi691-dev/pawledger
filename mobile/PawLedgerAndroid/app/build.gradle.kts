plugins {
    id("com.android.application")
}

android {
    namespace = "com.pawledger.staff"
    compileSdk = 35

    defaultConfig {
        applicationId = "com.pawledger.staff"
        minSdk = 26
        targetSdk = 35
        versionCode = 130
        versionName = "1.3.0"
    }

    buildTypes {
        release {
            isMinifyEnabled = false
            proguardFiles(getDefaultProguardFile("proguard-android-optimize.txt"), "proguard-rules.pro")
        }
    }

    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_17
        targetCompatibility = JavaVersion.VERSION_17
    }
}
