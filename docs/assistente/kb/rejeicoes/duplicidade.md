---
id: rejeicoes/duplicidade
titulo: "Duplicidade de NF-e (rejeições 204 e 539)"
categoria: rejeicoes
nivel_fonte: N1
codigos_rejeicao: [204, 539]
campos_relacionados: [serie, numero]
telas: [emitir-nfe, notas, nota-detalhe]
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
resumo_curto: "204: já existe NF-e autorizada com o mesmo número e série. 539: mesmo número e série, mas com chave diferente. Consulte a nota original antes de reenviar e não reutilize o número."
---

## Resumo

- **204 — Duplicidade de NF-e**: a SEFAZ já tem uma nota autorizada com essa série e número.
- **539 — Duplicidade de NF-e com diferença na chave de acesso**: há uma nota com a mesma série e número, mas com outra chave.

## Causa

Normalmente acontece em reenvio depois de falha de comunicação, ou quando a numeração da empresa foi alterada ou usada
em outro sistema.

## Como corrigir no NFeFlow

1. Veja em **Notas emitidas** se a nota original já aparece como autorizada.
2. O NFeFlow protege contra reenvio duplicado da mesma tentativa; se a numeração foi usada fora do NFeFlow, ajuste a
   próxima numeração em **Config. Empresa**.
3. Se precisar, use **Pendente de retransmissão** apenas para a mesma nota — nunca gere outra nota com o mesmo número.

## Regra oficial

As regras de unicidade da chave de acesso e da numeração estão no MOC (validação de duplicidade).

> Rascunho gerado com apoio de IA — conferir números, prazos e códigos na fonte oficial antes de publicar.
