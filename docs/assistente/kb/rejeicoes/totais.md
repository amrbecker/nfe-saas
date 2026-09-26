---
id: rejeicoes/totais
titulo: "Total da nota diferente da soma dos itens (rejeição 610)"
categoria: rejeicoes
nivel_fonte: N1
codigos_rejeicao: [610]
campos_relacionados: [valor_unitario, quantidade]
telas: [emitir-nfe]
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
resumo_curto: "A rejeição 610 aparece quando o total da NF-e não bate com a soma dos valores que o compõem (produtos, frete, seguro, descontos, impostos). Revise descontos, frete e arredondamentos dos itens."
---

## Resumo

A rejeição **610** indica que o valor total da nota difere do somatório dos valores que o compõem.

## Causa

Diferença de arredondamento, desconto aplicado só no total, frete ou seguro informados de um jeito e somados de outro.

## Como corrigir no NFeFlow

1. Emitir NF-e › **Produtos**: confira quantidade, valor unitário e desconto de cada item.
2. Aba **Transporte/Pgto**: confira frete e seguro.
3. O NFeFlow calcula os totais automaticamente; se a diferença persistir, abra um chamado pela Ori.

## Regra oficial

A composição do valor total (vNF) e as regras de validação dos totais estão no MOC.

> Rascunho gerado com apoio de IA — conferir números, prazos e códigos na fonte oficial antes de publicar.
