#!/bin/bash
# PreToolUse (Edit|Write|MultiEdit): bloqueia edição de segredos e de arquivos gerados pelo dotnet ef.
f=$(jq -r '.tool_input.file_path // empty')
[ -z "$f" ] && exit 0
nome=$(basename "$f")

case "$nome" in
  .env.example) exit 0 ;;
  .env|.env.*)
    echo "Bloqueado: $nome guarda segredos reais e não deve ser editado pelo Claude. Peça ao usuário para alterar manualmente." >&2
    exit 2 ;;
esac

case "$f" in
  */Migrations/*.Designer.cs|*/Migrations/*ModelSnapshot.cs)
    echo "Bloqueado: $nome é gerado pelo dotnet ef. Altere a entidade/configuração e rode 'dotnet ef migrations add' (ou /migration) em vez de editar à mão." >&2
    exit 2 ;;
esac
exit 0
