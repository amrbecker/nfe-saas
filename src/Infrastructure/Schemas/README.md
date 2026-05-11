# Schemas XSD para Validação Fiscal

Esta pasta contém schemas XSD usados pelo `XsdValidationService` para validar
XMLs gerados antes da transmissão à SEFAZ.

## Arquivos atuais (skeletons)

| Arquivo | Cobertura | Conformidade |
|---|---|---|
| `nfe-skeleton.xsd` | NFe / NFC-e v4.00 — `<nfeProc>` e `<NFe>` com estrutura raiz, `infNFe` Id, tags obrigatórias | **Não-oficial** — apenas estrutura de alto nível |
| `evento-skeleton.xsd` | Eventos v1.00 — `<envEvento>`, CC-e, Manifestação, Cancelamento (110111) | **Não-oficial** |
| `inutilizacao-skeleton.xsd` | Inutilização v4.00 — `<inutNFe>` com `<infInut>` | **Não-oficial** |

Estes skeletons validam:
- Raiz com namespace correto (`http://www.portalfiscal.inf.br/nfe`)
- Atributo `versao` obrigatório
- Atributo `Id` no formato esperado (regex)
- Tags obrigatórias presentes na ordem correta
- Tipos de dados básicos (CNPJ 14 dígitos, chave 44 dígitos, etc.)
- Conteúdo de blocos complexos (`ide`, `emit`, `dest`, etc.) é aceito como `xs:any`

**Não validam:**
- Regras de negócio fiscais (totais batem, CST × CFOP, etc.)
- Valores monetários, alíquotas, formatos numéricos detalhados
- Cardinalidades específicas dentro dos blocos

## Para conformidade SEFAZ completa

Substitua os arquivos `*-skeleton.xsd` pelos XSDs oficiais:

1. **NFe v4.00**: baixe o pacote em
   <https://www.nfe.fazenda.gov.br/portal/listaConteudo.aspx?tipoConteudo=/fwLvLUSmU8=>
   - Coloque `procNFe_v4.00.xsd`, `nfe_v4.00.xsd`, `leiauteNFe_v4.00.xsd`,
     `tiposBasico_v4.00.xsd`, `xmldsig-core-schema_v1.01.xsd` nesta pasta

2. **Eventos**: <https://www.nfe.fazenda.gov.br/portal/listaConteudo.aspx?tipoConteudo=fwhWHbvAaq8=>
   - `envEvento_v1.00.xsd`, `leiauteCCe_v1.00.xsd`, etc.

3. **NFC-e**: schemas adicionais específicos.

O `XsdValidationService` carrega **todos os arquivos `*.xsd` desta pasta** no startup
e usa o `XmlSchemaSet` resultante para validar qualquer XML cujo namespace de raiz
case com algum schema. Adicionar XSDs oficiais não exige mudança de código.

## Reload em runtime

Os schemas são carregados uma vez no startup (singleton). Para recarregar após
alterar arquivos, reinicie o container da API:

```powershell
docker compose restart api
```
