---
id: sistema/email-danfe
titulo: "Enviar NF-e e DANFE por e-mail"
categoria: sistema
nivel_fonte: sistema
codigos_rejeicao: []
campos_relacionados: [email]
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
resumo_curto: "Envie uma NF-e autorizada e seu PDF (DANFE) ao destinatário via e-mail diretamente da lista de notas."
---

## Resumo

Após uma NF-e ser autorizada pela SEFAZ, você pode enviar o XML e o PDF (DANFE — Documento Auxiliar de Nota Fiscal Eletrônica) ao destinatário por e-mail com um clique. O envio é manual, nunca automático.

## Como fazer no NFeFlow

1. **Abra o menu lateral** e clique em **Notas Emitidas**.

2. **Procure a nota autorizada** que deseja enviar (status deve ser "Autorizada").

3. **Clique no ícone de E-mail** (envelope, na coluna "Ações" da nota).

4. **Uma janela aparecerá** para confirmar o e-mail do destinatário:
   - O e-mail vem do formulário da nota; você pode editá-lo antes de enviar.
   - Verifique se está correto.

5. **Clique em "Enviar"** para transmitir o XML e o PDF ao destinatário.

6. **Uma confirmação aparecerá** indicando sucesso ou erro no envio.

### Alternativa: Enviar da tela de detalhe da nota

1. **Clique no ícone de visualização** (olho) da nota na lista.

2. **Na tela de detalhe**, procure por um botão **"Enviar por E-mail"**.

3. **Confirme o e-mail** e clique em "Enviar".

## Observações

- **Envio manual**: a nota não é enviada automaticamente após autorização; você controla quando enviar.
- **O e-mail do destinatário** deve estar preenchido na nota para enviar com sucesso.
- **Se houver erro** (e-mail inválido, serviço de e-mail indisponível), uma mensagem aparecerá na tela.
- **Assunto e corpo do e-mail** são pré-configurados; você não pode personalizá-los, mas pode reenviar a mesma nota quantas vezes quiser.
- O **XML original** e a **DANFE em PDF** vêm anexados ao e-mail.
