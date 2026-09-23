# Assistente NFeFlow (a Coruja) — Plano de Ação

> **v2 — 2026-09-23**, após a fase de pesquisa ([`PESQUISA_REFINAMENTO.md`](PESQUISA_REFINAMENTO.md)).
> Premissa de capacidade: 1 dev full-stack + PO + **escritório parceiro** (curadoria e termos, ~4h/semana) + ferramentas de IA para o mascote (MCP) com acabamento pontual.
> Cada fase termina num **gate go/no-go**. Datas a partir de 2026-09-28.
>
> **Mudança principal da v2:** a coruja, a captura de contexto e as automações de maior valor **não dependem de IA**.
> Elas entram antes do modelo, custam zero por uso e validam a aceitação do mascote antes de gastar com LLM.

```
Fase 0  Fundação                                   ██████                                  sem 1–3   (28/09 – 16/10)
Fase 1  Coruja sem IA + automações onda 1                ██████                            sem 4–6   (19/10 – 06/11)
Fase 2  IA: explicar rejeição + "explicar melhor"              ██████                      sem 7–9   (09/11 – 27/11)
Fase 3  Conversa com ferramentas + automações onda 2                 ████████              sem 10–13 (30/11 – 08/01, recesso)
Fase 4  CS proativo, recorrência, monitor com IA, insights                   ████████      sem 14–17 (11/01 – 05/02)
Fase 5  Evolução contínua                                                              2027
```

---

## Fase 0 — Fundação (3 semanas)

| # | Tarefa | Onde | Critério de aceite |
|---|--------|------|--------------------|
| 0.1 | **Telemetria de produto**: `EventoProduto` + `RegistrarEventoCommand` + `POST /api/eventos` (lote) | Domain, Application, API, migration | Login, troca de empresa, tela aberta, emissão iniciada/autorizada/rejeitada; sem valores de campos |
| 0.2 | **Linha de base**: taxa de rejeição, tempo até a 1ª nota autorizada, top 30 `MotivoRejeicao`, conversão do trial, % de notas com destinatário ou item digitado à mão | `docs/assistente/sql/` | KPIs de `ESTRATEGIA.md` §7 preenchidos |
| 0.3 | **Alerta de certificado vencendo** (e-mail 30/15/7/1 dias + banner) | `SaudeContaWorker` | Teste de integração |
| 0.4 | **Base P1** (20+ artigos) no template novo (`nivel_fonte`, `campos_relacionados`, `resumo_curto`) | `docs/assistente/kb/` | Status `revisado` |
| 0.5 | Script de validação da base + geração do `kb/_mapa.json` | `scripts/kb-validar.*`, CI | Falha se artigo publicado estiver sem fonte ou sem nível |
| 0.6 | **Eval fiscal v1**: 100 perguntas + 10 armadilhas + 10 casos de linguagem (N2/N3/N4) | `tests/NfeSaas.Tests.Assistente/eval/` | Revisado pelo curador |
| 0.7 | **Termos de uso** revisados pelo escritório parceiro (assistente, operador de IA, Foundry e país de processamento, minimização, retenção de 180 dias, captura de contexto só ao chamar a Ori); cláusulas de LGPD e responsabilidade vistas por advogado | fora do código | Aprovado; aceite no próximo login |
| 0.7b | **Acordo com o escritório parceiro** (confidencialidade, assinatura técnica com CRC, SLA da fila, contrapartida) + papel `Curador` isolado | fora do código + Auth | Assinado; curador não vê dados de clientes |
| 0.8 | **Foundry** (decidido): criar o recurso DeepSeek V4, escolher a região (Brazil South, senão EUA/UE), medir preço e latência | Azure | Endpoint e região registrados em `PESQUISA_REFINAMENTO.md` |
| 0.9 | **Ori — protótipo** SVG + CSS (9 estados) publicado como Artifact para avaliação | `docs/assistente/mascote/` | PO aprova postura, posição e animações |
| 0.9b | **Ori — arte final** via Recraft MCP (ou svgapp) + acabamento em camadas; checagem de licença comercial; pedido de registro de marca no INPI | `MASCOTE_UX.md` §7 | SVG em camadas < 30 KB, licença verificada |
| 0.10 | Benchmark de 5 concorrentes | `docs/assistente/benchmark.md` | Tabela comparativa |

**Gate 0 → 1:** linha de base medida · 20 artigos · eval pronto · termo aprovado · acordo com o parceiro assinado · Foundry configurado · arte final da Ori entregue.

---

## Fase 1 — Coruja sem IA + automações onda 1 (3 semanas)

**Objetivo:** colocar a coruja na tela, útil desde o primeiro dia e sem custo por uso. Medir se o mascote agrada.

| # | Tarefa | Onde | Critério de aceite |
|---|--------|------|--------------------|
| 1.1 | Componente `Coruja` (SVG + CSS, 9 estados, `prefers-reduced-motion`, pausa em aba oculta) no rodapé do `MudDrawer`; botão flutuante em ≤ 600 px; atalho `Ctrl+Shift+Espaço` | `WebUI/Components/Assistente/`, `MainLayout.razor` | Não sobrepõe formulários; < 30 KB; acessível por teclado |
| 1.2 | Painel lateral (`MudDrawer Anchor.End`) com "Estou vendo que…" editável e até 3 hipóteses | `WebUI/Components/Assistente/` | Funciona em 400 px |
| 1.3 | **Captura de contexto**: `RastroSessao`, listener de foco (`data-ajuda`), `DelegatingHandler` de erros da API, `IContextoTela` em `EmitirNFe`, `NotaDetalhe`, `Produtos`, `Clientes`, `Empresa`, `Certificado` | WebUI | Payload ≤ 450 tokens; lista de bloqueio testada |
| 1.4 | Anotar campos fiscais com `data-ajuda` (CFOP, CST/CSOSN, NCM, CEST, natureza, IE, cClassTrib…) | Páginas acima | 100% dos campos com artigo associado |
| 1.5 | **Motor de hipóteses** (regras de `CAPTURA_CONTEXTO.md` §4) + resposta por `resumo_curto` do artigo | WebUI + `kb/_mapa.json` servido pela API | Abertura com resposta em < 200 ms, sem LLM |
| 1.6 | **Balões silenciosos** com as regras de frequência de `MASCOTE_UX.md` §5; "silenciar dicas" persistido por usuário | WebUI + preferência no servidor | Máx. 1 a cada 10 min e 3 por sessão; dispensada não volta |
| 1.7 | **A5 — "Emitir igual à última"**: `PrepararNotaAPartirDeCommand` + `EmitirNFe.razor?origem=` com campos destacados e "Desfazer" | Application, WebUI | Não persiste nada até o usuário clicar em Emitir |
| 1.8 | **A1/A2 — Guardar destinatário/produtos digitados à mão** após autorização | WebUI (`NotaDetalhe`/`EmitirNFe`) | Abre o formulário preenchido; usuário salva |
| 1.9 | **A9 — Checklist de onboarding** guiado pela coruja | WebUI | Ligado a `ConfiguracaoEmpresa.ConcluidoEm` e certificado |
| 1.10 | **Monitor de fontes — detecção** (sem IA): `FonteMonitorada`, `PublicacaoDetectada`, hash de páginas, INLABS com filtro por órgão e termos, e-mail semanal ao curador | `MonitorFontesWorker` | Detecta uma NT nova de teste; alerta de fonte cega |
| 1.11 | Flag `MostrarAssistente` no `PersonalizacaoService` + piloto com 3–5 escritórios | WebUI | Liga e desliga por escritório |

**Gate 1 → 2:** ≤ 15% silenciaram as dicas · ≥ 30% dos usuários abriram a coruja · ≥ 40% de aceite nas sugestões A1/A2/A5 · zero reclamação de privacidade.

---

## Fase 2 — IA: explicar rejeição e "explicar melhor" (3 semanas)

| # | Tarefa | Onde | Critério de aceite |
|---|--------|------|--------------------|
| 2.1 | `IAssistenteIA` + implementação `Microsoft.Extensions.AI` com cliente compatível com OpenAI; rotas `Conversa` (Foundry) e `FontesPublicas` (API DeepSeek); config `Assistente__*` | Application, Infrastructure, `render.yaml` | Sem chave ou flag → recurso oculto, sem erro; troca de endpoint só por config |
| 2.2 | Montagem do prompt: bloco fixo v0.2 + artigos roteados + contexto + janela de histórico | Application | Prefixo byte-idêntico (teste automatizado); acerto de cache medido |
| 2.2b | **`SanitizadorIA`**: atributos e resultados de validação no lugar de CPF/CNPJ/IE/nome/endereço; marcadores reidratados só na tela; filtro na pergunta livre | Application | 100% dos casos de vazamento do eval sanitizados; teste que falha se qualquer ferramenta devolver documento cru |
| 2.3 | `ExplicarRejeicaoQuery` e "Explicar melhor" a partir de uma hipótese | Application, WebUI | Nota de outra empresa → 404 |
| 2.4 | **Verificador** de números, datas e códigos + cálculo de selo N1/N2/N3 no servidor | Application | Casos do eval com número inventado são barrados |
| 2.5 | **Cotas**: `UsoAssistente` (dia/mês por usuário, teto por escritório), coruja "dormindo profundo" ao esgotar, contador visível | Domain, Application, WebUI | Checagem antes de chamar o modelo; incremento atômico |
| 2.6 | `InteracaoAssistente` (pergunta, resposta, artigos, selo, tokens com/sem cache, custo, 👍/👎) | Domain + migration | Painel de custo |
| 2.7 | **Eval** `deepseek-flash` × V4-Pro (precisão, linguagem por nível, custo, latência) | `tests/NfeSaas.Tests.Assistente` | Relatório → decisão do PO |
| 2.8 | Monitor de fontes — **resumo com IA** (rota FontesPublicas, lote às 6h de Brasília): relevância, vigência, artigos afetados → `em_revisao` | `MonitorFontesWorker` | Resumo revisado pelo curador em 3 publicações reais |

**Gate 2 → 3:** eval ≥ 95% e 0 respostas perigosas · linguagem correta por nível em ≥ 95% · ≥ 80% 👍 · custo médio ≤ US$ 0,01 por explicação · nenhum incidente de isolamento.

---

## Fase 3 — Conversa com ferramentas + automações onda 2 (4 semanas)

**Pré-requisito:** API fora do free tier do Render (a espera do servidor "acordando" inviabiliza o chat).

| # | Tarefa | Critério de aceite |
|---|--------|--------------------|
| 3.1 | `Conversa`/`MensagemConversa` (retenção de 180 dias + expurgo) | Expurgo testado |
| 3.2 | `EnviarMensagemAssistenteCommand` com loop de ferramentas + streaming SSE | Primeiro token < 3 s com o servidor quente |
| 3.3 | Ferramentas de leitura (`CONTEXTO_AGENTE.md` §5.1), cada uma com teste de isolamento | 100% com teste "outra empresa → vazio" |
| 3.4 | Ferramentas de preparação (`preparar_nota`, `preparar_cadastro`, `sugerir_personalizacao`) | Nada é concluído sem clique do usuário (teste de UI) |
| 3.5 | `Chamado` + `abrir_chamado` + tela de triagem interna | Chamado com rota, `LastEventId` do Sentry e rastro mascarado |
| 3.6 | **A6 — padrões de preenchimento** (`PadraoPreenchimento`, sugestão inline) | "Baseado nas últimas N notas" visível; recusa 3× → some |
| 3.7 | **A3, A4, A10** (destinatário já existe, atualizar produto após rejeição corrigida, sugestão de CEST) | Aceite ≥ 30% |
| 3.8 | Base P2 completa + eval de 200 perguntas em várias trocas de mensagem | Eval verde |

**Gate 3 → 4:** ≥ 70% das conversas resolvidas sem humano · ≥ 85% 👍 · ≥ 90% dos chamados completos.

---

## Fase 4 — CS proativo, recorrência e inteligência de produto (4 semanas)

| # | Tarefa | Critério de aceite |
|---|--------|--------------------|
| 4.1 | `SaudeContaWorker` completo (gatilhos de `CONTEXTO_AGENTE.md` §6) + `AlertaCs` | Deduplicação; máx. 1 balão proativo por sessão |
| 4.2 | **A7/A8 — Lembretes de nota recorrente**: detecção de periodicidade, `LembreteEmissao`, worker que gera **rascunho** (`SituacaoNota.Rascunho`) no dia e avisa | O worker nunca transmite; campos variáveis destacados |
| 4.3 | **A12** — rascunhos parados no fim do mês | — |
| 4.4 | Score de saúde por escritório + painel interno | Lista por risco |
| 4.5 | `registrar_sinal` + `SinalProduto` + **relatório semanal de insights** ao PO (lote, dados anonimizados) | Sem CPF ou nome de pessoa física no texto |
| 4.6 | Relatório mensal "Saúde fiscal" (Enterprise, flag `MostrarRelatoriosAvancados`) | — |

**Gate 4 → 5:** melhora mensurável na taxa de rejeição e na conversão do trial em relação à linha de base.

---

## Fase 5 — Evolução contínua (2027)

- **Pré-validação antes de emitir**: a coruja revisa a nota e aponta riscos de rejeição (maior alavanca da North Star).
- Animações ricas (Rive/Lottie) se a métrica de engajamento justificar.
- Busca vetorial (pgvector) quando o roteamento por mapa não bastar.
- Base por UF (RICMS) conforme a distribuição de clientes; referências N3 avaliadas e admitidas.
- Conteúdo público a partir de dados agregados (ex.: rejeições mais comuns por UF no mês).

---

## Rotinas permanentes

| Rotina | Frequência | Dono |
|--------|------------|------|
| Fila de curadoria (publicações detectadas pelo monitor) | 2× por semana | Curador |
| Artigos vencidos (`revisar_ate`) | Semanal | Curador |
| Lacunas, sinais e chamados | Semanal | PO |
| Amostra de 20 conversas (qualidade, linguagem por nível, segurança) | Semanal | PO |
| Eval completo | Todo PR em `kb/` ou no prompt + mensal | Dev |
| Custo, cotas e taxa de "silenciar dicas" | Mensal | PO |
| Revalidar preços e disponibilidade da DeepSeek e do Foundry | Trimestral | Dev |

---

## Próximos passos imediatos

1. PO: formalizar o acordo com o escritório parceiro e enviar a minuta dos termos para a revisão dele (com apoio jurídico nas cláusulas de LGPD).
2. Dev: `feat/telemetria-produto` (0.1) e `feat/alerta-certificado` (0.3).
3. Extrair do banco de produção o top 30 de `MotivoRejeicao` e o % de destinatários e itens digitados à mão (0.2),
   que dimensiona o ganho de A1/A2.
4. Protótipo da Ori (SVG + CSS) para avaliação; em seguida, conta no Recraft (ou svgapp) para a arte final.
