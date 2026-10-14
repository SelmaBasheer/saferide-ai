<#
Creates every Kubernetes Secret SafeRide needs, reading the values from
Doppler. Nothing sensitive is typed, and nothing sensitive is written to disk.

Safe to run repeatedly — each secret is rendered to YAML and applied, so an
existing secret is updated rather than erroring.

Usage:  .\deploy\scripts\sync-secrets.ps1
#>

param(
    [string]$Namespace = "saferide"
)

$ErrorActionPreference = "Stop"

Write-Host "Reading secrets from Doppler..." -ForegroundColor Cyan
$d = doppler secrets download --no-file --format json | ConvertFrom-Json

# kubectl has no "create or update" for secrets, so render the YAML with
# --dry-run=client and pipe it to apply, which does both.
function Set-Secret {
    param(
        [string]$Name,
        [hashtable]$Values
    )

    $literals = @()
    foreach ($key in $Values.Keys) {
        $literals += "--from-literal=$key=$($Values[$key])"
    }

    kubectl create secret generic $Name --namespace $Namespace @literals `
        --dry-run=client -o yaml | kubectl apply -f -
}

# Connection strings are assembled here rather than stored in Doppler, so the
# host names stay next to the Kubernetes Service names they refer to.
$pgConn = "Host=postgres;Port=5432;Database=$($d.POSTGRES_DB);Username=$($d.POSTGRES_USER);Password=$($d.POSTGRES_PASSWORD)"
$sqlConn = "Server=sqlserver,1433;User Id=sa;Password=$($d.MSSQL_SA_PASSWORD);TrustServerCertificate=True"
# The real Storage Account, read straight from Terraform's outputs so the name
# is never duplicated between the two.
$storageAccount = terraform -chdir=deploy/terraform output -raw storage_account_name
$azurite = az storage account show-connection-string `
    --name $storageAccount `
    --resource-group rg-saferide-dev `
    --query connectionString --output tsv

Set-Secret "postgres-secret" @{
    POSTGRES_USER     = $d.POSTGRES_USER
    POSTGRES_PASSWORD = $d.POSTGRES_PASSWORD
    POSTGRES_DB       = $d.POSTGRES_DB
}

Set-Secret "sqlserver-secret" @{
    MSSQL_SA_PASSWORD = $d.MSSQL_SA_PASSWORD
}

Set-Secret "mysql-secret" @{
    MYSQL_ROOT_PASSWORD = $d.MYSQL_ROOT_PASSWORD
}

Set-Secret "mongodb-secret" @{
    MONGO_INITDB_ROOT_USERNAME = $d.MONGO_ROOT_USERNAME
    MONGO_INITDB_ROOT_PASSWORD = $d.MONGO_ROOT_PASSWORD
}

Set-Secret "rabbitmq-secret" @{
    RABBITMQ_DEFAULT_USER = $d.RABBITMQ_USER
    RABBITMQ_DEFAULT_PASS = $d.RABBITMQ_PASSWORD
}

Set-Secret "identity-secret" @{
    "ConnectionStrings__Default" = $pgConn
    "RabbitMQ__Username"         = $d.RABBITMQ_USER
    "RabbitMQ__Password"         = $d.RABBITMQ_PASSWORD
    "JwtSettings__Secret"        = $d.JWT_SECRET
    "Seed__SuperAdminPassword"   = $d.SUPERADMIN_PASSWORD
    "AzureStorage__ConnectionString" = $azurite
}

Set-Secret "gateway-secret" @{
    "JwtSettings__Secret" = $d.JWT_SECRET
}

Set-Secret "school-secret" @{
    "ConnectionStrings__Default"     = "Server=sqlserver,1433;Database=SafeRide_School_Db;User Id=sa;Password=$($d.MSSQL_SA_PASSWORD);TrustServerCertificate=True"
    "RabbitMQ__Username"             = $d.RABBITMQ_USER
    "RabbitMQ__Password"             = $d.RABBITMQ_PASSWORD
    "AzureStorage__ConnectionString" = $azurite
    "Razorpay__KeyId"                = $d.RAZORPAY__KEYID
    "Razorpay__KeySecret"            = $d.RAZORPAY__KEYSECRET
    "Razorpay__WebhookSecret"        = $d.RAZORPAY__WEBHOOKSECRET
}

Set-Secret "driver-secret" @{
    "ConnectionStrings__DriverDb" = "Host=postgres;Port=5432;Database=saferide_driver;Username=$($d.POSTGRES_USER);Password=$($d.POSTGRES_PASSWORD)"
    "RabbitMQ__Username"          = $d.RABBITMQ_USER
    "RabbitMQ__Password"          = $d.RABBITMQ_PASSWORD
}

Set-Secret "tracking-secret" @{
    "Mongo__Username"    = $d.MONGO_ROOT_USERNAME
    "Mongo__Password"    = $d.MONGO_ROOT_PASSWORD
    "RabbitMQ__Username" = $d.RABBITMQ_USER
    "RabbitMQ__Password" = $d.RABBITMQ_PASSWORD
}

Set-Secret "ai-secret" @{
    "ConnectionStrings__AiDb" = "Server=sqlserver,1433;Database=SafeRide_Ai_Db;User Id=sa;Password=$($d.MSSQL_SA_PASSWORD);TrustServerCertificate=True"
    "RabbitMQ__Username"      = $d.RABBITMQ_USER
    "RabbitMQ__Password"      = $d.RABBITMQ_PASSWORD
    "Gemini__ApiKey"          = $d.GEMINI__APIKEY
}

Set-Secret "analytics-secret" @{
    "ConnectionStrings__Analytics" = "Server=sqlserver,1433;Database=SafeRide_Analytics_Db;User Id=sa;Password=$($d.MSSQL_SA_PASSWORD);TrustServerCertificate=True"
    "RabbitMQ__Username"           = $d.RABBITMQ_USER
    "RabbitMQ__Password"           = $d.RABBITMQ_PASSWORD
}

Set-Secret "notification-secret" @{
    SPRING_RABBITMQ_USERNAME = $d.RABBITMQ_USER
    SPRING_RABBITMQ_PASSWORD = $d.RABBITMQ_PASSWORD
    SENDGRID_API_KEY         = $d.SENDGRID_API_KEY
    SENDGRID_FROM_EMAIL      = $d.SENDGRID_FROM_EMAIL
    BREVO_API_KEY            = $d.BREVO_API_KEY
    BREVO_FROM_EMAIL         = $d.BREVO_FROM_EMAIL
    TWILIO_ACCOUNT_SID       = $d.TWILIO_ACCOUNT_SID
    TWILIO_AUTH_TOKEN        = $d.TWILIO_AUTH_TOKEN
    TWILIO_WHATSAPP_FROM     = $d.TWILIO_WHATSAPP_FROM
    ALERT_WHATSAPP_TO        = $d.ALERT_WHATSAPP_TO
    SUPERADMIN_EMAIL         = $d.SUPERADMIN_EMAIL
}

Set-Secret "bus-secret" @{
    MYSQL_ROOT_PASSWORD             = $d.MYSQL_ROOT_PASSWORD
    SPRING_RABBITMQ_USERNAME        = $d.RABBITMQ_USER
    SPRING_RABBITMQ_PASSWORD        = $d.RABBITMQ_PASSWORD
    AZURE_STORAGE_CONNECTION_STRING = $azurite
}

Set-Secret "route-secret" @{
    MONGO_USERNAME           = $d.MONGO_ROOT_USERNAME
    MONGO_PASSWORD           = $d.MONGO_ROOT_PASSWORD
    SPRING_RABBITMQ_USERNAME = $d.RABBITMQ_USER
    SPRING_RABBITMQ_PASSWORD = $d.RABBITMQ_PASSWORD
}

Set-Secret "student-secret" @{
    MYSQL_ROOT_PASSWORD      = $d.MYSQL_ROOT_PASSWORD
    SPRING_RABBITMQ_USERNAME = $d.RABBITMQ_USER
    SPRING_RABBITMQ_PASSWORD = $d.RABBITMQ_PASSWORD
}

Write-Host "Secrets synced to namespace '$Namespace'." -ForegroundColor Green