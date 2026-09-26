---
id: nfce/csc-qrcode
titulo: "NFC-e: CSC e QR Code"
categoria: nfce
nivel_fonte: N1
codigos_rejeicao: []
campos_relacionados: [csc, csc_token]
telas: [empresa, configuracao-inicial]
fontes:
  - documento: "Manual de Orientação do Contribuinte (MOC) — leiaute e regras de validação da NF-e/NFC-e"
    url: "https://www.nfe.fazenda.gov.br/portal/listaConteudo.aspx?tipoConteudo=ndIjl+iEFdE="
    nivel: N1
  - documento: "Ajuste SINIEF 19/2016 — institui a NFC-e"
    url: "https://www.confaz.fazenda.gov.br/legislacao/ajustes/2016/AJ_019_16"
    nivel: N1
vigencia_inicio: null
vigencia_fim: null
verificado_em: null
revisar_ate: null
curador: null
status: rascunho
resumo_curto: "A NFC-e exige o CSC (Código de Segurança do Contribuinte), obtido no portal da SEFAZ da UF, para gerar o QR Code do DANFE NFC-e. Sem CSC válido, a NFC-e é rejeitada."
---

## Resumo

A **NFC-e** (modelo 65) precisa do **CSC** — identificador e token gerados no portal da SEFAZ da UF — para montar o QR Code
impresso no DANFE NFC-e.

## Como corrigir no NFeFlow

1. Gere o CSC (ID e token) no portal da SEFAZ da sua UF.
2. Informe-os em **Config. Empresa**. O token é guardado cifrado.
3. Nunca envie o token por e-mail ou chat — a Ori também não pede esse dado.

## Regra oficial

A NFC-e foi instituída pelo Ajuste SINIEF 19/2016; o QR Code e o uso do CSC estão no MOC e nas Notas Técnicas da NFC-e.

> Rascunho gerado com apoio de IA — conferir números, prazos e códigos na fonte oficial antes de publicar.
