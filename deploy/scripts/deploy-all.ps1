<#
Deploys the data stores and every service to the cluster.

Each file in deploy/values is one service. The filename is the release name,
so adding a twelfth service means adding one file — no change here.

Usage:
  .\deploy\scripts\deploy-all.ps1
  .\deploy\scripts\deploy-all.ps1 -Only identity-service
#>

param(
    [string]$Namespace = "saferide",
    [string[]]$Only = @()
)

$ErrorActionPreference = "Stop"

kubectl get namespace $Namespace *> $null
if ($LASTEXITCODE -ne 0) { kubectl create namespace $Namespace }

Write-Host "Applying data stores..." -ForegroundColor Cyan
Get-ChildItem deploy\k8s\*.yaml | ForEach-Object {
    kubectl apply -f $_.FullName
}

Write-Host "`nDeploying services..." -ForegroundColor Cyan
$valueFiles = Get-ChildItem deploy\values\*.yaml

if ($Only.Count -gt 0) {
    $valueFiles = $valueFiles | Where-Object { $Only -contains $_.BaseName }
}

foreach ($f in $valueFiles) {
    # The filename is the release name, which the chart uses for the
    # Deployment, the Service, and the cluster DNS name other services use.
    $release = $f.BaseName
    Write-Host "  $release"

    helm upgrade --install $release deploy\helm\saferide-service `
        --namespace $Namespace `
        -f $f.FullName
    if ($LASTEXITCODE -ne 0) { throw "Helm failed for $release" }
}

Write-Host "`nDone." -ForegroundColor Green
kubectl get pods -n $Namespace