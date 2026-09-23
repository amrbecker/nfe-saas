# Assistente NFeFlow — Análise de Viabilidade e Estratégia

> Documento do Product Owner. Data: 2026-09-23. Status: **proposta aprovada para Fase 0**.
> Documentos irmãos: [`CONTEXTO_AGENTE.md`](CONTEXTO_AGENTE.md) (persona, regras e ferramentas do agente),
> [`BASE_CONHECIMENTO.md`](BASE_CONHECIMENTO.md) (fontes oficiais e governança),
> [`PLANO_ACAO.md`](PLANO_ACAO.md) (fases, épicos e critérios de aceite).
>
> **Atualizado em 2026-09-23 pela fase de pesquisa** ([`PESQUISA_REFINAMENTO.md`](PESQUISA_REFINAMENTO.md)):
> produto renomeado para **NFeFlow**, assistente ganha a forma de uma **coruja** ([`MASCOTE_UX.md`](MASCOTE_UX.md)),
> modelo passa a ser **DeepSeek** (§6), captura de contexto ([`CAPTURA_CONTEXTO.md`](CAPTURA_CONTEXTO.md)),
> automações ([`AUTOMACOES.md`](AUTOMACOES.md)) e monitoramento de fontes ([`MONITORAMENTO_FONTES.md`](MONITORAMENTO_FONTES.md)).

---

## 1. A ideia em uma frase

Um assistente que vive dentro do NFeFlow, **enxerga o contexto do usuário** (empresa selecionada, nota
aberta, rejeição recebida, certificado, plano) e atua em quatro papéis:

| Papel | O que faz | Para quem gera valor |
|-------|-----------|----------------------|
| **Suporte** | Responde dúvidas de uso e explica rejeições SEFAZ em linguagem clara, com fonte citada | Usuário (contador/auxiliar) |
| **Detector de bugs** | Percebe erros (API, Sentry, rejeições anômalas), coleta contexto técnico e abre chamado pronto para correção | Time de desenvolvimento |
| **Consultor técnico-fiscal** | Orienta sobre regras de preenchimento da NF-e/NFC-e (MOC, NTs, CST/CSOSN, IBS/CBS) com base em fontes oficiais | Usuário |
| **Customer Success** | Monitora saúde da conta, age antes do problema (certificado vencendo, trial acabando, rejeições repetidas, onboarding travado) e alimenta o PO com dores e oportunidades | Negócio (retenção, conversão) e produto |

---

## 2. Veredito: **VIÁVEL — com escopo faseado e três condições**

| Dimensão | Nota | Justificativa |
|----------|------|---------------|
| Técnica | 🟢 Alta | Stack já tem os pontos de apoio: MediatR (novo Command/Query), `MotivoRejeicao` na `NotaFiscal`, `AuditLog`, Sentry na API **e** na WebUI, `PersonalizacaoService` para gatear a UI, workers em background (`NcmUpdateWorker`), Postgres (pgvector disponível no Neon quando precisar de RAG). DeepSeek expõe API compatível com OpenAI (tool calls, JSON, streaming) — integrável via `Microsoft.Extensions.AI`. |
| Valor de negócio | 🟢 Alta | Rejeição SEFAZ é a dor nº 1 de quem emite nota; o público (escritórios contábeis) atende muitas empresas e tem pouco tempo. Um SaaS em piloto, sem equipe de suporte, ganha escala de atendimento sem contratar. |
| Custo variável | 🟢 Muito baixo | Com DeepSeek: ~US$ 0,006–0,02 por conversa (§6). Custo deixa de ser o limitante; a cota existe contra abuso. |
| Regulatório / jurídico | 🟢 com condições (antes 🟡) | Guarda-corpos definidos: níveis de fonte N1–N4 com linguagem própria, verificador determinístico de números e datas, selo de confiança e formato fixo de resposta (`CONTEXTO_AGENTE.md` §3). **Condição nova:** a hospedagem do DeepSeek precisa atender à LGPD (§6 e `PESQUISA_REFINAMENTO.md` §2.2). |
| Operacional | 🟢 com condições (antes 🟡) | O monitor automático detecta e resume as novidades das fontes oficiais (DOU via INLABS, Portal NF-e, SVRS, CONFAZ, SEFAZ). O curador só revisa e aprova (`MONITORAMENTO_FONTES.md`). Ainda exige um dono nomeado. |
| Diferenciação | 🟢 Alta, se bem feito | Chat genérico virou commodity. O diferencial defensável é **contexto da nota + base fiscal curada e citada + CS proativo** — algo que um ChatGPT externo não tem. |

**Condições para seguir (gates):**

1. **O agente nunca executa ação fiscal** (emitir, cancelar, CC-e, inutilizar). Ele lê, explica, sugere e
   prepara rascunhos; quem clica é o humano. Ferramentas de escrita limitam-se a chamado, feedback e
   preferências de UI.
2. **Toda afirmação fiscal cita fonte oficial com data de verificação**; sem fonte na base → o agente diz
   que não sabe e registra uma "lacuna de conhecimento".
3. **Instrumentar antes de conversar**: sem telemetria de produto (hoje só existem `AuditLog` e Sentry),
   o papel de CS e a "coleta de informação privilegiada" não têm dado para trabalhar. A Fase 0 entrega
   valor mesmo se o LLM nunca for ligado.

---

## 3. Reenquadramentos importantes (onde a ideia original precisa de ajuste)

| Ideia original | Risco | Reenquadramento |
|----------------|-------|-----------------|
| "Consultor para os usuários" | Os usuários **são contadores**. Uma consultoria tributária rasa ou errada destrói confiança em uma conversa. Planejamento tributário é responsabilidade técnica do contador (CRC). | Copiloto **técnico-operacional**: regras de preenchimento e validação da NF-e, significado de rejeições, uso do sistema. Não faz planejamento tributário nem enquadramento — aponta a fonte e devolve a decisão ao contador. |
| "Coleta de informações privilegiadas" | LGPD (finalidade, transparência). Dados de clientes dos escritórios não são nossos. | **Inteligência de produto agregada e consentida**: dores, pedidos de funcionalidade, fricções de UX, anonimizadas e agregadas por escritório. Nada de usar conteúdo de notas para outro fim. Termo de uso atualizado + aviso no widget. |
| "Tornar o sistema perfeito para cada usuário" | Objetivo não mensurável; tentação de o agente reconfigurar coisas sozinho. | Metas mensuráveis (seção 7) + personalização **sugerida** (ex.: "você nunca usa NFC-e — quer ocultar?"), aplicada via `ConfiguracaoEmpresa`/`PersonalizacaoService` só com confirmação. |
| "Perceber bugs e enviar para correção" | Chamados ruidosos ou com dados sensíveis vazando para ferramentas externas. | Chamado estruturado (rota, versão, `EmpresaId`, id do evento Sentry, passos) **com dados pessoais mascarados**, triado por humano antes de virar issue. |
| "Vive ativamente no sistema" | Assistente intrusivo irrita (efeito "Clippy"). | Coruja no rodapé do menu (espaço vazio), animada em espera, que **só fala quando chamada**; dicas proativas apenas como **balões de pensamento silenciosos** com limite de frequência (`MASCOTE_UX.md` §5). |

---

## 4. Análise de mercado e posicionamento

**Concorrência:** emissores de NF-e ligados a ERPs de PME, APIs de emissão e plataformas para
contadores. Vários já anunciam "IA", em geral chat genérico ou geração de texto.
*Ação da Fase 0:* benchmark de 5 concorrentes (testar o assistente de cada um com 10 perguntas reais de
rejeição) para confirmar o diferencial antes do marketing.

**Posicionamento proposto:**
> "O único emissor que explica a rejeição da sua nota, diz o que corrigir citando o Manual da SEFAZ e
> avisa antes do seu certificado vencer."

**SWOT do assistente**

| Forças | Fraquezas |
|--------|-----------|
| Contexto nativo (nota, empresa, rejeição, certificado) | Equipe pequena para curadoria contínua |
| Base de código limpa e modular (CQRS/MediatR) | Sem telemetria de produto hoje |
| Público de nicho com dor clara e recorrente | Infra free tier (cold start do Render degrada a experiência de chat) |
| **Oportunidades** | **Ameaças** |
| Reforma Tributária (IBS/CBS 2026–2033) gera onda de dúvidas — quem explicar melhor ganha | Resposta fiscal errada → dano reputacional e possível responsabilização |
| Upsell: assistente como diferencial dos planos Profissional/Enterprise | Custo de IA crescer sem cota |
| Dados agregados de rejeição por UF/tipo viram conteúdo e produto (relatório de conformidade) | Concorrente grande copiar a funcionalidade |

---

## 5. Riscos e mitigação

| # | Risco | Prob. | Impacto | Mitigação |
|---|-------|-------|---------|-----------|
| R1 | Alucinação em regra fiscal | Média | Alto | Resposta ancorada na base curada; citação obrigatória; "não sei" explícito; avaliação automatizada (eval) com 100+ perguntas antes de cada mudança de prompt/modelo; aviso "confirme com a legislação vigente" em temas de tributação |
| R2 | Vazamento entre tenants | Baixa | Crítico | Ferramentas executam no servidor com `EmpresaId`/`EscritorioId` **do JWT**, nunca de parâmetro do modelo; testes de integração de isolamento para cada ferramenta |
| R3 | Prompt injection via dados (descrição de produto, nome de destinatário, texto de rejeição) | Média | Médio | Ferramentas só-leitura; ações de escrita exigem confirmação na UI; dados de ferramentas marcados como dados, não instruções |
| R4 | LGPD (ver também R9 em `PESQUISA_REFINAMENTO.md`: transferência para a China se usar a API direta da DeepSeek) | Média | Alto | Base legal (execução de contrato + legítimo interesse documentado), termo de uso e aviso no widget, retenção de conversas 180 dias, mascaramento de CPF em chamados/insights, transferência internacional coberta por cláusulas contratuais do provedor; confirmar política de retenção de dados do provedor de IA |
| R5 | Custo descontrolado | Baixa | Médio | Cota mensal por plano, limite de tokens por conversa, cache de prompt, kill switch `Assistente__Habilitado` |
| R6 | Base de conhecimento desatualizada | Alta | Alto | Cada artigo tem `verificado_em` e `revisar_ate`; job semanal lista artigos vencidos; checagem mensal do Portal NF-e por NTs novas; dono nomeado |
| R7 | Latência / cold start | Alta (free tier) | Médio | Streaming de resposta; mensagem "acordando o servidor"; migrar API para plano pago antes de abrir para todos |
| R8 | Dependência de um fornecedor de IA | Média | Médio | Interface `IAssistenteIA` em `Application/` + `Microsoft.Extensions.AI` (`IChatClient`) em `Infrastructure/` — trocar de provedor ou endpoint (Foundry ↔ API DeepSeek ↔ outro) é configuração |

---

## 6. Arquitetura alvo (resumo)

```
WebUI (Blazor)                       API (ASP.NET)                          Infra
┌───────────────────┐   SSE/stream  ┌──────────────────────────────┐      ┌────────────────────────┐
│ Coruja + painel   │──────────────▶│ AssistenteController          │      │ DeepSeekChatClient      │
│  (MudDrawer)      │               │  └─ EnviarMensagemCommand     │─────▶│ (M.E.AI, OpenAI-compat.)│
│ Botão "Explicar   │               │      Handler (MediatR)        │      │ tool calls + cache auto │
│  rejeição"        │               │  ├─ monta contexto da tela    │      └────────────────────────┘
│ Alertas de CS     │               │  ├─ executa ferramentas       │      ┌────────────────────────┐
└───────────────────┘               │  │   (escopo = JWT)           │─────▶│ Postgres: Conversa,     │
                                    │  └─ grava Conversa/Mensagem   │      │ Mensagem, Chamado,      │
                                    │ SaudeContaWorker (CS diário)  │      │ SinalProduto, Evento    │
                                    │ InsightsWorker (semanal/batch)│      └────────────────────────┘
                                    └──────────────────────────────┘
Base de conhecimento: docs/assistente/kb/*.md (versionada no git, carregada no prompt com cache).
Quando passar de ~150k tokens → migrar para busca (pgvector no Neon).
```

**Decisões:**

- **Sem RAG vetorial no início; roteamento determinístico de artigos.** Prefixo fixo curto (persona + regras) +
  1 a 3 artigos escolhidos por código de rejeição, tela ou campo (`kb/_mapa.json`). Mais barato e preciso que mandar a
  base inteira. Busca vetorial (pgvector) entra quando o roteamento não der conta.
- **Contexto de tela injetado pelo cliente** (rota atual, `NotaId` aberta) mas **dados buscados pelo
  servidor** via ferramentas escopadas pelo JWT.
- **CS proativo é determinístico primeiro** (regras SQL no worker); o LLM só redige a mensagem e
  sumariza insights semanais (em lote, fora do pico da DeepSeek — metade do preço).

**Modelo: DeepSeek** (decisão do PO, 2026-09-23). Detalhes, fontes e técnicas de economia de tokens em
`PESQUISA_REFINAMENTO.md` §2.

| Rota | Modelo | Endpoint | Custo aproximado |
|------|--------|----------|------------------|
| Conversa e explicação de rejeição (contém dado de cliente) | `deepseek-flash`; V4-Pro nas perguntas fiscais se o eval exigir | **Microsoft Foundry** (residência de dados, contrato de tratamento Microsoft) | ~US$ 0,006–0,02 por conversa (preço da API direta; conferir o preço no Foundry) |
| Monitoramento de fontes públicas, rascunhos da base, sumarização de sinais já anonimizados | `deepseek-flash` | API oficial DeepSeek, em lote fora do pico | Centavos por mês |

O horário comercial brasileiro cai na faixa **fora do pico** da DeepSeek, com preço pela metade. O cache é
automático por prefixo: o prefixo precisa ser byte-idêntico (nada de data ou nome do usuário no início).

---

## 7. Métricas de sucesso (North Star e KPIs)

**North Star:** *% de notas rejeitadas que viram autorizadas em até 24h* (mede suporte + consultoria
num número que o cliente sente).

| KPI | Linha de base (medir na Fase 0) | Meta 6 meses |
|-----|--------------------------------|--------------|
| Taxa de rejeição por nota emitida | medir | −30% |
| Tempo até a 1ª nota autorizada (onboarding) | medir | −40% |
| Conversão trial → pago | medir | +20% relativo |
| Conversas resolvidas sem humano | — | ≥ 70% |
| Avaliação 👍 das respostas | — | ≥ 85% |
| Precisão no eval fiscal (100 perguntas) | — | ≥ 95%, 0 respostas perigosas |
| Chamados de bug com contexto completo | — | ≥ 90% |
| Certificados que venceram sem aviso | medir | 0 |
| Custo IA / receita do escritório | — | ≤ 1% |
| Usuários que silenciaram as dicas da coruja | — | ≤ 15% |
| Sugestões de automação aceitas (salvar cadastro, emitir igual, lembrete) | — | ≥ 40% |

---

## 8. Modelo de negócio

| Plano | Assistente |
|-------|-----------|
| Básico | Coruja com hipóteses e artigos (sem limite, custo zero) + explicar rejeição + automações onda 1 — 150 mensagens ao modelo/usuário/mês |
| Profissional | + consultor técnico-fiscal com ferramentas, padrões de preenchimento, lembretes de nota recorrente — 600 mensagens/usuário/mês |
| Enterprise | + CS proativo, relatório mensal de saúde e rejeições por empresa — 2.000 mensagens/usuário/mês |

Limite diário padrão: 40 mensagens por usuário (`PESQUISA_REFINAMENTO.md` §2.5).

Trial: experiência Profissional completa (o assistente ajuda a converter o trial).

---

## 9. Decisões pendentes do PO

1. ~~Modelo de IA~~ → **DeepSeek no Microsoft Foundry** (decidido); o modelo **não recebe dado pessoal** (`PESQUISA_REFINAMENTO.md` §2.6). Pendente: `flash` × V4-Pro após o eval.
2. ~~Nome da coruja~~ → **Ori**.
3. ~~Curadoria~~ → **escritório parceiro** (acordo formal pendente — `PESQUISA_REFINAMENTO.md` §4).
4. Momento de migrar Render/Neon para plano pago (recomendado antes da Fase 2).
5. ~~Revisão dos termos~~ → **escritório parceiro**, com as cláusulas de LGPD e responsabilidade vistas por um advogado (`PESQUISA_REFINAMENTO.md` §4).
6. ~~Produção do mascote~~ → IA via MCP + acabamento humano (`MASCOTE_UX.md` §7). Pendente: escolher caminho gratuito (ComfyUI local ou HF + VTracer + Inkscape) após o protótipo; Recraft grátis não serve (sem uso comercial).
7. Lista final de referências N3 admitidas (`MONITORAMENTO_FONTES.md` §2).
