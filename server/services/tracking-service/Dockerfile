# syntax=docker/dockerfile:1
# Build context = REPO ROOT (so it can reach Directory.Build.props)

# =========================== STAGE 1: build ================================

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

ARG SVC=server/services/tracking-service

COPY Directory.Build.props ./
COPY ${SVC}/SafeRide.Tracking/SafeRide.Tracking.csproj ${SVC}/SafeRide.Tracking/

RUN dotnet restore ${SVC}/SafeRide.Tracking/SafeRide.Tracking.csproj

# Only the service project. The test project shares this folder and has no place
# in a runtime image.
COPY ${SVC}/SafeRide.Tracking/ ${SVC}/SafeRide.Tracking/

RUN dotnet publish ${SVC}/SafeRide.Tracking/SafeRide.Tracking.csproj -c Release -o /app/publish /p:UseAppHost=false

# =========================== STAGE 2: runtime ==============================

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish .

RUN mkdir -p /app/logs && chown -R app:app /app

USER app
EXPOSE 8080

HEALTHCHECK --interval=15s --timeout=3s --start-period=40s --retries=5 \
    CMD curl -fsS http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "SafeRide.Tracking.dll"]