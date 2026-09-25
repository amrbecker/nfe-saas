---
id: sistema/certificado-upload
titulo: "Fazer upload e validar certificado digital A1"
categoria: sistema
nivel_fonte: sistema
codigos_rejeicao: []
campos_relacionados: [certificado, senha_certificado]
telas: [certificado]
fontes:
  - documento: "NFeFlow — tela Certificado Digital (src/WebUI/Pages/Certificado.razor)"
    url: null
    nivel: sistema
vigencia_inicio: null
vigencia_fim: null
verificado_em: null
revisar_ate: null
curador: null
status: rascunho
resumo_curto: "Faça upload de um certificado digital A1 (arquivo .pfx ou .p12) na tela Certificado Digital fornecendo a senha para validação."
---

## Resumo

O certificado digital A1 é necessário para assinar e transmitir NF-e à SEFAZ. No NFeFlow, você carrega o arquivo de certificado (.pfx ou .p12) e sua senha em uma tela dedicada; o sistema valida e armazena de forma segura.

## Como fazer no NFeFlow

1. **Abra o menu lateral** e clique em **Certificado Digital**.

2. **Seção "Status do Certificado":**
   - Verifique o status atual:
     - **✓ Válido** (verde): certificado carregado e válido.
     - **✗ Inválido ou não configurado** (vermelho): nenhum certificado ou certificado vencido.
   - Se válido, aparecem os dados: **Titular**, **CNPJ** e **Validade**.
   - Se o certificado estiver **vencendo em breve** (menos de 30 dias), aparecerá um aviso.

3. **Seção "Carregar Novo Certificado":**
   - Clique em **Selecionar Arquivo (.pfx / .p12)** para escolher seu arquivo de certificado.
   - O sistema aceita apenas arquivos `.pfx` ou `.p12` (limite de 5 MB).

4. **Preencha a "Senha do Certificado":**
   - Digite a senha que você definiu ao gerar ou exportar o certificado.
   - Use o ícone de olho para visualizar/ocultar a senha enquanto digita.

5. **Clique em "Enviar Certificado":**
   - O sistema valida o arquivo, verifica a senha e os dados do certificado.
   - Se tudo estiver correto, o certificado é armazenado de forma criptografada.
   - Se houver erro (senha incorreta, arquivo inválido, certificado vencido), uma mensagem aparecerá.

## Observações

- O certificado é armazenado de forma **segura e criptografada** no servidor; a senha não é salva.
- Você pode carregar um novo certificado a qualquer momento (substitui o anterior).
- Se o certificado vencer, você receberá um aviso; faça upload de um novo certificado antes que expire.
- Certificados A1 são válidos por 1 ano (em geral); guarde a senha em local seguro.
- Se perder a senha do certificado, você precisará solicitar um novo certificado à sua Autoridade Certificadora.
