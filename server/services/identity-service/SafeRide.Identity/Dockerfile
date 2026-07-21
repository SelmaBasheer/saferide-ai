# syntax=docker/dockerfile:1

# =========================== STAGE 1: build ================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

ARG SVC=server/services/identity-service/SafeRide.Identity

# --- 1a. Copy ONLY the project files first, then restore -----------------
COPY Directory.Build.props ./
COPY ${SVC}/SafeRide.Identity.Domain/SafeRide.Identity.Domain.csproj                 ${SVC}/SafeRide.Identity.Domain/
COPY ${SVC}/SafeRide.Identity.Application/SafeRide.Identity.Application.csproj       ${SVC}/SafeRide.Identity.Application/
COPY ${SVC}/SafeRide.Identity.Infrastructure/SafeRide.Identity.Infrastructure.csproj ${SVC}/SafeRide.Identity.Infrastructure/
COPY ${SVC}/SafeRide.Identity.Api/SafeRide.Identity.Api.csproj                       ${SVC}/SafeRide.Identity.Api/

RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet restore ${SVC}/SafeRide.Identity.Api/SafeRide.Identity.Api.csproj

# --- 1b. Now copy source and publish -------------------------------------
COPY ${SVC}/ ${SVC}/

RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet publish ${SVC}/SafeRide.Identity.Api/SafeRide.Identity.Api.csproj \
        --configuration Release \
        --no-restore \
        --output /app/publish \
        /p:UseAppHost=false

# =========================== STAGE 2: runtime ==============================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

RUN apt-get update \
 && apt-get install -y --no-install-recommends curl \
 && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true

COPY --from=build /app/publish .

RUN mkdir -p /app/logs && chown -R app:app /app

USER app
EXPOSE 8080

HEALTHCHECK --interval=15s --timeout=3s --start-period=40s --retries=5 \
    CMD curl -fsS http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "SafeRide.Identity.Api.dll"]