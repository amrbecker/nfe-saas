---
id: rejeicoes/destinatario-ie
titulo: "IE do destinatário e indicador de contribuinte"
categoria: rejeicoes
nivel_fonte: N1
codigos_rejeicao: [232, 233]
campos_relacionados: [ie, ind_ie, cpf_cnpj, uf]
telas: [emitir-nfe, clientes]
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
resumo_curto: "Rejeições de IE do destinatário surgem quando o indicador de contribuinte não combina com a IE informada: contribuinte exige IE válida da UF do destinatário; não contribuinte não deve informar IE."
---

## Resumo

Quando o destinatário é **contribuinte do ICMS**, a IE precisa ser informada e válida para a UF dele. Quando é **não
contribuinte**, a IE não deve ser informada. Divergências geram rejeições de IE do destinatário (ex.: 232 e 233).

## Causa

Cadastro do destinatário com IE em branco, IE de outra UF, formato inválido, ou indicador de contribuinte incorreto.

## Como corrigir no NFeFlow

1. Em **Destinatários**, abra o cadastro e confira **Inscrição Estadual** e o **indicador de IE**.
2. Na emissão, a aba **Destinatário** mostra a IE usada — corrija antes de reenviar.
3. Pessoa física ou empresa não contribuinte: deixe a IE em branco e use o indicador de não contribuinte.

## Regra oficial

As validações de IE do destinatário e do indicador (indIEDest) estão no MOC.

## Observações por UF / exceções

A situação cadastral da IE pode ser consultada no Sintegra da UF do destinatário.

> Rascunho gerado com apoio de IA — conferir números, prazos e códigos na fonte oficial antes de publicar.
