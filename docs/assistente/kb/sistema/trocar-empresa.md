---
id: sistema/trocar-empresa
titulo: "Trocar de empresa selecionada para emitir notas"
categoria: sistema
nivel_fonte: sistema
codigos_rejeicao: []
campos_relacionados: []
telas: [dashboard]
fontes:
  - documento: "NFeFlow — MainLayout (src/WebUI/Shared/MainLayout.razor)"
    url: null
    nivel: sistema
vigencia_inicio: null
vigencia_fim: null
verificado_em: null
revisar_ate: null
curador: null
status: rascunho
resumo_curto: "Selecione qual empresa deseja usar para emitir notas usando o seletor no topo da interface."
---

## Resumo

Se seu escritório tem múltiplas empresas clientes, você pode trocar entre elas usando o seletor no topo da tela. Todas as operações (emissão, consulta de notas, cadastro de produtos e clientes) são específicas da empresa selecionada.

## Como fazer no NFeFlow

### Acessar o seletor de empresa

1. **Abra o NFeFlow após fazer login**.

2. **No topo da tela (logo abaixo da barra superior)**, veja um **seletor mostrando a empresa selecionada**:
   - Aparece o ícone de empresa.
   - Nome da empresa (ou razão social).
   - CNPJ da empresa.

### Trocar de empresa

1. **Clique no seletor de empresa** no topo.

2. **Uma lista de empresas aparecerá** com todas as empresas vinculadas ao seu escritório.

3. **Clique na empresa desejada**.

4. **A troca é imediata:**
   - Você será redirecionado ao dashboard.
   - Menu lateral, configurações e dados agora são da empresa selecionada.
   - Qualquer nota que você emita ou consulte será daquela empresa.

### Se você tem apenas uma empresa

1. **O seletor mostrará apenas essa empresa** (sem opção de trocar).

2. **Você continuará usando apenas essa empresa** para todas as operações.

## Observações

- **Dados isolados por empresa**: cada empresa tem seus próprios:
  - Notas emitidas.
  - Cadastro de produtos.
  - Cadastro de clientes.
  - Certificado digital.
  - Configurações (regime tributário, ambiente SEFAZ).

- **Usuários compartilhados**: todos os usuários do escritório veem todas as empresas; a troca de empresa é individual.

- **Menu lateral personalizável**: dependendo das configurações da empresa, o menu pode mostrar mais ou menos opções (modo Simples vs. Completo).

- **Após trocar**, qualquer ação que você fazer (emitir nota, consultar histórico, etc.) será para a empresa selecionada.
