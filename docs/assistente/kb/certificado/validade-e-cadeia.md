---
id: certificado/validade-e-cadeia
titulo: "Certificado digital A1: validade, senha e cadeia"
categoria: certificado
nivel_fonte: N1
codigos_rejeicao: []
campos_relacionados: [certificado, senha_certificado]
telas: [certificado]
fontes:
  - documento: "ICP-Brasil — normas e cadeia de Autoridades Certificadoras (ITI)"
    url: "https://www.gov.br/iti/pt-br/assuntos/repositorio"
    nivel: N1
  - documento: "Manual de Orientação do Contribuinte (MOC) — leiaute e regras de validação da NF-e/NFC-e"
    url: "https://www.nfe.fazenda.gov.br/portal/listaConteudo.aspx?tipoConteudo=ndIjl+iEFdE="
    nivel: N1
vigencia_inicio: null
vigencia_fim: null
verificado_em: null
revisar_ate: null
curador: null
status: rascunho
resumo_curto: "O certificado A1 (.pfx/.p12) precisa estar válido, pertencer ao CNPJ do emitente e ser da cadeia ICP-Brasil. Certificado vencido ou com senha errada impede a assinatura e a emissão."
---

## Resumo

Para assinar a NF-e, o certificado **A1** precisa estar **dentro da validade**, ser do **CNPJ do emitente** e pertencer à
cadeia **ICP-Brasil**.

## Como corrigir no NFeFlow

1. Menu **Certificado Digital**: veja validade e situação do certificado atual.
2. Para renovar, envie o novo arquivo .pfx/.p12 com a senha — a senha é guardada cifrada e nunca é exibida.
3. A Ori avisa quando faltam 30, 15, 7 e 1 dia para o vencimento.

## Regra oficial

A assinatura digital da NF-e com certificado ICP-Brasil está prevista no Ajuste SINIEF 07/05 e detalhada no MOC.

> Rascunho gerado com apoio de IA — conferir números, prazos e códigos na fonte oficial antes de publicar.
