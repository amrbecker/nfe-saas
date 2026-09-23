---
id: rejeicoes/exemplo-familia            # caminho sem extensão; é o identificador citado pelo agente
titulo: "Título claro, como o usuário perguntaria"
categoria: rejeicoes                     # sistema | rejeicoes | preenchimento | eventos | certificado | nfce | reforma-tributaria | problemas-conhecidos
codigos_rejeicao: []                     # ex.: [225, 226] quando aplicável
fontes:
  - documento: "MOC — Manual de Orientação do Contribuinte, versão X.XX, seção Y"
    url: "https://www.nfe.fazenda.gov.br/..."
vigencia_inicio: null                    # AAAA-MM-DD quando a regra tem início definido
vigencia_fim: null                       # AAAA-MM-DD quando a regra deixa de valer
verificado_em: AAAA-MM-DD
revisar_ate: AAAA-MM-DD                  # padrão: verificado_em + 90 dias (30 para Reforma Tributária)
curador: nome
status: rascunho                         # rascunho | revisado | publicado — só "publicado" entra no prompt
---

## Resumo

Uma a três frases com a resposta direta.

## Causa

O que a SEFAZ está validando e por que a nota falhou.

## Como corrigir no NfeSaas

1. Menu → Tela → Campo (nomes exatamente como aparecem na interface).
2. ...

## Regra oficial

Resumo da regra com referência à seção do documento oficial. Não copiar o texto integral.

## Observações por UF / exceções

Somente se houver fonte específica.
