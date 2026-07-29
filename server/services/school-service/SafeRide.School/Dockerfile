# syntax=docker/dockerfile:1
# Build context = REPO ROOT (so it can reach Directory.Build.props)

# =========================== STAGE 1: build ================================

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

ARG SVC=server/services/school-service/SafeRide.School

# --- 1a. Copy ONLY the project files first, then restore -----------------

COPY Directory.Build.props ./
COPY ${SVC}/src/SafeRide.Schools.Domain/SafeRide.Schools.Domain.csproj                 ${SVC}/src/SafeRide.Schools.Domain/
COPY ${SVC}/src/SafeRide.Schools.Application/SafeRide.Schools.Application.csproj       ${SVC}/src/SafeRide.Schools.Application/
COPY ${SVC}/src/SafeRide.Schools.Infrastructure/SafeRide.Schools.Infrastructure.csproj ${SVC}/src/SafeRide.Schools.Infrastructure/
COPY ${SVC}/src/SafeRide.Schools.Api/SafeRide.Schools.Api.csproj                       ${SVC}/src/SafeRide.Schools.Api/


RUN dotnet restore ${SVC}/src/SafeRide.Schools.Api/SafeRide.Schools.Api.csproj

# --- 1b. Now copy source and publish -------------------------------------

COPY ${SVC}/ ${SVC}/
RUN dotnet publish ${SVC}/src/SafeRide.Schools.Api/SafeRide.Schools.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

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

ENTRYPOINT ["dotnet", "SafeRide.Schools.Api.dll"]