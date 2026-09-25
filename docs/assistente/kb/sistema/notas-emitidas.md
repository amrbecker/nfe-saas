---
id: sistema/notas-emitidas
titulo: "Consultar e gerenciar notas emitidas"
categoria: sistema
nivel_fonte: sistema
codigos_rejeicao: []
campos_relacionados: [numero, serie]
telas: [notas, nota-detalhe]
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
resumo_curto: "Visualize todas as notas emitidas, seus status e detalhes; baixe DANFE, cancele ou retransmita conforme necessário."
---

## Resumo

A página de "Notas Emitidas" lista todas as NF-e e NFC-e da empresa selecionada, mostrando seu status (Autorizada, Rejeitada, Cancelada, etc.), destinatário, valor e data. Daí você pode visualizar detalhes, baixar DANFE, enviar por e-mail, cancelar ou retransmitir.

## Como fazer no NFeFlow

### Acessar a lista de notas emitidas

1. **Abra o menu lateral** e clique em **Notas Emitidas**.

2. **Uma tabela aparecerá** com as colunas:
   - **Número**: série-número (ex: 001-000000001).
   - **Tipo**: NF-e ou NFC-e.
   - **Destinatário**: nome/razão social do cliente (ou "Consumidor Final" se venda ao consumidor).
   - **Valor Total**: em reais.
   - **Data Emissão**: quando a nota foi criada.
   - **Situação**: status atual (Autorizada, Rejeitada, Cancelada, Processando, etc.).
   - **Ações**: botões para interagir com a nota.

### Filtrar ou buscar notas

1. **Use os filtros no topo** (se disponíveis):
   - Por **data**, **status**, **destinatário**, etc.

2. **Ou role a tabela** para ver mais notas (paginação automática).

### Visualizar detalhes de uma nota

1. **Clique no ícone de visualização** (olho) na coluna "Ações".

2. **A tela de detalhe mostra:**
   - Resumo das informações (emitente, destinatário, itens, impostos).
   - XML original (se autorizada).
   - Protocolo SEFAZ (número de autorização).
   - Histórico de eventos (transmissão, autorização, etc.).

### Baixar DANFE (PDF)

1. **Clique no ícone de PDF** (na coluna "Ações").
   - Só funciona se a nota estiver **Autorizada**.

2. **O PDF será gerado** e baixado automaticamente.

3. **Use o DANFE para:**
   - Imprimir e anexar à documentação.
   - Enviar ao cliente junto com a nota.

### Enviar por e-mail

1. **Clique no ícone de E-mail** (na coluna "Ações").
   - Só funciona se a nota estiver **Autorizada**.

2. **Confirme o e-mail do destinatário** e clique em "Enviar".

3. **O XML e PDF serão enviados** ao e-mail do cliente.

### Cancelar uma nota

1. **Clique no ícone de Cancelar** (X vermelho).
   - Só funciona se a nota estiver **Autorizada** e sem manifestação do destinatário.

2. **Confirme** e digite uma justificativa (opcional).

3. **O cancelamento é transmitido** à SEFAZ.

### Retransmitir uma nota

1. **Se a nota estiver com status "Pendente Retransmissão"**, clique no ícone de **Retransmitir** (seta circular).

2. **A nota será reenviada** à SEFAZ.

3. **Útil se houve erro na transmissão** anterior e a SEFAZ não recebeu.

## Observações

- **Paginação**: a tabela mostra 10 notas por página; use os controles de paginação no rodapé.
- **Status "Processando"**: a nota foi enviada mas ainda aguarda resposta da SEFAZ (geralmente alguns segundos).
- **Status "Rejeitada"**: a SEFAZ não aceitou; verifique o erro na tela de detalhe e corrija.
- **Status "Cancelada"**: a nota foi cancelada com sucesso.
- **Notas em rascunho** (não transmitidas) **não aparecem** nesta lista; elas ficam no formulário de emissão.
