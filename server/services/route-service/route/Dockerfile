# ---- Build stage ----
FROM maven:3.9-eclipse-temurin-21 AS build
WORKDIR /app

COPY pom.xml .
RUN mvn -q -B dependency:go-offline

COPY src ./src

# Checkstyle is skipped here on purpose: its ruleset lives four directories up
# in the repository, which isn't in this build context. Linting belongs to CI.
RUN mvn -q -B clean package -DskipTests -Dcheckstyle.skip=true

# ---- Runtime stage ----
FROM eclipse-temurin:21-jre-alpine
WORKDIR /app

RUN addgroup -S app && adduser -S app -G app

COPY --from=build --chown=app:app /app/target/*.jar app.jar

USER app

# application.yaml says 5013, which is right on a developer's machine and wrong
# inside a container where every service can own 8080.
ENV SERVER_PORT=8080
EXPOSE 8080

ENTRYPOINT ["java", "-jar", "app.jar"]