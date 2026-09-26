---
id: preenchimento/cst-csosn
titulo: "CST x CSOSN: qual usar conforme o regime"
categoria: preenchimento
nivel_fonte: N1
codigos_rejeicao: [590, 591]
campos_relacionados: [cst, csosn, regime_tributario]
telas: [emitir-nfe, empresa]
fontes:
  - documento: "Manual de Orientação do Contribuinte (MOC) — leiaute e regras de validação da NF-e/NFC-e"
    url: "https://www.nfe.fazenda.gov.br/portal/listaConteudo.aspx?tipoConteudo=ndIjl+iEFdE="
    nivel: N1
  - documento: "Lei Complementar 123/2006 (Simples Nacional)"
    url: "https://www.planalto.gov.br/ccivil_03/leis/lcp/lcp123.htm"
    nivel: N1
vigencia_inicio: null
vigencia_fim: null
verificado_em: null
revisar_ate: null
curador: null
status: rascunho
resumo_curto: "Empresa do Simples Nacional (CRT 1) informa o ICMS pelo CSOSN (101, 102, 500...). Empresa do regime normal (CRT 3) usa o CST (00, 10, 20...). Misturar os dois gera rejeição."
---

## Resumo

O código de tributação do ICMS depende do regime do emitente (CRT): **Simples Nacional** usa **CSOSN** (3 dígitos, ex.:
101, 102, 500, 900) e **regime normal** usa **CST** (2 dígitos, ex.: 00, 10, 20, 60, 90).

## Causa

As rejeições 590 (CST informado para emitente do Simples Nacional) e 591 (CSOSN informado para emitente que não é do
Simples Nacional) aparecem quando o regime cadastrado não combina com o código usado nos itens.

## Como corrigir no NFeFlow

1. Confira o **Regime tributário** em **Config. Empresa**. O NFeFlow mostra CSOSN para Simples Nacional e CST para regime normal.
2. Em Emitir NF-e › **Produtos**, revise o campo **CSOSN** ou **CST ICMS** de cada item.
3. CSOSN mais usados: **102** (tributada pelo Simples sem permissão de crédito), **101** (com permissão de crédito),
   **500** (ICMS cobrado anteriormente por substituição tributária).

## Regra oficial

O CRT e as tabelas de CST e CSOSN estão no leiaute da NF-e (MOC). O regime do Simples Nacional é definido pela LC 123/2006.

## Observações por UF / exceções

A escolha entre 101 e 102 (crédito de ICMS) depende da situação do cliente — decisão do contador.

> Rascunho gerado com apoio de IA — conferir números, prazos e códigos na fonte oficial antes de publicar.
