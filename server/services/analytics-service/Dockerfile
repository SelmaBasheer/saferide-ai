FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

# QuestPDF renders through SkiaSharp, which needs fontconfig. The aspnet image
# ships without it, and without this line every PDF request throws at runtime —
# on Linux only, so it would pass on your machine and fail in the container.
RUN apt-get update \
    && apt-get install -y --no-install-recommends libfontconfig1 \
    && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["SafeRide.Analytics/SafeRide.Analytics.csproj", "SafeRide.Analytics/"]
RUN dotnet restore "SafeRide.Analytics/SafeRide.Analytics.csproj"
COPY . .
WORKDIR "/src/SafeRide.Analytics"
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SafeRide.Analytics.dll"]