---
id: sistema/emitir-nfe
titulo: "Como emitir uma NF-e no NFeFlow"
categoria: sistema
nivel_fonte: sistema
codigos_rejeicao: []
campos_relacionados: [serie, numero, natureza_operacao, cfop, ncm, cst, quantidade, valor_unitario, regime_tributario, ambiente, cpf_cnpj, razao_social, email, cep]
telas: [emitir-nfe]
fontes:
  - documento: "NFeFlow — tela Emitir NF-e (src/WebUI/Pages/EmitirNFe.razor)"
    url: null
    nivel: sistema
vigencia_inicio: null
vigencia_fim: null
verificado_em: null
revisar_ate: null
curador: null
status: rascunho
resumo_curto: "Emita uma NF-e preenchendo o tipo de nota, destinatário, itens, pagamento e observações em abas do formulário. Depois de salva, a nota é transmitida à SEFAZ."
---

## Resumo

Emitir uma NF-e no NFeFlow envolve preencher um formulário em abas com os dados da nota (tipo, configuração, destinatário, itens, pagamento) e depois transmiti-la à SEFAZ. A nota pode ser salva como rascunho antes de ser enviada.

## Como fazer no NFeFlow

1. **Abra o menu lateral** e clique em **Emitir NF-e** (ou use o botão **Nova Nota Fiscal** na página de notas emitidas).

2. **Aba "Configuração":**
   - Escolha **Tipo de Nota**: NF-e (55) ou NFC-e (65, se habilitado).
   - Escolha **Tipo de Operação**: Saída ou Entrada.
   - Escolha **Finalidade**: Normal, Complementar ou Devolução.

3. **Aba "Destinatário":**
   - Selecione um cliente já cadastrado na autocomplete, ou preencha manualmente.
   - Escolha **Tipo de Pessoa**: Pessoa Física (CPF) ou Pessoa Jurídica (CNPJ).
   - Preencha **CPF/CNPJ**, **Nome/Razão Social**, **E-mail** (opcional), **Inscrição Estadual** (em branco se não contribuinte).
   - Na seção **Endereço**, digite o **CEP** para preencher automaticamente logradouro, bairro, cidade e município. Complemente com **Número** e **Bairro** se necessário.

4. **Aba "Produtos":**
   - Clique em **Adicionar Produto** para cada item da nota.
   - Selecione um produto já cadastrado, ou preencha os dados manualmente:
     - **Código** do produto.
     - **Descrição**.
     - **NCM** (Nomenclatura Comum do Mercosul) — procure por código ou descrição.
     - **CFOP** (Código Fiscal de Operação) — escolha conforme o tipo de operação, UF de origem e destino.
     - **UN** (unidade de medida, ex.: UN, KG, L).
     - **Qtd** (quantidade).
     - **V.Unit. R$** (valor unitário).
     - **Desconto R$** (opcional).
   - Remova itens clicando no ícone de lixeira.

5. **Aba "Impostos":**
   - Preencha os dados de tributação conforme o regime tributário da empresa:
     - **Simples Nacional**: CST Simples (101, 102, etc.).
     - **Lucro Real/Presumido**: CST Normal (00, 10, 20, etc.), ICMS, PIS, COFINS.
   - O sistema calcula automaticamente quando aplicável.

6. **Aba "Pagamento":**
   - Escolha **Forma de Pagamento**: Dinheiro, Cheque, Cartão Débito, Cartão Crédito, etc.
   - Preencha **Valor** (preenchido automaticamente com o total da nota).
   - Se parcelado, adicione parcelas com datas de vencimento.

7. **Aba "Observações":**
   - Adicione **Informações Adicionais** ao fisco ou ao cliente (opcional).

8. **Salve ou emita:**
   - Clique em **Salvar Rascunho** para guardar sem enviar à SEFAZ.
   - Clique em **Emitir** para transmitir a nota à SEFAZ. A nota mudará para o status "Processando" e depois "Autorizada" (sucesso) ou "Rejeitada" (erro).

## Observações

- Todos os campos obrigatórios estão marcados com um asterisco (*).
- Se a nota for rejeitada pela SEFAZ, o erro aparecerá na tela. Corrija os dados e tente novamente.
- Notas em rascunho permanecem no formulário para edição; notas emitidas aparecem em "Notas Emitidas" no menu lateral.
- O CNPJ da empresa emitente é preenchido automaticamente com base na empresa selecionada no topo do menu.
- NFC-e (modelo 65) é para vendas ao consumidor final; NF-e (modelo 55) é para operações em geral.
