FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# csproj first, restore, then source — so a code change doesn't re-download packages
COPY SafeRide.Gateway.csproj .
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .

EXPOSE 8080

# appsettings binds to localhost, which inside a container means the container
# itself. Environment configuration wins, so this makes it reachable.
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "SafeRide.Gateway.dll"]