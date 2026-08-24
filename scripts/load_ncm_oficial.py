"""
Carga da tabela NCM oficial (Portal Único Siscomex / Resolução Camex/Gecex).

Entrada: JSON exportado de https://portalunico.siscomex.gov.br/
Saída:   scripts/ncm_full_seed.sql (UPSERT idempotente compatível com tabela `ncms`)

Comportamento:
  - Aceita apenas códigos de 8 dígitos (NCMs finais) vigentes (Data_Fim = 31/12/9999)
  - Enriquece descrição com contexto da posição/subposição pai (ex.: "Cavalos — Reprodutores de raça pura")
  - Limpa prefixos hierárquicos "- ", "-- ", "--- ", " ° " etc.
  - Preserva `ExigeCest=TRUE` dos NCMs já marcados manualmente no seed mínimo
    (via `ExigeCest = ncms."ExigeCest" OR EXCLUDED."ExigeCest"` no UPSERT)

Uso:
  python scripts/load_ncm_oficial.py <caminho_para_json> [versao_tabela]

  Exemplo:
    python scripts/load_ncm_oficial.py C:/Dev/NFe/Tabela_NCM_Vigente_20260511.json 2026-05
"""
import json
import re
import sys
from pathlib import Path

PREFIX_RE = re.compile(r"^[\-\s°•·]+")  # remove "- ", "-- ", "---", marcadores de bullet
COLLAPSE_WS = re.compile(r"\s+")
HTML_TAG_RE = re.compile(r"<[^>]+>")  # remove tags HTML cruas da fonte (ex.: <i>smartphones</i>)


def limpar(desc: str) -> str:
    """Remove tags HTML, prefixos hierárquicos e colapsa espaços."""
    s = HTML_TAG_RE.sub("", desc or "")
    s = PREFIX_RE.sub("", s).strip()
    return COLLAPSE_WS.sub(" ", s)


def apenas_digitos(codigo: str) -> str:
    return re.sub(r"\D", "", codigo or "")


def montar_descricao(desc_propria: str, desc_subpos: str, desc_pos: str) -> str:
    """Monta uma descrição autoexplicativa para o NCM final.

    Estratégia (em ordem de preferência):
      1. {subposição} — {item}        (mais específico)
      2. {posição} — {item}           (fallback)
      3. {item}                       (último caso)
    """
    item = limpar(desc_propria)
    sub = limpar(desc_subpos)
    pos = limpar(desc_pos)

    # Se a descrição própria for genérica ("Outros", "Outras", "Os demais"),
    # o contexto da subposição/posição é o que dá significado.
    if sub and sub.lower() not in (item.lower(),) and len(sub) > 3:
        return f"{sub} — {item}".rstrip(" —")
    if pos and pos.lower() not in (item.lower(),) and len(pos) > 3:
        return f"{pos} — {item}".rstrip(" —")
    return item


def sql_escape(s: str) -> str:
    return s.replace("'", "''")


def main():
    if len(sys.argv) < 2:
        print(__doc__)
        sys.exit(1)

    json_path = Path(sys.argv[1])
    versao = sys.argv[2] if len(sys.argv) > 2 else "2026-05"

    if not json_path.exists():
        print(f"ERRO: arquivo não encontrado: {json_path}", file=sys.stderr)
        sys.exit(2)

    print(f"Lendo {json_path} ...", file=sys.stderr)
    with json_path.open("r", encoding="utf-8") as f:
        data = json.load(f)

    nomenclaturas = data.get("Nomenclaturas", [])
    total = len(nomenclaturas)
    print(f"  {total} entradas na árvore NCM (todos os níveis).", file=sys.stderr)

    # Indexa por código (sem pontos) para lookup do pai.
    indexado = {}
    for n in nomenclaturas:
        cod = apenas_digitos(n.get("Codigo", ""))
        indexado[cod] = n

    ncms_finais = []
    descartados_nao_vigentes = 0
    descartados_nao_finais = 0

    for n in nomenclaturas:
        cod = apenas_digitos(n.get("Codigo", ""))

        # 1) Só códigos de 8 dígitos.
        if len(cod) != 8:
            descartados_nao_finais += 1
            continue

        # 2) Só vigentes (Data_Fim = 31/12/9999 ou ausente).
        data_fim = (n.get("Data_Fim") or "").strip()
        if data_fim and data_fim != "31/12/9999":
            descartados_nao_vigentes += 1
            continue

        # 3) Encontra pais para enriquecer a descrição:
        #    Subposição = 6 dígitos, Posição = 4 dígitos.
        cod_subpos = cod[:6]
        cod_pos = cod[:4]

        desc_subpos = indexado.get(cod_subpos, {}).get("Descricao", "")
        desc_pos = indexado.get(cod_pos, {}).get("Descricao", "")

        desc_final = montar_descricao(n.get("Descricao", ""), desc_subpos, desc_pos)
        if not desc_final:
            continue
        # Truncar a 500 chars (limite do varchar)
        desc_final = desc_final[:500]

        capitulo = cod[:2]
        posicao = cod[:4]
        ncms_finais.append((cod, desc_final, capitulo, posicao))

    print(f"  NCMs finais vigentes: {len(ncms_finais)}", file=sys.stderr)
    print(f"  Descartados (intermediários da árvore): {descartados_nao_finais}", file=sys.stderr)
    print(f"  Descartados (não vigentes): {descartados_nao_vigentes}", file=sys.stderr)

    # Gera SQL em batches de 1000 INSERTs por VALUES para performance.
    out_path = Path(__file__).parent / "ncm_full_seed.sql"
    BATCH = 1000

    with out_path.open("w", encoding="utf-8") as out:
        out.write(f"-- ============================================================\n")
        out.write(f"-- NCM oficial — Portal Único Siscomex\n")
        out.write(f"-- Origem: {json_path.name}\n")
        out.write(f"-- {data.get('Data_Ultima_Atualizacao_NCM', '')}\n")
        out.write(f"-- Ato: {data.get('Ato', '')}\n")
        out.write(f"-- Versão da tabela: {versao}\n")
        out.write(f"-- Total: {len(ncms_finais)} NCMs vigentes\n")
        out.write(f"-- ============================================================\n\n")
        out.write("BEGIN;\n\n")

        for i in range(0, len(ncms_finais), BATCH):
            chunk = ncms_finais[i:i + BATCH]
            out.write(
                'INSERT INTO ncms ("Codigo","Descricao","CategoriaCapitulo","Posicao",'
                '"AliquotaIpiPadrao","ExigeCest","Ativo","VersaoTabela","AtualizadoEm") VALUES\n'
            )
            linhas = []
            for cod, desc, cap, pos in chunk:
                linhas.append(
                    f"  ('{cod}','{sql_escape(desc)}','{cap}','{pos}',NULL,FALSE,TRUE,'{versao}',NOW())"
                )
            out.write(",\n".join(linhas))
            out.write(
                '\nON CONFLICT ("Codigo") DO UPDATE SET\n'
                '  "Descricao"         = EXCLUDED."Descricao",\n'
                '  "CategoriaCapitulo" = EXCLUDED."CategoriaCapitulo",\n'
                '  "Posicao"           = EXCLUDED."Posicao",\n'
                '  "VersaoTabela"      = EXCLUDED."VersaoTabela",\n'
                '  "Ativo"             = TRUE,\n'
                '  "ExigeCest"         = ncms."ExigeCest" OR EXCLUDED."ExigeCest",\n'
                '  "AtualizadoEm"      = NOW();\n\n'
            )

        # Desativa NCMs que sumiram da tabela oficial (ficaram fora da nova versão).
        out.write(
            "-- Desativa NCMs que não constam mais na tabela vigente.\n"
            f"UPDATE ncms SET \"Ativo\" = FALSE, \"AtualizadoEm\" = NOW()\n"
            f"WHERE \"VersaoTabela\" <> '{versao}' AND \"Ativo\" = TRUE;\n\n"
        )

        out.write("COMMIT;\n")

    print(f"\nSQL gerado: {out_path}", file=sys.stderr)
    print(f"Aplicar no Postgres com:", file=sys.stderr)
    print(f"  MSYS_NO_PATHCONV=1 docker cp ./scripts/ncm_full_seed.sql nfesaas_postgres:/tmp/", file=sys.stderr)
    print(f"  MSYS_NO_PATHCONV=1 docker exec nfesaas_postgres psql -U nfesaas -d nfesaas -f /tmp/ncm_full_seed.sql", file=sys.stderr)


if __name__ == "__main__":
    main()
