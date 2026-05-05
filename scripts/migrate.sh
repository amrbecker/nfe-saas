#!/bin/bash
# =============================================================
# Script para gerar e aplicar migrations EF Core
# Execute na raiz do projeto
# =============================================================

set -e

echo "📦 Instalando dotnet-ef (se necessário)..."
dotnet tool install --global dotnet-ef 2>/dev/null || dotnet tool update --global dotnet-ef

echo "🏗️  Gerando migration inicial..."
cd src/Infrastructure
dotnet ef migrations add InitialCreate \
    --startup-project ../API \
    --output-dir Data/Migrations

echo "🗄️  Aplicando migration no banco de dados..."
dotnet ef database update \
    --startup-project ../API

echo "✅ Migrations concluídas!"
echo ""
echo "📋 Para criar nova migration:"
echo "   dotnet ef migrations add NomeDaMigration --startup-project ../API"
echo ""
echo "📋 Para reverter última migration:"
echo "   dotnet ef migrations remove --startup-project ../API"
