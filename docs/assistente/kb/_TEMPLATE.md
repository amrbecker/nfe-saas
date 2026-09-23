---
id: rejeicoes/exemplo-familia            # caminho sem extensão; é o identificador citado pelo agente
titulo: "Título claro, como o usuário perguntaria"
categoria: rejeicoes                     # sistema | rejeicoes | preenchimento | eventos | certificado | nfce | reforma-tributaria | problemas-conhecidos
nivel_fonte: N1                          # N1 normativo | N2 orientação oficial | N3 referência técnica (ver BASE_CONHECIMENTO §1.1)
codigos_rejeicao: []                     # ex.: [225, 226] quando aplicável — alimenta kb/_mapa.json
campos_relacionados: []                  # ids de data-ajuda dos campos da UI, ex.: [cfop, cst] — alimenta kb/_mapa.json
telas: []                                # rotas, ex.: [emitir-nfe]
fontes:
  - documento: "MOC — Manual de Orientação do Contribuinte, versão X.XX, seção Y"
    url: "https://www.nfe.fazenda.gov.br/..."
    nivel: N1
vigencia_inicio: null                    # AAAA-MM-DD quando a regra tem início definido
vigencia_fim: null                       # AAAA-MM-DD quando a regra deixa de valer
verificado_em: AAAA-MM-DD
revisar_ate: AAAA-MM-DD                  # padrão: verificado_em + 90 dias (30 para Reforma Tributária)
curador: nome
status: rascunho                         # rascunho | revisado | publicado | em_revisao — só publicado/em_revisao entram no prompt
                                         # em_revisao = monitor detectou mudança na fonte; coruja avisa "regra em atualização"
resumo_curto: ""                         # ≤ 280 caracteres — exibido sem chamar o modelo (resposta de custo zero)
---

## Resumo

Uma a três frases com a resposta direta.

## Causa

O que a SEFAZ está validando e por que a nota falhou.

## Como corrigir no NFeFlow

1. Menu → Tela → Campo (nomes exatamente como aparecem na interface).
2. ...

## Regra oficial

Resumo da regra com referência à seção do documento oficial. Não copiar o texto integral.

## Observações por UF / exceções

Somente se houver fonte específica.
