#!/usr/bin/env bash
#
# Reinicia os containers do NfeSaas, aplica migrations, semeia dados de demo e abre a solucao no IDE.
#
# Fluxo padrao:
#   1. Valida pre-requisitos (Docker, dotnet, .env, JWT_SECRET >= 32 chars)
#   2. (Opcional) Remove volumes para banco limpo (--clean) - re-aplica seed automaticamente
#      AVISO: --clean tambem apaga dp_keys, invalidando CertificadoSenha/CscToken ja cifrados
#   3. Sobe os containers (build opcional)
#   4. Aguarda o postgres ficar saudavel
#   5. Gera o script SQL idempotente das migrations EF Core
#   6. Copia e aplica o script no container do postgres
#   7. Aplica seed de demonstracao quando o banco esta vazio
#   8. Aguarda a API responder em /health
#   9. Mostra endpoints prontos para teste + dicas
#  10. (Opcional) Roda testes Unit + Integration (--test)
#  11. Abre a solution no IDE padrao (a menos que --no-ide)
#
# Uso:
#   ./restart.sh                                   Reinicia tudo com rebuild, aplica migrations e abre a solution.
#   ./restart.sh --clean                            Apaga o banco, recria, re-aplica migrations + seed e abre a solution.
#   ./restart.sh --no-build --skip-migrations --no-ide   Restart rapido (uso CI / iteracao rapida).
#   ./restart.sh --test                             Reinicia tudo e roda a suite de testes. Implica --no-ide.
#
# Flags: --clean --no-build --skip-migrations --no-ide --no-seed --test

set -euo pipefail

CLEAN=false
NO_BUILD=false
SKIP_MIGRATIONS=false
NO_IDE=false
NO_SEED=false
TEST=false

for arg in "$@"; do
    case "$arg" in
        --clean) CLEAN=true ;;
        --no-build) NO_BUILD=true ;;
        --skip-migrations) SKIP_MIGRATIONS=true ;;
        --no-ide) NO_IDE=true ;;
        --no-seed) NO_SEED=true ;;
        --test) TEST=true ;;
        -h|--help)
            grep '^#' "$0" | sed 's/^#//; s/^ //'
            exit 0
            ;;
        *)
            echo "Flag desconhecida: $arg" >&2
            exit 1
            ;;
    esac
done

# --test implica nao abrir IDE (uso em CI / pre-commit)
if [ "$TEST" = true ]; then NO_IDE=true; fi

PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$PROJECT_ROOT"

POSTGRES_CONTAINER='nfesaas_postgres'
API_URL='http://localhost:5001/health'
WEBUI_URL='http://localhost:5002'
MIGRATION_FILE="$PROJECT_ROOT/migration.sql"
ENV_FILE="$PROJECT_ROOT/.env"
ENV_EXAMPLE="$PROJECT_ROOT/.env.example"
SEED_FILE="$PROJECT_ROOT/scripts/seed.sql"
SOLUTION_FILE="$PROJECT_ROOT/NfeSaas.sln"

CYAN='\033[0;36m'; GREEN='\033[0;32m'; YELLOW='\033[0;33m'; RED='\033[0;31m'; GRAY='\033[0;90m'; NC='\033[0m'

step()  { printf "\n${CYAN}==> %s${NC}\n" "$1"; }
ok()    { printf "    ${GREEN}[OK]${NC} %s\n" "$1"; }
warn()  { printf "    ${YELLOW}[!] ${NC} %s\n" "$1"; }
err()   { printf "    ${RED}[X] ${NC} %s\n" "$1"; }
die()   { err "$1"; exit 1; }

# Carrega variaveis do .env para o processo atual (necessario para docker compose substituir
# ${VAR} e para o psql receber POSTGRES_USER/POSTGRES_DB).
load_dotenv() {
    local path="$1"
    [ -f "$path" ] || return 1
    while IFS= read -r line || [ -n "$line" ]; do
        line="$(echo "$line" | sed 's/^[[:space:]]*//; s/[[:space:]]*$//')"
        [ -z "$line" ] && continue
        case "$line" in \#*) continue ;; esac
        local name="${line%%=*}"
        local value="${line#*=}"
        # remove aspas envolventes se houver
        if [[ "$value" == \"*\" ]]; then
            value="${value#\"}"; value="${value%\"}"
        fi
        export "$name=$value"
    done < "$path"
    return 0
}

wait_healthy() {
    local container="$1" timeout="${2:-60}"
    local deadline=$((SECONDS + timeout))
    while [ $SECONDS -lt $deadline ]; do
        local status
        status="$(docker inspect --format '{{.State.Health.Status}}' "$container" 2>/dev/null || true)"
        [ "$status" = "healthy" ] && return 0
        sleep 2
    done
    return 1
}

wait_http_ok() {
    local url="$1" timeout="${2:-120}"
    local deadline=$((SECONDS + timeout))
    while [ $SECONDS -lt $deadline ]; do
        local code
        code="$(curl -s -o /dev/null -w '%{http_code}' --max-time 5 "$url" 2>/dev/null || true)"
        if [ -n "$code" ] && [ "$code" -ge 200 ] 2>/dev/null && [ "$code" -lt 500 ] 2>/dev/null; then
            return 0
        fi
        sleep 2
    done
    return 1
}

psql_file() {
    docker exec "$POSTGRES_CONTAINER" psql -U "$POSTGRES_USER" -d "$POSTGRES_DB" -v ON_ERROR_STOP=1 -f "$1"
}

# -------------------------------------------------------------
# 1. Pre-requisitos
# -------------------------------------------------------------
step "Verificando pre-requisitos"

docker version --format '{{.Server.Version}}' >/dev/null 2>&1 || die "Docker nao esta acessivel. Inicie o Docker Desktop."
ok "Docker disponivel"

if [ ! -f "$ENV_FILE" ]; then
    if [ -f "$ENV_EXAMPLE" ]; then
        err ".env nao encontrado em $ENV_FILE"
        echo ""
        printf "${YELLOW}    Crie copiando .env.example e preencha JWT_SECRET e POSTGRES_PASSWORD com valores fortes:${NC}\n"
        echo "      cp .env.example .env"
        printf "${GRAY}      # Edite .env e gere secrets com:${NC}\n"
        printf "${GRAY}      #   JWT_SECRET:        openssl rand -base64 48${NC}\n"
        printf "${GRAY}      #   POSTGRES_PASSWORD: openssl rand -base64 24${NC}\n"
        echo ""
        die "Configuracao obrigatoria ausente."
    else
        die ".env e .env.example nao encontrados. Verifique a raiz do projeto."
    fi
fi
load_dotenv "$ENV_FILE"
if [ -z "${POSTGRES_USER:-}" ] || [ -z "${POSTGRES_DB:-}" ] || [ -z "${POSTGRES_PASSWORD:-}" ] || [ -z "${JWT_SECRET:-}" ]; then
    die "Variaveis obrigatorias ausentes no .env (POSTGRES_USER, POSTGRES_DB, POSTGRES_PASSWORD, JWT_SECRET)."
fi
if [ "${#JWT_SECRET}" -lt 32 ] || [[ "$JWT_SECRET" == *SUA_CHAVE* ]] || [[ "$JWT_SECRET" == *__TROCAR* ]]; then
    die "JWT_SECRET invalido no .env (precisa ter >= 32 chars e nao pode ser placeholder)."
fi
ok ".env carregado (${POSTGRES_USER}@${POSTGRES_DB})"

if [ "$SKIP_MIGRATIONS" = false ]; then
    dotnet --version >/dev/null 2>&1 || die "dotnet SDK nao encontrado no PATH."

    if ! dotnet tool list -g 2>/dev/null | grep -q dotnet-ef; then
        warn "dotnet-ef nao encontrado globalmente - instalando..."
        dotnet tool install --global dotnet-ef >/dev/null
    fi
    ok "dotnet + dotnet-ef disponiveis"
fi

# -------------------------------------------------------------
# 2. Derrubar containers (e volumes, se --clean)
# -------------------------------------------------------------
step "Parando containers existentes"
if [ "$CLEAN" = true ]; then
    warn "Modo --clean: volumes serao removidos (banco e chaves de cifragem serao apagados)"
    docker compose down -v || warn "docker compose down retornou erro (pode ser normal se nada estava rodando)"
else
    docker compose down || warn "docker compose down retornou erro (pode ser normal se nada estava rodando)"
fi

# -------------------------------------------------------------
# 3. Subir containers
# -------------------------------------------------------------
step "Subindo containers"
if [ "$NO_BUILD" = true ]; then
    docker compose up -d || die "Falha ao subir containers."
else
    docker compose up -d --build || die "Falha ao subir containers."
fi

step "Aguardando postgres ficar saudavel"
wait_healthy "$POSTGRES_CONTAINER" 60 || die "Postgres nao ficou saudavel em 60s. Veja: docker compose logs postgres"
ok "Postgres pronto"

# -------------------------------------------------------------
# 4. Migrations
# -------------------------------------------------------------
if [ "$SKIP_MIGRATIONS" = false ]; then
    step "Gerando script idempotente de migrations"
    dotnet ef migrations script --idempotent \
        -o "$MIGRATION_FILE" \
        --project src/Infrastructure --startup-project src/API \
        || die "Falha ao gerar migration.sql"
    ok "migration.sql gerado"

    step "Aplicando migrations no postgres"
    docker cp "$MIGRATION_FILE" "${POSTGRES_CONTAINER}:/tmp/migration.sql" || die "Falha ao copiar migration.sql para o container"

    psql_file '/tmp/migration.sql' || die "Falha ao aplicar migration.sql no postgres"
    ok "Migrations aplicadas"

    rm -f "$MIGRATION_FILE"
else
    warn "Migrations puladas (--skip-migrations)"
fi

# -------------------------------------------------------------
# 5. Seed (so quando o banco esta vazio)
# -------------------------------------------------------------
if [ "$NO_SEED" = false ] && [ -f "$SEED_FILE" ]; then
    step "Verificando se seed de demo e necessario"
    escritorio_count="$(docker exec "$POSTGRES_CONTAINER" psql -U "$POSTGRES_USER" -d "$POSTGRES_DB" -tAc "SELECT COUNT(*) FROM escritorios;" 2>/dev/null | tr -d '[:space:]' || true)"

    if [ "$escritorio_count" = "0" ]; then
        warn "Banco vazio detectado - aplicando seed de demonstracao"
        docker cp "$SEED_FILE" "${POSTGRES_CONTAINER}:/tmp/seed.sql"
        if psql_file '/tmp/seed.sql'; then
            ok "Seed aplicado (admin@nfesaas.com.br / Admin@123)"
        else
            warn "Falha parcial ao aplicar seed (verifique os logs)"
        fi
    elif [[ "$escritorio_count" =~ ^[0-9]+$ ]]; then
        ok "Banco ja contem dados (${escritorio_count} escritorio(s)) - seed pulado"
    else
        warn "Nao foi possivel checar contagem de escritorios - seed pulado"
    fi
fi

# -------------------------------------------------------------
# 6. Aguardar API
# -------------------------------------------------------------
step "Aguardando API responder em $API_URL"
if wait_http_ok "$API_URL" 120; then
    ok "API respondendo"
else
    warn "API nao respondeu em 120s. Veja: docker compose logs api"
fi

# -------------------------------------------------------------
# 7. Resumo
# -------------------------------------------------------------
step "Ambiente pronto"
echo "    API     : http://localhost:5001"
echo "    Swagger : http://localhost:5001/swagger"
echo "    Health  : http://localhost:5001/health"
echo "    WebUI   : $WEBUI_URL"
echo ""
printf "${GRAY}    Login demo  : admin@nfesaas.com.br / Admin@123${NC}\n"
printf "${GRAY}    Plano demo  : ativo por 1 ano (seed); novos cadastros = trial de 30 dias${NC}\n"
printf "${GRAY}    Empresa demo: 00.000.000/0001-91 (auto-selecionada apos login)${NC}\n"
echo ""
printf "${GRAY}    Atalhos uteis:${NC}\n"
printf "${GRAY}      docker compose logs -f api    # logs API${NC}\n"
printf "${GRAY}      docker compose logs -f webui  # logs WebUI${NC}\n"
printf "${GRAY}      ./restart.sh --test           # rodar testes${NC}\n"
printf "${GRAY}      ./restart.sh --clean          # zerar banco${NC}\n"

# -------------------------------------------------------------
# 8. Testes (opcional)
# -------------------------------------------------------------
if [ "$TEST" = true ]; then
    step "Executando testes Unit + Integration"
    (
        cd "$PROJECT_ROOT"
        unit_exit=0
        int_exit=0
        dotnet test tests/NfeSaas.Tests.Unit --nologo --logger "console;verbosity=minimal" || unit_exit=$?
        dotnet test tests/NfeSaas.Tests.Integration --nologo --logger "console;verbosity=minimal" || int_exit=$?
        if [ "$unit_exit" -ne 0 ] || [ "$int_exit" -ne 0 ]; then
            err "Testes falharam (Unit=$unit_exit, Integration=$int_exit)"
            exit 1
        fi
        ok "Todos os testes passaram"
    )
fi

# -------------------------------------------------------------
# 9. Abrir IDE
# -------------------------------------------------------------
if [ "$NO_IDE" = false ]; then
    if [ -f "$SOLUTION_FILE" ]; then
        step "Abrindo solution no IDE padrao"
        if command -v rider >/dev/null 2>&1; then
            rider "$SOLUTION_FILE" >/dev/null 2>&1 &
            ok "$(basename "$SOLUTION_FILE") aberto no Rider"
        elif open -Ra "Rider" >/dev/null 2>&1; then
            open -a "Rider" "$SOLUTION_FILE"
            ok "$(basename "$SOLUTION_FILE") aberto no Rider"
        elif command -v code >/dev/null 2>&1; then
            code "$PROJECT_ROOT"
            ok "Projeto aberto no VS Code"
        else
            warn "Nenhum IDE conhecido encontrado (Rider ou VS Code) - abra manualmente"
            printf "${GRAY}    Abra manualmente: %s${NC}\n" "$SOLUTION_FILE"
        fi
    else
        warn "NfeSaas.sln nao encontrado em $PROJECT_ROOT - pulando abertura do IDE"
    fi
fi
