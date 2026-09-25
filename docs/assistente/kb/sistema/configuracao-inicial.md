---
id: sistema/configuracao-inicial
titulo: "Configuração inicial e personalização da empresa"
categoria: sistema
nivel_fonte: sistema
codigos_rejeicao: []
campos_relacionados: [regime_tributario, ambiente, csc, csc_token]
telas: [configuracao-inicial]
fontes:
  - documento: "NFeFlow — tela Personalização (src/WebUI/Pages/ConfiguracaoInicial.razor)"
    url: null
    nivel: sistema
vigencia_inicio: null
vigencia_fim: null
verificado_em: null
revisar_ate: null
curador: null
status: rascunho
resumo_curto: "Personalize sua empresa na tela de Configuração definindo o regime tributário, ambiente SEFAZ, modo simples/completo e dados para NFC-e."
---

## Resumo

A configuração inicial permite personalizar a forma como o NFeFlow se comporta para sua empresa: qual regime tributário, se emite NF-e ou NFC-e, modo simples (menos campos) ou completo, e dados específicos para NFC-e (CSC e Código do CSC se aplicável).

## Como fazer no NFeFlow

1. **Abra o menu lateral** e clique em **Personalização** (ou acesse via **Config. Empresa** se for a primeira vez).

2. **Seção "Regime Tributário":**
   - Escolha o regime que sua empresa se enquadra:
     - **Simples Nacional**: para empresas no regime de tributação simplificada.
     - **Simples Nacional - Excesso Sublimite**: para empresas que ultrapassam o limite de faturamento.
     - **Regime Normal (Lucro Real/Presumido)**: para empresas fora do Simples.
   - Este campo afeta o preenchimento de impostos na emissão de notas.

3. **Seção "Ambiente SEFAZ":**
   - Escolha entre **Homologação (teste)** ou **Produção**.
   - **Homologação**: use para testes; notas não têm validade fiscal.
   - **Produção**: use para emitir notas com validade fiscal. Só mude para produção quando estiver pronto.

4. **Seção "Modo de Uso":**
   - Escolha entre **Simples** (menos campos, fluxo rápido) ou **Completo** (todos os campos, funcionalidades avançadas).
   - Esta configuração personaliza o que aparece no menu lateral e nos formulários.

5. **Seção "NFC-e" (se aplicável):**
   - Se sua empresa emite NFC-e (Nota Fiscal de Consumidor Eletrônica), preencha:
     - **CSC (Código de Segurança do Contribuinte)**: código fornecido pela SEFAZ do seu estado.
     - **Código do CSC**: dígito de validação fornecido junto com o CSC.
   - Estes dados são obrigatórios para gerar o hash de autenticação da NFC-e.

6. **Clique em "Salvar"** para confirmar as configurações.

## Observações

- A configuração inicial é feita uma única vez; depois você pode alterar a qualquer momento em **Personalização**.
- Se mudar de **Homologação** para **Produção**, tenha certeza de que seu certificado digital está instalado e válido.
- O **Modo Simples** oculta campos e funcionalidades não essenciais; o **Modo Completo** exibe tudo (cadastro de produtos, inutilizações, relatórios avançados).
- CSC e Código do CSC são específicos por UF; se sua empresa atua em vários estados, configure conforme necessário.
