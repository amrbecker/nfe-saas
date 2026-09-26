---
id: rejeicoes/uso-denegado
titulo: "Uso denegado (301 e 302)"
categoria: rejeicoes
nivel_fonte: N1
codigos_rejeicao: [301, 302]
campos_relacionados: [cpf_cnpj, ie]
telas: [nota-detalhe]
fontes:
  - documento: "Manual de Orientação do Contribuinte (MOC) — leiaute e regras de validação da NF-e/NFC-e"
    url: "https://www.nfe.fazenda.gov.br/portal/listaConteudo.aspx?tipoConteudo=ndIjl+iEFdE="
    nivel: N1
vigencia_inicio: null
vigencia_fim: null
verificado_em: null
revisar_ate: null
curador: null
status: rascunho
resumo_curto: "Uso denegado significa irregularidade fiscal: 301 do emitente e 302 do destinatário. A nota fica registrada como denegada, o número é consumido e não pode ser reutilizado. A regularização é com a SEFAZ."
---

## Resumo

- **301 — Uso denegado: irregularidade fiscal do emitente.**
- **302 — Uso denegado: irregularidade fiscal do destinatário.**

## Causa

A SEFAZ identificou situação cadastral irregular (por exemplo, IE suspensa, cancelada ou baixada).

## Como corrigir no NFeFlow

1. A nota denegada não pode ser corrigida nem cancelada, e o número não pode ser reutilizado.
2. Verifique a situação cadastral do emitente ou do destinatário no Sintegra/SEFAZ da UF.
3. Depois de regularizar, emita uma nova nota com novo número.

## Regra oficial

A denegação de uso e seus efeitos estão no MOC e no Ajuste SINIEF 07/05.

> Rascunho gerado com apoio de IA — conferir números, prazos e códigos na fonte oficial antes de publicar.
