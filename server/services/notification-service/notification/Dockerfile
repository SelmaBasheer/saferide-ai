# ---- Build stage: compile + package the fat jar ----
FROM maven:3.9-eclipse-temurin-21 AS build
WORKDIR /app

# Copy pom first and pre-download dependencies.
# This layer is cached and only re-runs when pom.xml changes,
# so day-to-day code rebuilds are fast.
COPY pom.xml .
RUN mvn -q -B dependency:go-offline

# Now copy source and build (skip tests in the image build)
COPY src ./src
RUN mvn -q -B clean package -DskipTests

# ---- Runtime stage: just the JRE + the jar ----
FROM eclipse-temurin:21-jre-alpine
WORKDIR /app
COPY --from=build /app/target/notification.jar app.jar
EXPOSE 8080
ENTRYPOINT ["java", "-jar", "app.jar"]