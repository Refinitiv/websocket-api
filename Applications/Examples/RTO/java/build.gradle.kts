plugins {
    java
}

java {
    sourceCompatibility = JavaVersion.VERSION_11
    targetCompatibility = JavaVersion.VERSION_11
}

repositories {
	mavenCentral()
}

subprojects {
    apply(plugin = "java")

	repositories {
		mavenCentral()
	}

    dependencies {
        implementation("org.apache.httpcomponents:httpclient:4.5.14")
        implementation("commons-cli:commons-cli:1.5.0")
        implementation("org.json:json:20220924")
        implementation("com.neovisionaries:nv-websocket-client:2.14")
        implementation("org.bitbucket.b_c:jose4j:0.9.3")
        implementation("ch.qos.logback:logback-classic:1.2.3")
        implementation("ch.qos.logback:logback-core:1.2.3")
		implementation("org.apache.commons:commons-lang3:3.13.0")
		implementation("commons-io:commons-io:2.11.0")
		implementation("org.apache.commons:commons-collections4:4.4")
		implementation("org.apache.commons:commons-math3:3.6.1")

    }
}