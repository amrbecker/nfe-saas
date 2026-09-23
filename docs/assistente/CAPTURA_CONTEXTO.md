# Captura de Contexto — O que a coruja precisa saber

> Pergunta do PO: *"Ao ser chamado, ele primeiro identifica qual a possível dúvida: em que tela está, qual campo
> tem o ponto de inserção, o que foi clicado, preenchido, qual operação estava sendo realizada e o rastro da
> sessão. Verificar viabilidade e quais informações realmente são necessárias."*

**Resposta curta: é viável no Blazor WASM e barato.** O segredo é capturar **significado**, não DOM cru. Cada
página diz o que está acontecendo ("emitindo NF-e, aba Produtos, item 2") em vez de mandarmos cliques e HTML
para o modelo. Isso reduz tokens, reduz risco de privacidade e melhora a precisão.

---

## 1. Matriz de necessidade

| Informação | Necessária? | Por quê | Como capturar | Vai para o modelo? |
|------------|-------------|---------|---------------|--------------------|
| **Tela atual** (rota + nome amigável) | ✅ Essencial | Define o universo da dúvida | `NavigationManager.LocationChanged` | Sim |
| **Operação em curso** (ex.: `EmitindoNFe`, etapa `Produtos`, item 2 de 3) | ✅ Essencial | É o melhor preditor da dúvida | Interface `IContextoTela` implementada pela página | Sim |
| **Campo com foco** (id semântico + rótulo) | ✅ Essencial | "Estou no CFOP" → dúvida quase certa | JS `focusin` lendo `data-ajuda="cfop"` | Sim |
| **Valor do campo com foco** | 🟡 Útil | Explicar se o valor está certo | Mesmo listener | Sim, **exceto campos sensíveis** (lista de bloqueio, §3) |
| **Erros de validação visíveis** | ✅ Essencial | Dúvida mais comum depois de rejeição | Página expõe `MudForm.Errors` via `IContextoTela` | Sim |
| **Último erro da API** (status, `codigo`, mensagem) | ✅ Essencial | Detecta bug × erro de preenchimento | `DelegatingHandler` no `HttpClient` da WebUI (buffer dos últimos 5) | Sim |
| **Nota em foco** (id, situação, `MotivoRejeicao`) | ✅ Essencial | Explicar rejeição | Rota + `IContextoTela`; **o servidor busca os dados** | Só o id; o servidor completa |
| **Rastro da sessão** (últimas ~20 ações semânticas) | 🟡 Útil | "Tentou emitir 3 vezes", "voltou da aba Totais" | Buffer circular em memória (`RastroSessao`) | Sim, **resumido** nas últimas 8 ações |
| Cliques brutos (coordenadas, seletores CSS) | ❌ Desnecessário | Ruído e tokens caros; as ações semânticas bastam | — | Não |
| Todos os valores do formulário | ❌ Desnecessário | Privacidade e tokens | — | Não — só os campos com erro e o com foco |
| Tempo em cada tela | 🟡 Útil para CS, não para a resposta | Detectar travamento ("12 min na aba Impostos") | Rastro com timestamp | Não; vai para telemetria (`EventoProduto`) |
| Perfil da empresa, UF, regime, plano | ✅ Essencial | Personaliza a resposta | **Servidor**, a partir do JWT | Sim, montado no servidor |
| Gravação de tela / teclas digitadas | ❌ Proibido | Invasivo e desproporcional (LGPD, finalidade) | — | Nunca |

---

## 2. Modelo de dados (payload enviado ao chamar a coruja)

```json
{
  "tela": "emitir-nfe",
  "op": { "tipo": "EmitindoNFe", "etapa": "Produtos", "item": 2, "itens": 3, "modelo": 55 },
  "foco": { "campo": "cfop", "rotulo": "CFOP", "valor": "5102", "item": 2 },
  "erros": [ { "campo": "ncm", "item": 2, "msg": "NCM não encontrado na tabela" } ],
  "api": [ { "rota": "POST /api/notas", "status": 422, "codigo": "ValidacaoFalhou", "ha_s": 40 } ],
  "notaId": null,
  "rastro": [
    "abriu emitir-nfe",
    "selecionou cliente cadastrado",
    "adicionou item 1 (produto cadastrado)",
    "adicionou item 2 (manual)",
    "tentou emitir → erro de validação",
    "voltou para aba Produtos"
  ]
}
```

Tamanho típico: **250–450 tokens**. O servidor acrescenta o bloco de empresa, plano e data (montado a partir do JWT)
e valida `notaId` contra o `EmpresaId` do token.

---

## 3. Privacidade por padrão

**Nunca capturados** (lista de bloqueio aplicada no JS e revalidada no servidor):
senhas, senha do certificado, token CSC, arquivo `.pfx`, campos `type="password"`, e-mail e telefone de
destinatário.

**Mascarados antes do envio:** CPF → `***.456.789-**`; CNPJ de destinatário → mantido (é dado de empresa), mas
**sem razão social** se for MEI ou produtor rural (pode conter nome de pessoa física).

**Ciclo de vida:**
1. O rastro vive **só na memória do navegador** (não vai para `localStorage`) e é descartado ao sair ou recarregar.
2. **Nada é enviado até o usuário chamar a coruja.**
3. Ao chamar, o painel mostra "Estou vendo que…" com os itens do contexto; o usuário pode remover qualquer um.
4. O contexto enviado é gravado com a conversa (retenção de 180 dias, `ESTRATEGIA.md` R4).
5. Telemetria agregada (`EventoProduto`) é separada: não guarda valores de campos, só eventos e telas.

---

## 4. Hipóteses de dúvida sem LLM (motor de regras)

Tabela de regras avaliada no **cliente**, instantânea e sem custo. As 3 de maior prioridade viram botões.

| Prioridade | Condição | Hipótese exibida | Resposta |
|------------|----------|------------------|----------|
| 100 | `notaId` e situação = Rejeitada | "Por que esta nota foi rejeitada?" | Artigo por código de rejeição → modelo se pedir "explicar melhor" |
| 90 | Último erro da API com status 5xx | "Parece um erro do sistema. Quer que eu registre?" | Fluxo de chamado |
| 80 | `erros` não vazio | "O que está errado no campo {rotulo}?" | Artigo do campo + modelo |
| 70 | `foco.campo` tem artigo de ajuda | "Como preencher {rotulo}?" | Artigo do campo (custo zero) |
| 60 | Rastro com ≥ 2 tentativas de emitir falhas | "Não consegue emitir? Vamos ver juntos." | Modelo com contexto completo |
| 50 | Tela de certificado e upload falhou | "Problema com o certificado?" | Artigo `certificado/*` |
| 10 | Sempre | "Outra dúvida…" | Campo livre |

Mapeamento `campo → artigo` e `código de rejeição → artigo` fica em `docs/assistente/kb/_mapa.json`, gerado a partir
do front matter dos artigos (campo `campos_relacionados` e `codigos_rejeicao`).

---

## 5. Viabilidade técnica no Blazor WASM

| Peça | Implementação | Esforço |
|------|---------------|---------|
| `RastroSessao` (serviço scoped, buffer circular de 50 eventos) | C# puro | P |
| Listener de foco | ~40 linhas de JS (`focusin` e `focusout` em `document`, lê `data-ajuda` e `aria-label`) + `DotNetObjectReference` | P |
| Anotar campos | Atributo `data-ajuda="cfop"` nos `MudTextField`/`MudSelect` (via `UserAttributes`) nas telas fiscais. Começar por `EmitirNFe.razor`, `Produtos.razor`, `Clientes.razor` e `Empresa.razor` | M (mecânico) |
| `IContextoTela` | Interface com `ContextoAtual()` implementada pelas páginas-chave; páginas sem implementação caem só na rota | M |
| Erros da API | `DelegatingHandler` registrado no `HttpClient` (`Program.cs` da WebUI) | P |
| Montagem no servidor | `MontarContextoAssistenteQuery` (empresa, plano, nota validada por tenant) | P |

**Obs.:** o SDK Sentry da WebUI já coleta *breadcrumbs* de navegação para erros. O `RastroSessao` é independente
(semântico e controlado por nós), mas pode anexar o `SentrySdk.LastEventId` quando houver um erro recente, para
ligar o chamado ao evento no Sentry.
