---
id: eventos/contingencia
titulo: "SEFAZ indisponível e contingência (SVC-AN / SVC-RS)"
categoria: eventos
nivel_fonte: N1
codigos_rejeicao: []
campos_relacionados: []
telas: [emitir-nfe, nota-detalhe]
fontes:
  - documento: "Manual de Orientação do Contribuinte (MOC) — leiaute e regras de validação da NF-e/NFC-e"
    url: "https://www.nfe.fazenda.gov.br/portal/listaConteudo.aspx?tipoConteudo=ndIjl+iEFdE="
    nivel: N1
  - documento: "Portal Nacional da NF-e — Disponibilidade dos autorizadores"
    url: "https://www.nfe.fazenda.gov.br/portal/disponibilidade.aspx"
    nivel: N1
vigencia_inicio: null
vigencia_fim: null
verificado_em: null
revisar_ate: null
curador: null
status: rascunho
resumo_curto: "Quando o autorizador da UF está fora do ar, a NF-e pode ser autorizada em contingência pela SEFAZ Virtual (SVC-AN ou SVC-RS, conforme a UF). O NFeFlow tenta a contingência e, se nada responder, deixa a nota pendente de retransmissão."
---

## Resumo

Se o autorizador da UF não responde, a NF-e pode ser autorizada pela **SEFAZ Virtual de Contingência** — **SVC-AN** ou
**SVC-RS**, conforme a UF do emitente.

## Como corrigir no NFeFlow

1. O NFeFlow tenta o autorizador normal e, em falha de comunicação, a contingência correspondente à UF.
2. Se nenhum responder, a nota fica como **Pendente de retransmissão**: abra o detalhe e use **Retransmitir para a SEFAZ**
   quando o serviço voltar. O número não é consumido de novo.
3. Você pode pedir à Ori para consultar a situação da SEFAZ agora.

## Regra oficial

A contingência SVC e a relação de UFs atendidas por SVC-AN e SVC-RS estão no MOC e nas Notas Técnicas de contingência.

> Rascunho gerado com apoio de IA — conferir números, prazos e códigos na fonte oficial antes de publicar.
