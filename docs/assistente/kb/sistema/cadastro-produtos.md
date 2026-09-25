---
id: sistema/cadastro-produtos
titulo: "Cadastrar produtos para reutilizar em notas"
categoria: sistema
nivel_fonte: sistema
codigos_rejeicao: []
campos_relacionados: [ncm, cest, unidade, gtin]
telas: [produtos]
fontes:
  - documento: "NFeFlow — tela Produtos (src/WebUI/Pages/Produtos.razor)"
    url: null
    nivel: sistema
vigencia_inicio: null
vigencia_fim: null
verificado_em: null
revisar_ate: null
curador: null
status: rascunho
resumo_curto: "Cadastre produtos com código, descrição, NCM e outras informações uma vez e reutilize em futuras emissões de notas."
---

## Resumo

O cadastro de produtos permite guardar produtos usados frequentemente (código, descrição, NCM, unidade, GTIN) para reutilizá-los rapidamente ao emitir notas. Economiza tempo em operações repetitivas.

## Como fazer no NFeFlow

### Acessar o cadastro de produtos

1. **Abra o menu lateral** e clique em **Produtos** (disponível se o modo não for tão simplificado).

2. **Uma tabela com produtos já cadastrados aparecerá**.

3. **Clique em "Novo Produto"** (ou botão similar) para adicionar um novo.

### Cadastrar um novo produto

1. **Na tela de cadastro, preencha:**

   **Identificação:**
   - **Código**: identificador único do produto (ex: SKU interno).
   - **Descrição**: nome do produto, conforme aparecerá na nota.
   - **GTIN** (opcional): código de barras (EAN/UPC).

   **Classificação Fiscal:**
   - **NCM**: procure por código ou descrição (ex: 6204.62.00 para camisetas).
   - **CEST** (opcional): Código de Especificação do Substituto Tributário.

   **Unidade e Quantidade:**
   - **Unidade de Medida**: UN, KG, L, M, etc.

2. **Clique em "Salvar"**.

3. **O produto será criado** e aparecerá na lista.

### Usar um produto cadastrado ao emitir uma nota

1. **Abra uma nota (nova ou existente)** na aba **"Produtos"**.

2. **Clique em "Adicionar Produto"**.

3. **Na autocomplete "Selecionar produto cadastrado"**, comece a digitar o código ou descrição.

4. **O sistema mostrará produtos que correspondem**; clique em um para selecioná-lo.

5. **Os campos de NCM, unidade e outras informações serão preenchidos automaticamente**.

6. **Complete apenas quantidade, valor unitário e desconto** conforme a operação.

### Editar um produto

1. **Na lista de produtos**, clique no produto para editar.

2. **Altere os dados** e clique em "Salvar".

3. **A mudança afeta** somente novas notas; notas já emitidas mantêm os dados originais.

### Deletar um produto

1. **Na lista de produtos**, clique no ícone de deletar (lixeira).

2. **Confirme a exclusão**.

3. **O produto será removido** do cadastro (mas notas que o usaram não são afetadas).

## Observações

- **Código do produto** deve ser único no cadastro da empresa.
- **NCM** é obrigatório para qualquer produto que entre em uma nota (mesmo que deixe em branco durante cadastro, você preencherá ao emitir).
- **Produtos são específicos da empresa selecionada**; cada empresa tem seu próprio cadastro.
- **GTIN é opcional** mas facilita identificação de produtos com código de barras.
- **Reutilize** produtos cadastrados sempre que possível para manter consistência e agilizar emissão.
