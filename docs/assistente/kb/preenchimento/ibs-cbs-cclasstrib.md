---
id: preenchimento/ibs-cbs-cclasstrib
titulo: "IBS e CBS na NF-e: CST e cClassTrib"
categoria: preenchimento
nivel_fonte: N1
codigos_rejeicao: []
campos_relacionados: [cst_ibs_cbs, cclasstrib]
telas: [emitir-nfe]
fontes:
  - documento: "Portal Nacional da NF-e — Notas Técnicas vigentes"
    url: "https://www.nfe.fazenda.gov.br/portal/listaConteudo.aspx?tipoConteudo=04BIflQt1aY="
    nivel: N1
  - documento: "Lei Complementar 214/2025 (IBS, CBS e IS)"
    url: "https://www.planalto.gov.br/ccivil_03/leis/lcp/lcp214.htm"
    nivel: N1
vigencia_inicio: 2026-01-01
vigencia_fim: null
verificado_em: null
revisar_ate: null
curador: null
status: rascunho
resumo_curto: "Na Reforma Tributária, o item da NF-e traz o grupo IBS/CBS com CST (3 dígitos, ex.: 000 tributação integral) e cClassTrib (6 dígitos da tabela oficial, ex.: 000001 sem benefício). Em 2026, fase de teste: CBS 0,9% e IBS 0,1%."
---

## Resumo

Cada item da NF-e passa a ter o grupo **IBS/CBS**, com:
- **CST do IBS/CBS** — 3 dígitos (ex.: **000** tributação integral, **200** alíquota reduzida, **410** imunidade);
- **cClassTrib** — 6 dígitos da tabela oficial de classificação tributária (ex.: **000001**, sem benefício).

Em **2026** vale a fase de teste da LC 214/2025, com alíquotas de **0,9%** de CBS e **0,1%** de IBS.

## Como corrigir no NFeFlow

1. Emitir NF-e › **Produtos** › campos **CST IBS/CBS** e **cClassTrib** (aparecem para empresas do regime normal).
2. Use o código da tabela oficial — o NFeFlow valida o formato de 6 dígitos.

## Regra oficial

A estrutura do grupo IBS/CBS está nas Notas Técnicas da Reforma Tributária do Consumo publicadas no Portal Nacional da NF-e;
as alíquotas de teste de 2026 estão na LC 214/2025.

## Observações por UF / exceções

O enquadramento em reduções, isenções e regimes específicos é decisão do contador — a Ori só explica os códigos.

> Rascunho gerado com apoio de IA — conferir números, prazos e códigos na fonte oficial antes de publicar.
