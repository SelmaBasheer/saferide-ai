FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Shared build settings live at the repository root and apply to every project,
# so they have to come along or the container build differs from the local one.
COPY Directory.Build.props .

# csproj files first, restore, then source — so a code change doesn't re-download
# packages. Four projects, so four copies.
COPY server/services/ai-service/SafeRide.Ai/SafeRide.Ai.Domain/SafeRide.Ai.Domain.csproj server/services/ai-service/SafeRide.Ai/SafeRide.Ai.Domain/
COPY server/services/ai-service/SafeRide.Ai/SafeRide.Ai.Application/SafeRide.Ai.Application.csproj server/services/ai-service/SafeRide.Ai/SafeRide.Ai.Application/
COPY server/services/ai-service/SafeRide.Ai/SafeRide.Ai.Infrastructure/SafeRide.Ai.Infrastructure.csproj server/services/ai-service/SafeRide.Ai/SafeRide.Ai.Infrastructure/
COPY server/services/ai-service/SafeRide.Ai/SafeRide.Ai.Api/SafeRide.Ai.Api.csproj server/services/ai-service/SafeRide.Ai/SafeRide.Ai.Api/

RUN dotnet restore server/services/ai-service/SafeRide.Ai/SafeRide.Ai.Api/SafeRide.Ai.Api.csproj

COPY server/services/ai-service/SafeRide.Ai/ server/services/ai-service/SafeRide.Ai/

RUN dotnet publish server/services/ai-service/SafeRide.Ai/SafeRide.Ai.Api/SafeRide.Ai.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .

EXPOSE 8080

# appsettings binds to localhost, which inside a container means the container
# itself. Environment configuration wins, so this makes it reachable.
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "SafeRide.Ai.Api.dll"]