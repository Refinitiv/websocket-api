
dependencies {
    implementation("org.apache.httpcomponents:httpclient:4.5.3")
    implementation("commons-cli:commons-cli:1.3") 
    implementation("org.json:json:20160810")
    implementation("com.neovisionaries:nv-websocket-client:1.30")
}

repositories {
    mavenCentral()
}

plugins {
    java
    application
}

java {
    sourceCompatibility = JavaVersion.VERSION_17
    targetCompatibility = JavaVersion.VERSION_17
}

application {
    mainClass.set(System.getProperty("mainClass","MarketPrice"))
}

sourceSets {
    named("main") {
        java.srcDir(".")
    }
}