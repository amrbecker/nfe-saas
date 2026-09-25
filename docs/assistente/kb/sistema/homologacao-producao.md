---
id: sistema/homologacao-producao
titulo: "Diferenciar homologação (teste) e produção"
categoria: sistema
nivel_fonte: sistema
codigos_rejeicao: []
campos_relacionados: [ambiente]
telas: [configuracao-inicial, empresa]
fontes:
  - documento: "NFeFlow — instruções gerais no CLAUDE.md (Ambiente SEFAZ)"
    url: null
    nivel: sistema
vigencia_inicio: null
vigencia_fim: null
verificado_em: null
revisar_ate: null
curador: null
status: rascunho
resumo_curto: "Use Homologação para testes sem valor fiscal; mude para Produção apenas quando estiver pronto para emitir notas oficiais."
---

## Resumo

O NFeFlow oferece dois ambientes: **Homologação (teste)** para aprender e validar configurações, e **Produção** para emitir notas com validade fiscal. Você escolhe qual ambiente usar ao configurar a empresa.

## Como fazer no NFeFlow

### Usar Homologação (ambiente de teste)

1. **Abra o menu lateral** e clique em **Personalização** (ou **Config. Empresa**).

2. **Na seção "Ambiente SEFAZ"**, escolha **Homologação (teste)**.

3. **Clique em "Salvar"**.

4. **Todas as notas emitidas** irão para o ambiente de teste da SEFAZ:
   - Notas **não têm validade fiscal**.
   - **Não geram vínculo com o fisco** (CNPJs de teste são permitidos).
   - Útil para **validar configurações, testar fluxos e treinar** o uso do sistema.

### Usar Produção

1. **Abra o menu lateral** e clique em **Personalização** (ou **Config. Empresa**).

2. **Na seção "Ambiente SEFAZ"**, escolha **Produção**.

3. **Clique em "Salvar"**.

4. **Todas as notas emitidas** irão para o ambiente oficial da SEFAZ:
   - Notas **têm validade fiscal**.
   - **Obrigação de transmissão**: você precisa cumprir prazos legais de emissão.
   - **Certificado digital obrigatório** e válido.

### Mudar de Homologação para Produção

1. **Certifique-se de que:**
   - Seu certificado digital A1 está instalado e válido (verifique em **Certificado Digital**).
   - Você testou o fluxo completo em Homologação.
   - Seus dados de empresa estão corretos em **Config. Empresa**.

2. **Siga os passos acima** para ativar Produção.

3. **A mudança é imediata**; novas notas serão transmitidas à SEFAZ oficial.

## Observações

- **Homologação** usa CNPJs de teste (ex: 99.999.999/0001-91); qualquer certificado válido funciona.
- **Produção** exige seu CNPJ real e certificado digital emitido por uma Autoridade Certificadora reconhecida.
- **Uma vez em produção**, todas as notas precisam estar conformes com a legislação fiscal; erros resulta em rejeição pela SEFAZ.
- **Não é possível voltar** uma nota de produção para teste; só é possível cancelá-la.
- **Planeje a mudança** para um dia útil durante o horário de funcionamento do suporte, caso precise de ajuda.
