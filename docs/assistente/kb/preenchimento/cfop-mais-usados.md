---
id: preenchimento/cfop-mais-usados
titulo: "CFOP: como escolher pelo destino da operação"
categoria: preenchimento
nivel_fonte: N1
codigos_rejeicao: []
campos_relacionados: [cfop]
telas: [emitir-nfe, produtos]
fontes:
  - documento: "Manual de Orientação do Contribuinte (MOC) — leiaute e regras de validação da NF-e/NFC-e"
    url: "https://www.nfe.fazenda.gov.br/portal/listaConteudo.aspx?tipoConteudo=ndIjl+iEFdE="
    nivel: N1
  - documento: "Tabela de CFOP — Convênio s/nº de 15/12/1970 (Anexo) e alterações (CONFAZ)"
    url: "https://www.confaz.fazenda.gov.br/legislacao/convenios"
    nivel: N1
vigencia_inicio: null
vigencia_fim: null
verificado_em: null
revisar_ate: null
curador: null
status: rascunho
resumo_curto: "O primeiro dígito do CFOP indica o destino: 5 (mesma UF), 6 (outra UF) e 7 (exterior) nas saídas; 1, 2 e 3 nas entradas. Venda de mercadoria comprada de terceiros: 5102 dentro da UF e 6102 para outra UF."
---

## Resumo

O primeiro dígito do CFOP diz para onde vai a mercadoria. Nas saídas: **5** para destinatário na mesma UF do emitente,
**6** para outra UF e **7** para o exterior. Nas entradas: **1**, **2** e **3**, na mesma lógica.

## Causa

A SEFAZ compara o primeiro dígito do CFOP com as UFs do emitente e do destinatário. Um CFOP 5xxx numa venda de SP para
MG, por exemplo, é recusado.

## Como corrigir no NFeFlow

1. Emitir NF-e › aba **Produtos** › campo **CFOP** de cada item.
2. Confira a UF do destinatário na aba **Destinatário** — o seletor de CFOP considera as duas UFs.
3. Casos mais comuns em vendas:
   - **5102 / 6102**: venda de mercadoria adquirida ou recebida de terceiros (revenda).
   - **5101 / 6101**: venda de produção do próprio estabelecimento.
   - **5405**: venda de mercadoria com ICMS retido anteriormente por substituição tributária (substituído), dentro da UF.
4. Se o produto sempre usa o mesmo CFOP, deixe-o no **CFOP padrão** do cadastro em **Produtos**.

## Regra oficial

A tabela de CFOP é anexo do Convênio s/nº de 15/12/1970 (CONFAZ). O MOC traz as regras de validação que cruzam o CFOP
com o destino da operação (indicador de local de destino).

## Observações por UF / exceções

A natureza exata da operação (revenda, industrialização, remessa, devolução) é decisão do contador — o CFOP deve refletir
a operação real.

> Rascunho gerado com apoio de IA — conferir números, prazos e códigos na fonte oficial antes de publicar.
