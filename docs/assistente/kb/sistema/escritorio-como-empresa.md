---
id: sistema/escritorio-como-empresa
titulo: "Cadastrar o próprio escritório como empresa emitente"
categoria: sistema
nivel_fonte: sistema
codigos_rejeicao: []
campos_relacionados: [cpf_cnpj, razao_social, regime_tributario, ambiente, cep, codigo_municipio, ie, uf]
telas: [escritorio-como-empresa, empresas]
fontes:
  - documento: "NFeFlow — tela Cadastrar Escritório como Empresa (src/WebUI/Pages/EscritorioComoEmpresa.razor)"
    url: null
    nivel: sistema
vigencia_inicio: null
vigencia_fim: null
verificado_em: null
revisar_ate: null
curador: null
status: rascunho
resumo_curto: "Permita que o próprio escritório emita NF-e em seu nome sem criar uma empresa cliente duplicando dados."
---

## Resumo

Se o escritório (PJ com CNPJ) emite NF-e em seu próprio nome, não precisa criar uma empresa cliente duplicando os dados. Use a opção de cadastrar o escritório como empresa emitente: reutiliza CNPJ, razão social e nome fantasia; você preenche apenas endereço e dados fiscais.

## Como fazer no NFeFlow

1. **Abra o menu lateral** e clique em **Empresas Clientes** (acesso de Admin).

2. **Procure por um botão ou link "Cadastrar Escritório como Empresa"** (ou acesse diretamente via `/escritorio-como-empresa`).

3. **Na página de cadastro, preencha:**

   **Seção "Identificação":**
   - **Inscrição Estadual** (ou "ISENTO" se não for contribuinte do ICMS).
   - **UF** (estado do escritório).

   **Seção "Endereço":**
   - **CEP**: digite o CEP; o sistema busca e preencha automaticamente logradouro, bairro, cidade e código de município.
   - **Cód. IBGE Município** (preenchido automaticamente pelo CEP).
   - **Logradouro** (rua/avenida).
   - **Número**.
   - **Bairro**.
   - **Cidade**.

   **Seção "Dados Fiscais":**
   - **Regime Tributário**: Simples Nacional, Simples Nacional - Excesso Sublimite, ou Regime Normal.
   - **Ambiente SEFAZ**: Homologação (teste) ou Produção.
   - **CNAE** (Classificação Nacional de Atividade Econômica) — opcional; procure por código ou descrição.

4. **Clique em "Cadastrar"** para salvar.

5. **A empresa será criada** usando o CNPJ, razão social e nome fantasia do escritório. Você poderá então emitir notas em seu próprio nome.

## Observações

- O CNPJ, razão social e nome fantasia são **copiados automaticamente** do escritório; não precisa preenchê-los novamente.
- Se já existe uma empresa com o CNPJ do escritório no mesmo Escritório, o sistema retorna a existente (idempotência).
- Se o CNPJ existe em **outro Escritório**, o cadastro é bloqueado (evita "roubo" de CNPJ).
- Após criar a empresa, você pode trocá-la no topo do menu e começar a emitir notas.
- Dados de contato (e-mail, telefone) também vêm do escritório; você pode editá-los depois em **Config. Empresa**.
