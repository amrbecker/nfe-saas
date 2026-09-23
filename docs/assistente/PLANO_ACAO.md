# Assistente Sideral — Plano de Ação

> Premissa de capacidade: 1 dev full-stack + PO + curador fiscal parcial (4h/semana nas Fases 0–1).
> Cada fase termina num **gate go/no-go** com métricas. Uma fase só começa quando o gate da anterior passa.
> Estimativas em semanas corridas; datas a partir de 2026-09-28.

```
Fase 0  Fundação (sem IA)            ██████                      sem 1–3   (28/09 – 16/10)
Fase 1  MVP "Explicar rejeição"            ██████                sem 4–6   (19/10 – 06/11)
Fase 2  Assistente com ferramentas               ████████        sem 7–10  (09/11 – 04/12)
Fase 3  CS proativo + insights                           ████████  sem 11–14 (07/12 – 15/01, com recesso)
Fase 4  Evolução contínua                                          a partir de 2027
```

---

## Fase 0 — Fundação (3 semanas) · *entrega valor sem IA*

**Objetivo:** medir a linha de base, montar a base de conhecimento e resolver pré-requisitos jurídicos.

| # | Épico / Tarefa | Onde no código | Critério de aceite |
|---|----------------|----------------|--------------------|
| 0.1 | **Telemetria de produto**: entidade `EventoProduto` (`EscritorioId`, `EmpresaId?`, `UsuarioId`, `Tipo`, `Tela`, `DadosJson`, `OcorridoEm`) + `RegistrarEventoCommand` + endpoint `POST /api/eventos` em lote | `Domain/Entities`, `Application/Commands`, nova migration | Eventos de login, seleção de empresa, abertura de tela, emissão iniciada/concluída/rejeitada gravados; sem CPF/CNPJ de destinatário no payload |
| 0.2 | Query de **linha de base**: taxa de rejeição, tempo até 1ª nota autorizada, top 30 `MotivoRejeicao`, conversão trial→pago | `Application/Queries` + script SQL em `docs/assistente/sql/` | Números da seção 7 da estratégia preenchidos |
| 0.3 | **Alerta de certificado vencendo** (determinístico): e-mail ao Admin em 30/15/7/1 dias via `IEmailService` + banner na WebUI | Novo `SaudeContaWorker` em `API/Workers` (modelo do `NcmUpdateWorker`) | Empresa com certificado vencendo em ≤ 30 dias recebe e-mail uma vez por marco; teste de integração |
| 0.4 | **Base de conhecimento P1**: artigos `sistema/*` P1, `certificado/*`, `preenchimento/ncm-cest` e as 10 rejeições mais frequentes | `docs/assistente/kb/` | 20+ artigos com status `revisado`, fontes e `verificado_em` preenchidos |
| 0.5 | **Script de validação da base**: checa front matter, lista artigos com `revisar_ate` vencido, conta tokens da base | `scripts/kb-validar.*` | Roda no CI; falha se artigo `publicado` sem fonte |
| 0.6 | **Eval fiscal v1**: 100 perguntas reais (fórum interno, suporte, rejeições do banco) com resposta de referência e fonte, 10 perguntas "armadilha" (fora da base, planejamento tributário, pedido de dado de outra empresa) | `tests/NfeSaas.Tests.Assistente/eval/` | Revisado pelo curador |
| 0.7 | **Jurídico/LGPD**: atualizar termo de uso e política de privacidade (assistente, finalidade, retenção 180 dias, operador de IA, transferência internacional); registro da avaliação de legítimo interesse | fora do código | Texto aprovado; checkbox de aceite no próximo login |
| 0.8 | Benchmark de 5 concorrentes (10 perguntas de rejeição em cada) | `docs/assistente/benchmark.md` | Tabela comparativa confirmando (ou não) o diferencial |

**Gate 0 → 1:** linha de base medida · 20 artigos revisados · eval pronto · termo aprovado.

---

## Fase 1 — MVP "Explicar esta rejeição" (3 semanas)

**Objetivo:** provar precisão e valor no caso de maior dor, com o menor risco (chamada única, sem conversa, sem ferramentas de escrita).

| # | Tarefa | Onde | Critério de aceite |
|---|--------|------|--------------------|
| 1.1 | Interface `IAssistenteIA` (Application) + `ClaudeAssistenteService` (Infrastructure) usando o SDK oficial Anthropic para C#; prompt de sistema e base carregados de arquivo; cache de prompt; configuração `Assistente__Habilitado`, `Assistente__Modelo`, `Anthropic__ApiKey` | `Application/Interfaces`, `Infrastructure/Services`, `DependencyInjection.cs`, `render.yaml` | Sem chave ou flag desligada → funcionalidade oculta, sem erro |
| 1.2 | `ExplicarRejeicaoQuery(notaId)` → handler valida tenant, monta contexto da nota (CPF mascarado), chama IA, retorna explicação + passos + fontes | `Application/Queries` | Teste de integração: nota de outra empresa → 404 |
| 1.3 | Botão **"Explicar rejeição"** em `NotaDetalhe.razor` quando status = Rejeitada; resposta em painel com fontes clicáveis e 👍/👎 | `WebUI/Pages/NotaDetalhe.razor` | Gateado por flag nova `MostrarAssistente` no `PersonalizacaoService` |
| 1.4 | Persistir `InteracaoAssistente` (pergunta, resposta, fontes, modelo, tokens, custo, avaliação) | nova entidade + migration | Painel SQL de custo por escritório |
| 1.5 | **Rodar eval** nos modelos candidatos (Opus 5, Sonnet 5, Haiku 4.5) e registrar precisão/custo | `tests/NfeSaas.Tests.Assistente` | Relatório para decisão do PO |
| 1.6 | Piloto com 3–5 escritórios (flag por escritório) | config | Termo aceito pelos pilotos |

**Gate 1 → 2:** eval ≥ 95% e 0 respostas perigosas · ≥ 80% 👍 no piloto · custo médio ≤ US$ 0,06/explicação · nenhum incidente de isolamento.

---

## Fase 2 — Assistente conversacional com ferramentas (4 semanas)

**Pré-requisito de infra:** API fora do free tier do Render (cold start inviabiliza chat).

| # | Tarefa | Onde | Critério de aceite |
|---|--------|------|--------------------|
| 2.1 | Entidades `Conversa` e `MensagemConversa` (retenção 180 dias, job de expurgo) | Domain + migration | Expurgo testado |
| 2.2 | `EnviarMensagemAssistenteCommand` com loop de ferramentas; endpoint `POST /api/assistente/conversas/{id}/mensagens` com **streaming** (SSE) | `API/Controllers/AssistenteController.cs` | Primeiro token < 3s com servidor quente |
| 2.3 | Ferramentas de leitura da seção 4.1 do `CONTEXTO_AGENTE.md`, cada uma com teste de isolamento multi-tenant | `Application/Services/Assistente/Ferramentas/` | 100% das ferramentas com teste "outra empresa → vazio" |
| 2.4 | `AssistenteWidget` (MudDrawer lateral, acessível de todas as páginas via `MainLayout.razor`), recebe rota e `notaId` atuais | `WebUI/Components`, `WebUI/Shared/MainLayout.razor` | Funciona em 400px; aviso de privacidade visível |
| 2.5 | Entidade `Chamado` + ferramenta `abrir_chamado` com confirmação na UI; anexa rota, versão, `EventId` do Sentry (`SentrySdk.LastEventId` na WebUI) e últimas exceções da empresa | Domain, Application, WebUI | Chamado de teste chega com contexto completo e CPF mascarado |
| 2.6 | Tela interna de triagem de chamados (role `SuperAdmin` nova ou acesso restrito) com botão "criar issue no GitHub" (manual) | WebUI | PO consegue triar sem acessar o banco |
| 2.7 | Cota mensal por plano (seção 8 da estratégia) + contador visível ao usuário | `Escritorio` / nova tabela de uso | Ultrapassou → mensagem amigável, sem chamar IA |
| 2.8 | Base de conhecimento P2 completa; eval ampliado para 200 perguntas multi-turno | `kb/`, eval | Eval verde |

**Gate 2 → 3:** ≥ 70% de conversas resolvidas sem humano · ≥ 85% 👍 · ≥ 90% dos chamados com contexto completo · custo ≤ 5% da receita dos pilotos.

---

## Fase 3 — Customer Success proativo e inteligência de produto (4 semanas)

| # | Tarefa | Onde | Critério de aceite |
|---|--------|------|--------------------|
| 3.1 | `SaudeContaWorker` completo: gatilhos da seção 5 do `CONTEXTO_AGENTE.md`, entidade `AlertaCs` (com deduplicação e "dispensar") | `API/Workers`, Domain | Cada gatilho com teste unitário; máx. 1 alerta proativo por sessão |
| 3.2 | **Score de saúde** por escritório (0–100): uso, rejeição, onboarding, certificado, assinatura | `Application/Queries` | Lista ordenada por risco no painel interno |
| 3.3 | Ferramenta `registrar_sinal` + entidade `SinalProduto` | Domain, Application | Sinais visíveis no painel |
| 3.4 | **Relatório semanal de insights** ao PO: `InsightsWorker` agrupa sinais e conversas da semana (anonimizadas) e gera resumo via Message Batches (dores, pedidos, fricções por tela, lacunas de conhecimento, bugs recorrentes) | `API/Workers` | E-mail semanal ao PO; nenhum CPF/CNPJ de destinatário no texto |
| 3.5 | `sugerir_personalizacao` ligado ao `PersonalizacaoService` (aplicar só com clique) | WebUI + ferramenta | Sugestão aceita altera `ConfiguracaoEmpresa` pelo command existente |
| 3.6 | Relatório mensal "Saúde fiscal" por empresa (Enterprise): rejeições, causas, evolução | WebUI | Ligar à flag já existente `MostrarRelatoriosAvancados` |

**Gate 3 → 4:** conversão trial→pago e taxa de rejeição com melhora mensurável contra a linha de base.

---

## Fase 4 — Evolução contínua (2027)

- Busca semântica (pgvector no Neon) quando a base passar de ~150k tokens ou quando o custo de cache justificar.
- Pré-validação antes da transmissão: assistente revisa a nota antes de "Emitir" e aponta riscos de rejeição (maior alavanca de North Star; exige eval próprio).
- Assistente no onboarding (conduz a `ConfiguracaoInicial.razor` por conversa).
- Base por UF (RICMS) conforme a distribuição de clientes.
- Conteúdo público a partir dos dados agregados (ex.: "as 10 rejeições mais comuns em SP este mês") como marketing.

---

## Rotinas permanentes (a partir da Fase 1)

| Rotina | Frequência | Dono |
|--------|------------|------|
| Checar Portal NF-e (NTs, MOC, schemas) | Quinzenal | Curador |
| Revisar artigos vencidos (`revisar_ate`) | Semanal | Curador |
| Triar lacunas, sinais e chamados | Semanal | PO |
| Revisar amostra de 20 conversas (qualidade e segurança) | Semanal | PO |
| Rodar eval completo | A cada mudança de prompt/base/modelo + mensal | Dev |
| Revisar custo por escritório e cotas | Mensal | PO |

---

## Próximos passos imediatos (esta semana)

1. PO decide as pendências 2, 3 e 5 da estratégia (nome, curador, jurídico).
2. Dev abre branch `feat/telemetria-produto` (tarefa 0.1) e `feat/alerta-certificado` (0.3) — independentes da IA.
3. Extrair do banco de produção o top 30 de `MotivoRejeicao` (0.2) para priorizar a base.
4. Curador começa pelos artigos `sistema/*` P1 usando `kb/_TEMPLATE.md`.
