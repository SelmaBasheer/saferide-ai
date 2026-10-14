<#
Builds and pushes every SafeRide image to ACR.

The build context and Dockerfile path for each service are taken from
docker-compose.yml, so this matches exactly how you build locally.

Usage:
  .\deploy\scripts\build-push.ps1                      # all eleven
  .\deploy\scripts\build-push.ps1 -Only identity-service, gateway
#>

param(
    [string]$Registry = "saferidedevacr30.azurecr.io",
    [string]$Tag = "v1",
    [string[]]$Only = @()
)

$ErrorActionPreference = "Stop"

# Some services build from the repo root because their Dockerfile copies
# shared project files; others build from their own folder. Both patterns
# exist in docker-compose.yml and are preserved here.
$services = @(
    @{ name = "gateway";              context = "server/services/gateway-service/SafeRide.Gateway"; dockerfile = "server/services/gateway-service/SafeRide.Gateway/Dockerfile" }
    @{ name = "identity-service";     context = ".";                                                dockerfile = "server/services/identity-service/SafeRide.Identity/Dockerfile" }
    @{ name = "school-service";       context = ".";                                                dockerfile = "server/services/school-service/SafeRide.School/Dockerfile" }
    @{ name = "driver-service";       context = ".";                                                dockerfile = "server/services/driver-service/Dockerfile" }
    @{ name = "tracking-service";     context = ".";                                                dockerfile = "server/services/tracking-service/Dockerfile" }
    @{ name = "ai-service";           context = ".";                                                dockerfile = "server/services/ai-service/SafeRide.Ai/Dockerfile" }
    @{ name = "analytics-service";    context = "server/services/analytics-service";                dockerfile = "server/services/analytics-service/Dockerfile" }
    @{ name = "notification-service"; context = "server/services/notification-service/notification"; dockerfile = "server/services/notification-service/notification/Dockerfile" }
    @{ name = "bus-service";          context = "server/services/bus-service/bus";                  dockerfile = "server/services/bus-service/bus/Dockerfile" }
    @{ name = "route-service";        context = "server/services/route-service/route";              dockerfile = "server/services/route-service/route/Dockerfile" }
    @{ name = "student-service";      context = "server/services/student-service/student";          dockerfile = "server/services/student-service/student/Dockerfile" }
)

if ($Only.Count -gt 0) {
    $services = $services | Where-Object { $Only -contains $_.name }
}

Write-Host "Logging in to $Registry..." -ForegroundColor Cyan
az acr login --name $Registry.Split(".")[0]

foreach ($s in $services) {
    $image = "$Registry/$($s.name):$Tag"
    Write-Host "`n=== $($s.name) ===" -ForegroundColor Cyan

    docker build -t $image -f $s.dockerfile $s.context
    if ($LASTEXITCODE -ne 0) { throw "Build failed for $($s.name)" }

    docker push $image
    if ($LASTEXITCODE -ne 0) { throw "Push failed for $($s.name)" }
}

Write-Host "`nDone. $($services.Count) image(s) pushed." -ForegroundColor Green