#Requires -Version 5.1
<#
.SYNOPSIS
    Reinicia os containers do NfeSaas, aplica migrations e prepara o ambiente para testes.

.DESCRIPTION
    Fluxo padrão:
      1. (Opcional) Remove volumes para banco limpo (-Clean)
      2. Sobe os containers com rebuild
      3. Aguarda o postgres ficar saudável
      4. Gera o script SQL idempotente das migrations EF Core
      5. Copia e aplica o script no container do postgres
      6. Aguarda a API responder em /health
      7. Mostra endpoints prontos para teste

.PARAMETER Clean
    Remove volumes do docker compose (apaga o banco). Útil para resetar dados de seed.

.PARAMETER NoBuild
    Pula o rebuild das imagens (mais rápido quando só houve alteração na WebUI/Blazor).

.PARAMETER SkipMigrations
    Não gera nem aplica migrations. Útil quando você só quer reiniciar.

.EXAMPLE
    .\restart.ps1
    Reinicia tudo com rebuild e aplica migrations.

.EXAMPLE
    .\restart.ps1 -Clean
    Apaga o banco e recria do zero (re-aplica seed via migrations).

.EXAMPLE
    .\restart.ps1 -NoBuild -SkipMigrations
    Restart rápido sem rebuild nem migrations.
#>
[CmdletBinding()]
param(
    [switch]$Clean,
    [switch]$NoBuild,
    [switch]$SkipMigrations
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $projectRoot

$postgresContainer = 'nfesaas_postgres'
$apiUrl = 'http://localhost:5001/health'
$webUiUrl = 'http://localhost:5002'
$migrationFile = Join-Path $projectRoot 'migration.sql'

function Write-Step($msg) {
    Write-Host ""
    Write-Host "==> $msg" -ForegroundColor Cyan
}

function Write-Ok($msg) {
    Write-Host "    [OK] $msg" -ForegroundColor Green
}

function Write-Warn($msg) {
    Write-Host "    [!]  $msg" -ForegroundColor Yellow
}

function Wait-Healthy {
    param(
        [string]$Container,
        [int]$TimeoutSec = 90
    )
    $deadline = (Get-Date).AddSeconds($TimeoutSec)
    while ((Get-Date) -lt $deadline) {
        $status = docker inspect --format '{{.State.Health.Status}}' $Container 2>$null
        if ($LASTEXITCODE -eq 0 -and $status -eq 'healthy') {
            return $true
        }
        Start-Sleep -Seconds 2
    }
    return $false
}

function Wait-HttpOk {
    param(
        [string]$Url,
        [int]$TimeoutSec = 120
    )
    $deadline = (Get-Date).AddSeconds($TimeoutSec)
    while ((Get-Date) -lt $deadline) {
        try {
            $resp = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 5 -ErrorAction Stop
            if ($resp.StatusCode -ge 200 -and $resp.StatusCode -lt 500) {
                return $true
            }
        } catch {}
        Start-Sleep -Seconds 2
    }
    return $false
}

# -------------------------------------------------------------
# 1. Pré-requisitos
# -------------------------------------------------------------
Write-Step "Verificando pre-requisitos"
docker version --format '{{.Server.Version}}' | Out-Null
if ($LASTEXITCODE -ne 0) { throw "Docker nao esta acessivel. Inicie o Docker Desktop." }
Write-Ok "Docker disponivel"

if (-not $SkipMigrations) {
    dotnet --version | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "dotnet SDK nao encontrado no PATH." }

    $efInstalled = (dotnet tool list -g 2>$null) -match 'dotnet-ef'
    if (-not $efInstalled) {
        Write-Warn "dotnet-ef nao encontrado globalmente — instalando..."
        dotnet tool install --global dotnet-ef | Out-Null
    }
    Write-Ok "dotnet + dotnet-ef disponiveis"
}

# -------------------------------------------------------------
# 2. Derrubar containers (e volumes, se -Clean)
# -------------------------------------------------------------
Write-Step "Parando containers existentes"
if ($Clean) {
    Write-Warn "Modo -Clean: volumes serao removidos (banco sera apagado)"
    docker compose down -v
} else {
    docker compose down
}
if ($LASTEXITCODE -ne 0) { Write-Warn "docker compose down retornou erro (pode ser normal se nada estava rodando)" }

# -------------------------------------------------------------
# 3. Subir containers
# -------------------------------------------------------------
Write-Step "Subindo containers"
if ($NoBuild) {
    docker compose up -d
} else {
    docker compose up -d --build
}
if ($LASTEXITCODE -ne 0) { throw "Falha ao subir containers." }

Write-Step "Aguardando postgres ficar saudavel"
if (-not (Wait-Healthy -Container $postgresContainer -TimeoutSec 60)) {
    throw "Postgres nao ficou saudavel em 60s. Veja: docker compose logs postgres"
}
Write-Ok "Postgres pronto"

# -------------------------------------------------------------
# 4. Migrations
# -------------------------------------------------------------
if (-not $SkipMigrations) {
    Write-Step "Gerando script idempotente de migrations"
    dotnet ef migrations script --idempotent `
        -o $migrationFile `
        --project src/Infrastructure `
        --startup-project src/API
    if ($LASTEXITCODE -ne 0) { throw "Falha ao gerar migration.sql" }
    Write-Ok "migration.sql gerado"

    Write-Step "Aplicando migrations no postgres"
    docker cp $migrationFile "${postgresContainer}:/tmp/migration.sql"
    if ($LASTEXITCODE -ne 0) { throw "Falha ao copiar migration.sql para o container" }

    docker exec $postgresContainer psql -U nfesaas -d nfesaas -v ON_ERROR_STOP=1 -f /tmp/migration.sql
    if ($LASTEXITCODE -ne 0) { throw "Falha ao aplicar migration.sql no postgres" }
    Write-Ok "Migrations aplicadas"
} else {
    Write-Warn "Migrations puladas (-SkipMigrations)"
}

# -------------------------------------------------------------
# 5. Aguardar API
# -------------------------------------------------------------
Write-Step "Aguardando API responder em $apiUrl"
if (Wait-HttpOk -Url $apiUrl -TimeoutSec 120) {
    Write-Ok "API respondendo"
} else {
    Write-Warn "API nao respondeu em 120s. Veja: docker compose logs api"
}

# -------------------------------------------------------------
# 6. Resumo
# -------------------------------------------------------------
Write-Step "Ambiente pronto"
Write-Host "    API     : http://localhost:5001"        -ForegroundColor White
Write-Host "    Swagger : http://localhost:5001/swagger" -ForegroundColor White
Write-Host "    Health  : http://localhost:5001/health"  -ForegroundColor White
Write-Host "    WebUI   : $webUiUrl"                     -ForegroundColor White
Write-Host ""
Write-Host "    Login demo: admin@nfesaas.com.br / Admin@123" -ForegroundColor DarkGray
Write-Host ""
Write-Host "    Logs em tempo real:" -ForegroundColor DarkGray
Write-Host "      docker compose logs -f api"            -ForegroundColor DarkGray
Write-Host "      docker compose logs -f webui"          -ForegroundColor DarkGray
