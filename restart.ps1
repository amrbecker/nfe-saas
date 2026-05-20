#Requires -Version 5.1
<#
.SYNOPSIS
    Reinicia os containers do NfeSaas, aplica migrations, semeia dados de demo e abre a solucao no IDE.

.DESCRIPTION
    Fluxo padrao:
      1. Valida pre-requisitos (Docker, dotnet, .env)
      2. (Opcional) Remove volumes para banco limpo (-Clean) - re-aplica seed automaticamente
      3. Sobe os containers (build opcional)
      4. Aguarda o postgres ficar saudavel
      5. Gera o script SQL idempotente das migrations EF Core
      6. Copia e aplica o script no container do postgres
      7. Aplica seed de demonstracao quando o banco esta vazio
      8. Aguarda a API responder em /health
      9. Mostra endpoints prontos para teste
     10. Abre a solution no IDE padrao (a menos que -NoIde)

.PARAMETER Clean
    Remove volumes do docker compose (apaga o banco). Util para resetar dados de seed.
    Implica re-aplicar o seed.

.PARAMETER NoBuild
    Pula o rebuild das imagens. Mais rapido quando so houve alteracao na WebUI/Blazor.

.PARAMETER SkipMigrations
    Nao gera nem aplica migrations. Util quando voce so quer reiniciar.

.PARAMETER NoIde
    Nao abre a solution no IDE ao final.

.PARAMETER NoSeed
    Nao aplica o seed mesmo quando o banco esta vazio. Util para testes do zero.

.EXAMPLE
    .\restart.ps1
    Reinicia tudo com rebuild, aplica migrations e abre a solution.

.EXAMPLE
    .\restart.ps1 -Clean
    Apaga o banco, recria, re-aplica migrations + seed e abre a solution.

.EXAMPLE
    .\restart.ps1 -NoBuild -SkipMigrations -NoIde
    Restart rapido sem rebuild, sem migrations e sem abrir IDE (uso CI / iteracao rapida).
#>
[CmdletBinding()]
param(
    [switch]$Clean,
    [switch]$NoBuild,
    [switch]$SkipMigrations,
    [switch]$NoIde,
    [switch]$NoSeed
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $projectRoot

$postgresContainer = 'nfesaas_postgres'
$apiUrl    = 'http://localhost:5001/health'
$webUiUrl  = 'http://localhost:5002'
$migrationFile = Join-Path $projectRoot 'migration.sql'
$envFile    = Join-Path $projectRoot '.env'
$envExample = Join-Path $projectRoot '.env.example'
$seedFile   = Join-Path $projectRoot 'scripts/seed.sql'
$solutionFile = Join-Path $projectRoot 'NfeSaas.sln'

function Write-Step($msg) {
    Write-Host ""
    Write-Host "==> $msg" -ForegroundColor Cyan
}

function Write-Ok($msg)   { Write-Host "    [OK] $msg" -ForegroundColor Green }
function Write-Warn($msg) { Write-Host "    [!]  $msg" -ForegroundColor Yellow }
function Write-Err($msg)  { Write-Host "    [X]  $msg" -ForegroundColor Red }

# Carrega variaveis do .env para o processo atual (necessario para docker compose substituir
# ${VAR} e para o psql receber POSTGRES_USER/POSTGRES_DB).
function Import-DotEnv {
    param([string]$Path)
    if (-not (Test-Path $Path)) { return $false }
    Get-Content $Path | ForEach-Object {
        $line = $_.Trim()
        if ($line -eq '' -or $line.StartsWith('#')) { return }
        $eq = $line.IndexOf('=')
        if ($eq -lt 1) { return }
        $name  = $line.Substring(0, $eq).Trim()
        $value = $line.Substring($eq + 1).Trim()
        # remove aspas envolventes se houver
        if ($value.StartsWith('"') -and $value.EndsWith('"')) {
            $value = $value.Substring(1, $value.Length - 2)
        }
        Set-Item -Path "Env:$name" -Value $value
    }
    return $true
}

function Wait-Healthy {
    param([string]$Container, [int]$TimeoutSec = 90)
    $deadline = (Get-Date).AddSeconds($TimeoutSec)
    while ((Get-Date) -lt $deadline) {
        $status = docker inspect --format '{{.State.Health.Status}}' $Container 2>$null
        if ($LASTEXITCODE -eq 0 -and $status -eq 'healthy') { return $true }
        Start-Sleep -Seconds 2
    }
    return $false
}

function Wait-HttpOk {
    param([string]$Url, [int]$TimeoutSec = 120)
    $deadline = (Get-Date).AddSeconds($TimeoutSec)
    while ((Get-Date) -lt $deadline) {
        try {
            $resp = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 5 -ErrorAction Stop
            if ($resp.StatusCode -ge 200 -and $resp.StatusCode -lt 500) { return $true }
        } catch {}
        Start-Sleep -Seconds 2
    }
    return $false
}

function Invoke-Psql {
    param([string]$Sql, [string]$File)
    $user = $env:POSTGRES_USER
    $db   = $env:POSTGRES_DB
    if ($File) {
        docker exec $postgresContainer psql -U $user -d $db -v ON_ERROR_STOP=1 -f $File
    } else {
        docker exec $postgresContainer psql -U $user -d $db -v ON_ERROR_STOP=1 -c $Sql
    }
}

# -------------------------------------------------------------
# 1. Pre-requisitos
# -------------------------------------------------------------
Write-Step "Verificando pre-requisitos"

docker version --format '{{.Server.Version}}' | Out-Null
if ($LASTEXITCODE -ne 0) { throw "Docker nao esta acessivel. Inicie o Docker Desktop." }
Write-Ok "Docker disponivel"

if (-not (Test-Path $envFile)) {
    if (Test-Path $envExample) {
        Write-Err ".env nao encontrado em $envFile"
        Write-Host ""
        Write-Host "    Crie copiando .env.example e preencha JWT_SECRET e POSTGRES_PASSWORD com valores fortes:" -ForegroundColor Yellow
        Write-Host "      Copy-Item .env.example .env" -ForegroundColor White
        Write-Host "      # Edite .env e gere secrets com:" -ForegroundColor DarkGray
        Write-Host "      #   JWT_SECRET:        openssl rand -base64 48" -ForegroundColor DarkGray
        Write-Host "      #   POSTGRES_PASSWORD: openssl rand -base64 24" -ForegroundColor DarkGray
        Write-Host ""
        throw "Configuracao obrigatoria ausente."
    } else {
        throw ".env e .env.example nao encontrados. Verifique a raiz do projeto."
    }
}
Import-DotEnv -Path $envFile | Out-Null
if (-not $env:POSTGRES_USER -or -not $env:POSTGRES_DB -or -not $env:POSTGRES_PASSWORD -or -not $env:JWT_SECRET) {
    throw "Variaveis obrigatorias ausentes no .env (POSTGRES_USER, POSTGRES_DB, POSTGRES_PASSWORD, JWT_SECRET)."
}
if ($env:JWT_SECRET.Length -lt 32 -or $env:JWT_SECRET -match 'SUA_CHAVE|__TROCAR') {
    throw "JWT_SECRET invalido no .env (precisa ter >= 32 chars e nao pode ser placeholder)."
}
Write-Ok ".env carregado ($($env:POSTGRES_USER)@$($env:POSTGRES_DB))"

if (-not $SkipMigrations) {
    dotnet --version | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "dotnet SDK nao encontrado no PATH." }

    $efInstalled = (dotnet tool list -g 2>$null) -match 'dotnet-ef'
    if (-not $efInstalled) {
        Write-Warn "dotnet-ef nao encontrado globalmente - instalando..."
        dotnet tool install --global dotnet-ef | Out-Null
    }
    Write-Ok "dotnet + dotnet-ef disponiveis"
}

# -------------------------------------------------------------
# 2. Derrubar containers (e volumes, se -Clean)
# -------------------------------------------------------------
Write-Step "Parando containers existentes"
if ($Clean) {
    Write-Warn "Modo -Clean: volumes serao removidos (banco e chaves de cifragem serao apagados)"
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
        --project src/Infrastructure --startup-project src/API
    if ($LASTEXITCODE -ne 0) { throw "Falha ao gerar migration.sql" }
    Write-Ok "migration.sql gerado"

    Write-Step "Aplicando migrations no postgres"
    docker cp $migrationFile "${postgresContainer}:/tmp/migration.sql"
    if ($LASTEXITCODE -ne 0) { throw "Falha ao copiar migration.sql para o container" }

    Invoke-Psql -File '/tmp/migration.sql'
    if ($LASTEXITCODE -ne 0) { throw "Falha ao aplicar migration.sql no postgres" }
    Write-Ok "Migrations aplicadas"

    Remove-Item $migrationFile -ErrorAction SilentlyContinue
} else {
    Write-Warn "Migrations puladas (-SkipMigrations)"
}

# -------------------------------------------------------------
# 5. Seed (so quando o banco esta vazio)
# -------------------------------------------------------------
if (-not $NoSeed -and (Test-Path $seedFile)) {
    Write-Step "Verificando se seed de demo e necessario"
    $countOutput = docker exec $postgresContainer psql -U $env:POSTGRES_USER -d $env:POSTGRES_DB -tAc "SELECT COUNT(*) FROM escritorios;" 2>$null
    $escritorioCount = if ($LASTEXITCODE -eq 0) { [int]($countOutput.Trim()) } else { -1 }

    if ($escritorioCount -eq 0) {
        Write-Warn "Banco vazio detectado - aplicando seed de demonstracao"
        docker cp $seedFile "${postgresContainer}:/tmp/seed.sql"
        Invoke-Psql -File '/tmp/seed.sql'
        if ($LASTEXITCODE -ne 0) { Write-Warn "Falha parcial ao aplicar seed (verifique os logs)" }
        else { Write-Ok "Seed aplicado (admin@nfesaas.com.br / Admin@123)" }
    } elseif ($escritorioCount -gt 0) {
        Write-Ok "Banco ja contem dados ($escritorioCount escritorio(s)) - seed pulado"
    } else {
        Write-Warn "Nao foi possivel checar contagem de escritorios - seed pulado"
    }
}

# -------------------------------------------------------------
# 6. Aguardar API
# -------------------------------------------------------------
Write-Step "Aguardando API responder em $apiUrl"
if (Wait-HttpOk -Url $apiUrl -TimeoutSec 120) {
    Write-Ok "API respondendo"
} else {
    Write-Warn "API nao respondeu em 120s. Veja: docker compose logs api"
}

# -------------------------------------------------------------
# 7. Resumo
# -------------------------------------------------------------
Write-Step "Ambiente pronto"
Write-Host "    API     : http://localhost:5001"          -ForegroundColor White
Write-Host "    Swagger : http://localhost:5001/swagger"  -ForegroundColor White
Write-Host "    Health  : http://localhost:5001/health"   -ForegroundColor White
Write-Host "    WebUI   : $webUiUrl"                      -ForegroundColor White
Write-Host ""
Write-Host "    Login demo: admin@nfesaas.com.br / Admin@123" -ForegroundColor DarkGray
Write-Host ""
Write-Host "    Logs em tempo real:" -ForegroundColor DarkGray
Write-Host "      docker compose logs -f api"   -ForegroundColor DarkGray
Write-Host "      docker compose logs -f webui" -ForegroundColor DarkGray

# -------------------------------------------------------------
# 8. Abrir IDE
# -------------------------------------------------------------
if (-not $NoIde) {
    if (Test-Path $solutionFile) {
        Write-Step "Abrindo solution no IDE padrao"
        try {
            Start-Process $solutionFile -ErrorAction Stop
            Write-Ok "$([IO.Path]::GetFileName($solutionFile)) aberto"
        } catch {
            Write-Warn "Nao foi possivel abrir a solution automaticamente: $($_.Exception.Message)"
            Write-Host "    Abra manualmente: $solutionFile" -ForegroundColor DarkGray
        }
    } else {
        Write-Warn "NfeSaas.sln nao encontrado em $projectRoot - pulando abertura do IDE"
    }
}
