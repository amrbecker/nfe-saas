---
id: sistema/cadastro-destinatarios
titulo: "Cadastrar clientes/destinatários para reutilizar em notas"
categoria: sistema
nivel_fonte: sistema
codigos_rejeicao: []
campos_relacionados: [cpf_cnpj, razao_social, email, telefone]
telas: [clientes]
fontes:
  - documento: "NFeFlow — tela Clientes (src/WebUI/Pages/Clientes.razor)"
    url: null
    nivel: sistema
vigencia_inicio: null
vigencia_fim: null
verificado_em: null
revisar_ate: null
curador: null
status: rascunho
resumo_curto: "Cadastre clientes com CNPJ/CPF, razão social, contato e endereço uma vez e reutilize ao emitir notas."
---

## Resumo

O cadastro de destinatários (clientes) permite guardar dados de clientes frequentes para reutilizá-los rapidamente ao emitir notas, evitando redigitação e garantindo consistência de dados.

## Como fazer no NFeFlow

### Acessar o cadastro de clientes

1. **Abra o menu lateral** e clique em **Destinatários** (ou **Clientes**, dependendo do rótulo).

2. **Uma tabela com clientes já cadastrados aparecerá**.

3. **Clique em "Novo Cliente"** (ou botão similar) para adicionar um novo.

### Cadastrar um novo cliente

1. **Na tela de cadastro, preencha:**

   **Identificação:**
   - **Tipo de Pessoa**: Pessoa Física (CPF) ou Pessoa Jurídica (CNPJ).
   - **CPF/CNPJ**: documento do cliente.
   - **Razão Social** (para PJ) ou **Nome** (para PF).
   - **Nome Fantasia** (opcional): apelido ou nome de marca.

   **Contato:**
   - **E-mail**: para comunicação e envio de notas.
   - **Telefone** (opcional).

   **Endereço:**
   - **CEP**: digite para preencher automaticamente logradouro, bairro, cidade e município.
   - **Logradouro**: rua/avenida.
   - **Número**.
   - **Complemento** (opcional): apto, sala, etc.
   - **Bairro**.
   - **Cidade**.
   - **UF** (estado).
   - **Código Município (IBGE)** (preenchido automaticamente pelo CEP).

   **Dados Fiscais:**
   - **Inscrição Estadual** (opcional; deixe em branco se não contribuinte).

2. **Clique em "Salvar"**.

3. **O cliente será criado** e aparecerá na lista.

### Usar um cliente cadastrado ao emitir uma nota

1. **Abra uma nota (nova ou existente)** na aba **"Destinatário"**.

2. **Na autocomplete "Selecionar cliente cadastrado"**, comece a digitar o nome, CNPJ ou CPF.

3. **O sistema mostrará clientes que correspondem**; clique em um para selecioná-lo.

4. **Os campos de contato, endereço e dados fiscais serão preenchidos automaticamente**.

5. **Você pode editar qualquer campo** se houver alteração específica para aquela nota.

### Editar um cliente

1. **Na lista de clientes**, clique no cliente para editar.

2. **Altere os dados** e clique em "Salvar".

3. **A mudança afeta** somente novas notas; notas já emitidas mantêm os dados que estavam no momento da emissão.

### Deletar um cliente

1. **Na lista de clientes**, clique no ícone de deletar (lixeira).

2. **Confirme a exclusão**.

3. **O cliente será removido** do cadastro (mas notas que o usaram não são afetadas).

## Observações

- **CPF/CNPJ** devem ser únicos no cadastro da empresa.
- **E-mail** é importante para enviar DANFE e notas por e-mail.
- **Clientes são específicos da empresa selecionada**; cada empresa tem seu próprio cadastro.
- **Você pode emitir para um cliente não cadastrado** preenchendo os dados manualmente; a reutilização é opcional.
- **Inscrição Estadual** pode ser deixada em branco se o cliente for contribuinte em outros estados ou não for contribuinte; o sistema valida conforme a regra fiscal.
