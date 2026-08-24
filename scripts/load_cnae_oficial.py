"""
Carga da tabela CNAE oficial (IBGE/CONCLA — CNAE 2.3).

Fonte: API pública do IBGE, buscada diretamente (sem download manual):
  https://servicodados.ibge.gov.br/api/v2/cnae/subclasses

Saída: scripts/cnae_seed.sql (UPSERT idempotente compatível com a tabela `cnaes`)

Diferente do load_ncm_oficial.py, não há conceito de "versão da tabela" nem desativação
automática de códigos removidos — CNAE muda raríssimo, então isso não foi implementado
(ver docs do plano). Rodar de novo só reaplica os mesmos códigos com ON CONFLICT.

Uso:
  python scripts/load_cnae_oficial.py
"""
import json
import sys
import urllib.request
from pathlib import Path

URL = "https://servicodados.ibge.gov.br/api/v2/cnae/subclasses"


def sql_escape(s: str) -> str:
    return (s or "").replace("'", "''")


def apenas_digitos(s: str) -> str:
    return "".join(ch for ch in (s or "") if ch.isdigit())


def main():
    print(f"Buscando {URL} ...", file=sys.stderr)
    with urllib.request.urlopen(URL, timeout=30) as resp:
        data = json.load(resp)

    print(f"  {len(data)} subclasses CNAE recebidas.", file=sys.stderr)

    cnaes = []
    for item in data:
        codigo = apenas_digitos(item.get("id", ""))
        if len(codigo) != 7:
            continue

        descricao = (item.get("descricao") or "").strip()[:500]
        if not descricao:
            continue

        classe = item.get("classe") or {}
        grupo = classe.get("grupo") or {}
        divisao_obj = grupo.get("divisao") or {}
        secao_obj = divisao_obj.get("secao") or {}

        divisao = apenas_digitos(str(divisao_obj.get("id", "")))[:2] or codigo[:2]
        secao = (str(secao_obj.get("id", "")).strip())[:1] or None

        cnaes.append((codigo, descricao, secao, divisao))

    print(f"  CNAEs válidos: {len(cnaes)}", file=sys.stderr)

    out_path = Path(__file__).parent / "cnae_seed.sql"
    BATCH = 500

    with out_path.open("w", encoding="utf-8") as out:
        out.write("-- ============================================================\n")
        out.write("-- CNAE oficial — IBGE/CONCLA (CNAE 2.3)\n")
        out.write(f"-- Origem: {URL}\n")
        out.write(f"-- Total: {len(cnaes)} subclasses\n")
        out.write("-- ============================================================\n\n")
        out.write("BEGIN;\n\n")

        for i in range(0, len(cnaes), BATCH):
            chunk = cnaes[i:i + BATCH]
            out.write(
                'INSERT INTO cnaes ("Codigo","Descricao","Secao","Divisao","Ativo","AtualizadoEm") VALUES\n'
            )
            linhas = []
            for cod, desc, secao, divisao in chunk:
                secao_sql = f"'{secao}'" if secao else "NULL"
                linhas.append(
                    f"  ('{cod}','{sql_escape(desc)}',{secao_sql},'{divisao}',TRUE,NOW())"
                )
            out.write(",\n".join(linhas))
            out.write(
                '\nON CONFLICT ("Codigo") DO UPDATE SET\n'
                '  "Descricao"    = EXCLUDED."Descricao",\n'
                '  "Secao"        = EXCLUDED."Secao",\n'
                '  "Divisao"      = EXCLUDED."Divisao",\n'
                '  "Ativo"        = TRUE,\n'
                '  "AtualizadoEm" = NOW();\n\n'
            )

        out.write("COMMIT;\n")

    print(f"\nSQL gerado: {out_path}", file=sys.stderr)
    print("Aplicar no Postgres com:", file=sys.stderr)
    print("  MSYS_NO_PATHCONV=1 docker cp ./scripts/cnae_seed.sql nfesaas_postgres:/tmp/", file=sys.stderr)
    print("  MSYS_NO_PATHCONV=1 docker exec nfesaas_postgres psql -U nfesaas -d nfesaas -f /tmp/cnae_seed.sql", file=sys.stderr)


if __name__ == "__main__":
    main()
