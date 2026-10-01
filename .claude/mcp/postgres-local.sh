#!/bin/bash
# MCP Postgres (somente leitura) apontando para o banco LOCAL do docker compose.
# Lê credenciais do .env para não deixar senha no .mcp.json. Nunca aponte para o Neon.
cd "$(dirname "$0")/../.." || exit 1
set -a; . ./.env; set +a
senha=$(jq -rn --arg p "$POSTGRES_PASSWORD" '$p|@uri')
exec npx -y @modelcontextprotocol/server-postgres \
  "postgresql://${POSTGRES_USER}:${senha}@localhost:5432/${POSTGRES_DB}"
