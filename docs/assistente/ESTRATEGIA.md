# Assistente Sideral — Análise de Viabilidade e Estratégia

> Documento do Product Owner. Data: 2026-09-23. Status: **proposta aprovada para Fase 0**.
> Documentos irmãos: [`CONTEXTO_AGENTE.md`](CONTEXTO_AGENTE.md) (persona, regras e ferramentas do agente),
> [`BASE_CONHECIMENTO.md`](BASE_CONHECIMENTO.md) (fontes oficiais e governança),
> [`PLANO_ACAO.md`](PLANO_ACAO.md) (fases, épicos e critérios de aceite).

---

## 1. A ideia em uma frase

Um assistente que vive dentro do NfeSaas, **enxerga o contexto do usuário** (empresa selecionada, nota
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
| Técnica | 🟢 Alta | Stack já tem os pontos de apoio: MediatR (novo Command/Query), `MotivoRejeicao` na `NotaFiscal`, `AuditLog`, Sentry na API **e** na WebUI, `PersonalizacaoService` para gatear a UI, workers em background (`NcmUpdateWorker`), Postgres (pgvector disponível no Neon quando precisar de RAG). SDK oficial Anthropic para C# existe e suporta tool use e streaming. |
| Valor de negócio | 🟢 Alta | Rejeição SEFAZ é a dor nº 1 de quem emite nota; o público (escritórios contábeis) atende muitas empresas e tem pouco tempo. Um SaaS em piloto, sem equipe de suporte, ganha escala de atendimento sem contratar. |
| Custo variável | 🟢 Controlável | Estimativa de US$ 0,05–0,50 por conversa (seção 6). Com cota por plano e cache de prompt, fica < 5% do ticket esperado. |
| Regulatório / jurídico | 🟡 Médio | Resposta fiscal errada pode gerar autuação do cliente do contador. LGPD: conversas contêm CPF/CNPJ de destinatários. Mitigável com regras rígidas (seção 5). |
| Operacional | 🟡 Médio | O maior custo **não é a IA, é manter a base de conhecimento atualizada** (NTs mudam, Reforma Tributária em transição 2026–2033). Exige rotina de curadoria com dono definido. |
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
| "Vive ativamente no sistema" | Assistente intrusivo irrita (efeito "Clippy"). | Proatividade por **gatilhos de alto valor** (rejeição, certificado, trial, onboarding parado) e no máximo 1 intervenção proativa por sessão. Resto é sob demanda. |

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
| R4 | LGPD | Média | Alto | Base legal (execução de contrato + legítimo interesse documentado), termo de uso e aviso no widget, retenção de conversas 180 dias, mascaramento de CPF em chamados/insights, transferência internacional coberta por cláusulas contratuais do provedor; confirmar política de retenção de dados do provedor de IA |
| R5 | Custo descontrolado | Baixa | Médio | Cota mensal por plano, limite de tokens por conversa, cache de prompt, kill switch `Assistente__Habilitado` |
| R6 | Base de conhecimento desatualizada | Alta | Alto | Cada artigo tem `verificado_em` e `revisar_ate`; job semanal lista artigos vencidos; checagem mensal do Portal NF-e por NTs novas; dono nomeado |
| R7 | Latência / cold start | Alta (free tier) | Médio | Streaming de resposta; mensagem "acordando o servidor"; migrar API para plano pago antes de abrir para todos |
| R8 | Dependência de um fornecedor de IA | Média | Médio | Interface `IAssistenteIA` em `Application/`, implementação em `Infrastructure/` — trocar de provedor não toca handlers nem UI |

---

## 6. Arquitetura alvo (resumo)

```
WebUI (Blazor)                       API (ASP.NET)                          Infra
┌───────────────────┐   SSE/stream  ┌──────────────────────────────┐      ┌────────────────────────┐
│ AssistenteWidget  │──────────────▶│ AssistenteController          │      │ ClaudeAssistenteService │
│  (MudDrawer)      │               │  └─ EnviarMensagemCommand     │─────▶│  (SDK Anthropic C#)     │
│ Botão "Explicar   │               │      Handler (MediatR)        │      │  tool use + cache       │
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

- **Sem RAG no início.** A base curada da Fase 1 (~30–80k tokens) cabe no prompt e fica em cache —
  mais simples, mais preciso e mais barato que manter embeddings. RAG entra quando a base crescer.
- **Contexto de tela injetado pelo cliente** (rota atual, `NotaId` aberta) mas **dados buscados pelo
  servidor** via ferramentas escopadas pelo JWT.
- **CS proativo é determinístico primeiro** (regras SQL no worker); o LLM só redige a mensagem e
  sumariza insights semanais (Message Batches, 50% mais barato, não precisa de tempo real).

**Estimativa de custo** (preços de tabela, set/2026; câmbio assumido R$ 5,50/US$):

Premissas por conversa: prompt fixo (persona + base) ~40k tokens em cache, 5 turnos, ~2k tokens novos e
~1k de saída (incluindo raciocínio) por turno.

| Modelo | Entrada US$/MTok | Saída US$/MTok | Custo/conversa (aprox.) | 40 conversas/escritório/mês |
|--------|------------------|----------------|-------------------------|-----------------------------|
| Claude Opus 5 (padrão recomendado para qualidade fiscal) | 5,00 | 25,00 | ~US$ 0,40 | ~US$ 16 (≈ R$ 88) |
| Claude Sonnet 5 | 2,00 | 10,00 | ~US$ 0,16 | ~US$ 6,40 (≈ R$ 35) |
| Claude Haiku 4.5 | 1,00 | 5,00 | ~US$ 0,08 | ~US$ 3,20 (≈ R$ 18) |

"Explicar rejeição" (chamada única, sem conversa): ~US$ 0,02–0,06. A escolha do modelo é decisão do PO
após o eval da Fase 1: rodar as 100 perguntas nos três e escolher o mais barato que atingir a meta de
precisão. Cache de leitura custa ~10% da entrada — o prompt fixo **precisa** ser byte-idêntico entre
requisições (nada de data/hora ou nome do usuário no início do prompt).

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
| Custo IA / receita do escritório | — | ≤ 5% |

---

## 8. Modelo de negócio

| Plano | Assistente |
|-------|-----------|
| Básico | Explicar rejeição + FAQ de uso — 30 conversas/mês |
| Profissional | + consultor técnico-fiscal com ferramentas — 150 conversas/mês |
| Enterprise | + CS proativo, relatório mensal de saúde e rejeições por empresa — ilimitado com uso justo |

Trial: experiência Profissional completa (o assistente ajuda a converter o trial).

---

## 9. Decisões pendentes do PO

1. Modelo de IA definitivo (após eval da Fase 1).
2. Nome/persona do assistente (sugestão: "Sideral", alinhado ao domínio `sideral.app.br`).
3. Quem é o dono da curadoria da base de conhecimento (sugestão: contador parceiro, 4h/mês remuneradas).
4. Momento de migrar Render/Neon para plano pago (recomendado antes da Fase 2).
5. Revisão jurídica do termo de uso (LGPD + limitação de responsabilidade da orientação fiscal).
