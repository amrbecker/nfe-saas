---
id: sistema/cancelar-cce-inutilizar
titulo: "Cancelar NF-e, emitir Carta de Correção e inutilizar série"
categoria: sistema
nivel_fonte: sistema
codigos_rejeicao: []
campos_relacionados: [numero, serie]
telas: [notas, nota-detalhe, inutilizacoes]
fontes:
  - documento: "NFeFlow — tela Notas Emitidas (src/WebUI/Pages/NotasEmitidas.razor)"
    url: null
    nivel: sistema
vigencia_inicio: null
vigencia_fim: null
verificado_em: null
revisar_ate: null
curador: null
status: rascunho
resumo_curto: "Cancele uma NF-e autorizada, emita uma Carta de Correção para erros simples ou inutilize uma série de notas sem emissão."
---

## Resumo

Após emitir uma NF-e autorizada pela SEFAZ, você pode cancelá-la (se não houver manifestação do destinatário), emitir uma Carta de Correção (para erros simples que não afetam cálculo de impostos) ou inutilizar uma série de números quando há erro de segurança.

## Como fazer no NFeFlow

### Cancelar uma NF-e

1. **Abra o menu lateral** e clique em **Notas Emitidas**.

2. **Encontre a nota** que deseja cancelar (status deve ser "Autorizada").

3. **Clique no ícone de Cancelar** (ícone com X, na coluna "Ações").

4. **Confirme o cancelamento** no diálogo que aparecerá.

5. **Digite uma justificativa** (opcional) explicando o motivo do cancelamento.

6. **Clique em "Confirmar"** para transmitir o cancelamento à SEFAZ.

7. **O status da nota mudará para "Cancelada"** após confirmação pela SEFAZ.

### Emitir uma Carta de Correção (CC-e)

1. **Abra o menu lateral** e clique em **Notas Emitidas**.

2. **Clique na nota** que deseja corrigir (deve estar Autorizada).

3. **Na tela de detalhe**, procure por uma opção de **Carta de Correção** (se disponível).

4. **Preencha os dados da correção:**
   - Indique qual campo foi corrigido (ex.: descrição do produto).
   - Descreva brevemente o erro anterior e o valor correto.

5. **Clique em "Emitir CC-e"** para transmitir à SEFAZ.

6. **A CC-e será gerada** com um novo número de protocolo.

### Inutilizar uma série

1. **Abra o menu lateral** e clique em **Inutilizações** (se habilitado no modo).

2. **Clique em "Nova Inutilização"**.

3. **Preencha os dados:**
   - **Série** de notas.
   - **Número Inicial** e **Número Final** (intervalo a inutilizar).
   - **Justificativa** (por que não foram emitidas).

4. **Clique em "Inutilizar"** para transmitir à SEFAZ.

5. **O sistema emitirá um protocolo** de inutilização; as notas não poderão mais ser emitidas.

## Observações

- **Cancelamento** só é permitido se não houver manifestação do destinatário sobre a nota.
- **Carta de Correção** é usada para erros simples (dados de identificação, descrição); não funciona para corrigir valores de impostos ou operação.
- **Inutilização** é para situações em que a nota nunca foi emitida ou foi emitida por erro e não pode ser mais usada.
- Todas as ações (cancelamento, CC-e, inutilização) são transmitidas à SEFAZ em tempo real.
