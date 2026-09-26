---
id: preenchimento/ncm-cest
titulo: "NCM válido e quando o CEST é obrigatório"
categoria: preenchimento
nivel_fonte: N1
codigos_rejeicao: [778, 806]
campos_relacionados: [ncm, cest]
telas: [emitir-nfe, produtos]
fontes:
  - documento: "Manual de Orientação do Contribuinte (MOC) — leiaute e regras de validação da NF-e/NFC-e"
    url: "https://www.nfe.fazenda.gov.br/portal/listaConteudo.aspx?tipoConteudo=ndIjl+iEFdE="
    nivel: N1
  - documento: "Convênio ICMS 142/2018 — CEST e substituição tributária"
    url: "https://www.confaz.fazenda.gov.br/legislacao/convenios"
    nivel: N1
  - documento: "Tabela NCM vigente (Receita Federal / Siscomex)"
    url: "https://portalunico.siscomex.gov.br/classif/"
    nivel: N1
vigencia_inicio: null
vigencia_fim: null
verificado_em: null
revisar_ate: null
curador: null
status: rascunho
resumo_curto: "O NCM tem 8 dígitos e precisa existir na tabela vigente (rejeição 778). O CEST tem 7 dígitos e é exigido quando a mercadoria está sujeita a substituição tributária (rejeição 806)."
---

## Resumo

- **NCM**: 8 dígitos, precisa existir na tabela NCM vigente. NCM inexistente gera a rejeição **778**.
- **CEST**: 7 dígitos, identifica mercadorias sujeitas a substituição tributária. Operação com ICMS-ST sem CEST gera a
  rejeição **806**.

## Causa

A SEFAZ valida o NCM contra a tabela oficial. Códigos extintos ou digitados errado são recusados. Quando a operação tem
ICMS-ST e o item não traz o CEST, a nota também é recusada.

## Como corrigir no NFeFlow

1. Emitir NF-e › **Produtos** › campo **NCM**: digite o código ou parte da descrição e escolha da lista (ela vem da tabela oficial).
2. Se o NFeFlow avisar que o NCM exige CEST, preencha o **CEST** no cadastro do produto em **Produtos**.
3. Corrija também o cadastro do produto, para a próxima nota já sair certa — a Ori oferece isso depois da autorização.

## Regra oficial

O CEST e a relação de mercadorias sujeitas a ST estão no Convênio ICMS 142/2018 e seus anexos. As regras de validação de
NCM e CEST estão no MOC.

## Observações por UF / exceções

A obrigatoriedade de ST varia por UF e por protocolo/convênio — confirme com a legislação da UF de destino.

> Rascunho gerado com apoio de IA — conferir números, prazos e códigos na fonte oficial antes de publicar.
