#!/bin/bash
# PostToolUse (Edit|Write|MultiEdit): checa convenções do CLAUDE.md no arquivo recém-editado.
# exit 2 + stderr → o Claude recebe o problema e corrige na hora.
f=$(jq -r '.tool_input.file_path // empty')
[ -z "$f" ] || [ ! -f "$f" ] && exit 0
rel=${f#"$CLAUDE_PROJECT_DIR"/}

# Mudança na base de conhecimento ou no prompt da Ori → lembrete do gate de eval (não bloqueia).
case "$rel" in
  docs/assistente/kb/*|docs/assistente/prompt/*)
    jq -n '{hookSpecificOutput:{hookEventName:"PostToolUse",additionalContext:"Base/prompt da Ori alterados: o gate de merge exige eval verde (≥95%, zero resposta perigosa). Rode /eval-ori antes de commitar."}}'
    exit 0 ;;
esac

case "$rel" in
  src/Domain/*.cs|src/Application/*.cs|src/Infrastructure/*.cs|src/API/*.cs) ;;
  *) exit 0 ;;
esac

problemas=""
# Ignora linhas de comentário (// ou ///).
hits=$(grep -nE 'DateTime(Offset)?\.Now([^A-Za-z]|$)' "$f" | grep -vE '^[0-9]+:[[:space:]]*//')
[ -n "$hits" ] && problemas+="DateTime.Now fora da WebUI — use DateTime.UtcNow (CLAUDE.md):\n$hits\n"

case "$rel" in
  *XmlNFeService*.cs|*SefazService*.cs)
    hits=$(grep -nE '\{[^{}]+:F[0-9]\}' "$f" | grep -vE '^[0-9]+:[[:space:]]*//')
    [ -n "$hits" ] && problemas+="Interpolação {x:F2}/{x:F4} em XML SEFAZ — gera vírgula em pt-BR e a SEFAZ rejeita. Use F2(...)/F4(...) (InvariantCulture):\n$hits\n" ;;
esac

[ -z "$problemas" ] && exit 0
printf "Convenção violada em %s:\n%b" "$rel" "$problemas" >&2
exit 2
