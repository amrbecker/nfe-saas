---
id: rejeicoes/schema
titulo: "Falha no schema XML (rejeição 225)"
categoria: rejeicoes
nivel_fonte: N1
codigos_rejeicao: [225]
campos_relacionados: []
telas: [emitir-nfe, nota-detalhe]
fontes:
  - documento: "Manual de Orientação do Contribuinte (MOC) — leiaute e regras de validação da NF-e/NFC-e"
    url: "https://www.nfe.fazenda.gov.br/portal/listaConteudo.aspx?tipoConteudo=ndIjl+iEFdE="
    nivel: N1
  - documento: "Pacote de schemas XSD vigente (Portal Nacional da NF-e)"
    url: "https://www.nfe.fazenda.gov.br/portal/listaConteudo.aspx?tipoConteudo=BMPFMBoln3w="
    nivel: N1
vigencia_inicio: null
vigencia_fim: null
verificado_em: null
revisar_ate: null
curador: null
status: rascunho
resumo_curto: "A rejeição 225 indica que o XML não segue o schema oficial: campo com tamanho ou formato inválido, campo obrigatório vazio ou caractere não permitido. Normalmente é defeito do sistema — vale abrir um chamado."
---

## Resumo

A rejeição **225 — Falha no Schema XML** significa que o arquivo da nota não obedece ao schema (XSD) oficial.

## Causa

Campo com tamanho ou formato fora do padrão, campo obrigatório ausente ou caractere especial não permitido. O NFeFlow valida
o XML contra o schema antes de enviar, então essa rejeição costuma indicar um caso não previsto pelo sistema.

## Como corrigir no NFeFlow

1. Revise textos longos ou com caracteres especiais (descrição do produto, informações adicionais).
2. Se o erro continuar, peça à Ori para **abrir um chamado** — o contexto técnico da tela vai junto, sem dados pessoais.

## Regra oficial

O schema XSD vigente é publicado no Portal Nacional da NF-e junto com as Notas Técnicas; o MOC define a validação de schema.

> Rascunho gerado com apoio de IA — conferir números, prazos e códigos na fonte oficial antes de publicar.
