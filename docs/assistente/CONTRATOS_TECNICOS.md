# Contratos técnicos da Ori (implementação)

> Fonte única de verdade para os pacotes de implementação em paralelo. Código de contrato:
> `src/Application/Assistente/Contratos.cs`, `src/Application/DTOs/Assistente/AssistenteDtos.cs`,
> `src/WebUI/Services/Assistente/Contratos.cs`, `src/Domain/Entities/Assistente/AssistenteEntities.cs`,
> `src/Domain/Interfaces/Assistente/IAssistenteRepositories.cs`.

## 1. Pacotes e donos

| Pacote | Escopo | Pastas/arquivos próprios |
|--------|--------|--------------------------|
| P1 | Motor da base de conhecimento (leitura dos .md, roteamento, mapa, endpoints) | `src/Infrastructure/Assistente/BaseConhecimento*.cs`, `src/API/Controllers/Assistente/AssistenteBaseController.cs`, testes `*/Assistente/BaseConhecimento*` |
| P2a | Artigos `sistema/*` | `docs/assistente/kb/sistema/` |
| P2b | Artigos fiscais + `_INDICE.md` + dataset do eval | `docs/assistente/kb/` (exceto `sistema/`), `docs/assistente/eval/` |
| P3 | IA, sanitizador, verificador, prompt, cotas, explicar rejeição, pergunta, conversa com ferramentas, expurgo, eval runner | `src/Infrastructure/Assistente/AssistenteIA*.cs`, `src/Application/Assistente/Servicos/`, `src/Application/Assistente/Ia/`, `src/API/Controllers/Assistente/AssistenteIaController.cs`, `src/API/Workers/Assistente/ExpurgoConversasWorker.cs`, `docs/assistente/prompt/`, testes `*/Assistente/Ia*` |
| P4a | Ori visual (coruja, poleiro, host, balões), captura de contexto, preferências, status | `src/WebUI/Components/Assistente/{Coruja,OriPoleiro,OriHost,OriBalao}*.razor`, `src/WebUI/Services/Assistente/{ContextoAssistente,OriControle,ErrosApiHandler,PreferenciasOriApi}.cs`, `src/WebUI/wwwroot/{css/ori.css,js/ori.js}`, `src/WebUI/Services/PersonalizacaoService.cs`, `src/Application/Assistente/Preferencias/`, `src/API/Controllers/Assistente/AssistentePreferenciasController.cs` |
| P4b | Painel da Ori (hipóteses, respostas, conversa SSE, avaliação, confirmação de ações) | `src/WebUI/Components/Assistente/OriPainel*.razor` (+ subcomponentes `OriPainel*`), `src/WebUI/Services/Assistente/OriApi.cs` |
| P5 | Integração nas páginas + automações A1–A6, A9, A10 | `src/WebUI/Pages/{EmitirNFe,NotaDetalhe,Produtos,Clientes,Empresa,Certificado,NotasEmitidas,Index}.razor`, `src/WebUI/Services/Assistente/AutomacoesApi.cs`, `src/Application/Assistente/Automacoes/`, `src/API/Controllers/Assistente/AssistenteAutomacoesController.cs` |
| P6 | Telemetria, CS proativo, alertas, lembretes (A7/A8/A12), saúde das contas, SQL de linha de base | `src/Application/Assistente/Cs/`, `src/API/Workers/Assistente/SaudeContaWorker.cs`, `src/API/Controllers/Assistente/{Eventos,AlertasCs,Lembretes,InternoSaude}Controller.cs`, `src/WebUI/Components/Assistente/AlertasCsBanner.razor`, `src/WebUI/Pages/Lembretes.razor`, `src/WebUI/Pages/Interno/SaudeContas.razor`, `src/WebUI/Services/Assistente/{CsApi,TelemetriaService}.cs`, `docs/assistente/sql/` |
| P7 | Monitor de fontes, curadoria, chamados, sinais, insights | `src/Application/Assistente/{Curadoria,Chamados,Sinais}/`, `src/API/Workers/Assistente/{MonitorFontesWorker,InsightsWorker}.cs`, `src/API/Controllers/Assistente/{Curadoria,Chamados,Sinais}Controller.cs`, `src/WebUI/Pages/Curadoria.razor`, `src/WebUI/Pages/Interno/Chamados.razor`, `src/WebUI/Services/Assistente/CuradoriaApi.cs` |

Testes: cada pacote cria arquivos novos em `tests/NfeSaas.Tests.Unit/Assistente/<Pacote>/` e `tests/NfeSaas.Tests.Integration/Assistente/<Pacote>/`.

## 2. Rotas da API

Todas `[Authorize]`, herdando `BaseApiController`; `EmpresaId`/`EscritorioId`/`UserId` sempre dos claims.

| Método e rota | Dono | Retorno |
|---------------|------|---------|
| `GET /api/assistente/base/mapa` | P1 | `MapaKbDto` (só artigos utilizáveis) |
| `GET /api/assistente/base/artigos/{**id}` | P1 | `ArtigoDetalheDto` |
| `GET /api/assistente/status` | P4a | `StatusAssistenteDto` |
| `GET /api/assistente/preferencias` · `PUT` | P4a | `PreferenciasOriDto` |
| `POST /api/assistente/sugestoes/ignorar` | P4a | 204 (`SugestaoIgnoradaDto`) |
| `POST /api/assistente/explicar-rejeicao/{notaId}` | P3 | `RespostaOriDto` (body `ExplicarRejeicaoDto`) |
| `POST /api/assistente/perguntar` | P3 | `RespostaOriDto` (body `PerguntaOriDto`) |
| `GET /api/assistente/conversas` | P3 | `List<ConversaResumoDto>` |
| `POST /api/assistente/conversas` | P3 | `ConversaResumoDto` |
| `GET /api/assistente/conversas/{id}/mensagens` | P3 | `List<MensagemConversaDto>` |
| `POST /api/assistente/conversas/{id}/mensagens` | P3 | **SSE** `text/event-stream`: cada evento `data: {EventoConversaDto em JSON}\n\n`; termina após `resposta` ou `erro` |
| `POST /api/assistente/interacoes/{id}/avaliacao` | P3 | 204 (body `AvaliacaoDto`) |
| `GET /api/assistente/automacoes/preparar-nota/{notaId}` | P5 | DTO de nota preparada (P5 define) |
| `GET /api/assistente/automacoes/padroes?destinatario=&produto=` | P5 | `PadroesPreenchimentoDto` |
| `GET /api/assistente/automacoes/cadastro-sugerido/{notaId}` | P5 | destinatário/itens digitados à mão (P5 define) |
| `GET /api/assistente/automacoes/destinatario-existente?doc=` | P5 | cliente existente ou 404 |
| `POST /api/eventos` | P6 | 202 (body `LoteEventosDto`) |
| `GET /api/assistente/alertas` · `POST .../{id}/visto` · `POST .../{id}/dispensar` | P6 | `List<AlertaCsDto>` |
| `GET/POST /api/assistente/lembretes` · `PUT/DELETE .../{id}` | P6 | `LembreteDto` |
| `GET /api/interno/saude` | P6 (role Plataforma) | P6 define |
| `POST /api/assistente/chamados` | P7 | `{ id }` (body `CriarChamadoDto`) |
| `POST /api/assistente/sinais` | P7 | `{ id }` (body `CriarSinalDto`) |
| `GET /api/interno/chamados` · `PUT .../{id}` | P7 (role Plataforma) | P7 define |
| `GET /api/curadoria/publicacoes` · `PUT .../{id}` · `GET /api/curadoria/artigos-vencidos` · `GET /api/curadoria/lacunas` · `GET /api/curadoria/fontes` | P7 (role Curador) | P7 define — **sem dados de clientes** |

## 3. IDs canônicos

**Campos (`data-ajuda`, `campos_relacionados`, `FocoDto.Campo`):**
`cfop`, `cst`, `csosn`, `ncm`, `cest`, `natureza_operacao`, `cst_ibs_cbs`, `cclasstrib`, `origem_mercadoria`,
`unidade`, `quantidade`, `valor_unitario`, `gtin`, `modalidade_frete`, `forma_pagamento`, `informacoes_adicionais`,
`serie`, `numero`, `regime_tributario`, `ambiente`, `cep`, `codigo_municipio`, `uf`, `ind_ie`,
`certificado`, `csc`, `cpf_cnpj`, `ie`, `razao_social`, `logradouro`, `email`, `telefone`, `senha_certificado`, `csc_token`.

**Campos sensíveis — nunca enviar o valor** (só o id do campo e, quando houver, o resultado de validação):
`cpf_cnpj`, `ie`, `razao_social`, `logradouro`, `email`, `telefone`, `senha_certificado`, `csc_token`, `csc`, `certificado`.

**Telas (`ContextoTelaDto.Tela`, `telas`):** `dashboard`, `emitir-nfe`, `notas`, `nota-detalhe`, `produtos`, `clientes`,
`empresa`, `empresas`, `configuracao-inicial`, `certificado`, `inutilizacoes`, `usuarios`, `escritorio-como-empresa`,
`lembretes`, `login`.

**Hipóteses (`PedidoOri.Hipotese`):** `rejeicao` (NotaId), `campo` (usa o foco), `erro_sistema`, `emissao_falhando`,
`certificado`, `livre`, `rota` (usada por dicas de alerta: "Mostrar" navega para `TextoInicial`).

**Eventos para `IOriControle.Notificar`:** `nota_autorizada`, `nota_autorizada_primeira`, `nota_rejeitada`, `erro_api`,
`marco_notas`.

**Chaves de dica/sugestão (`SugestaoDispensada.Chave`):** `dica:{id}`, `A1:{notaId}`, `A2:{notaId}`,
`A5:{clienteDoc-hash}`, `A6:{cliente}:{produto}:{cfop}:{cst}`, `A7:{notaModeloId}`.

**Nível `sistema`:** artigos `kb/sistema/*` usam `nivel_fonte: sistema` (e `nivel: sistema` nas fontes) → `NivelFonte.GuiaDoSistema`,
selo "Guia do NFeFlow". O selo de uma resposta é o do nível mais fraco entre os artigos FISCAIS citados; se só houver
artigos `sistema`, o selo é `GuiaDoSistema`.

**Preenchimento por URL (P5 implementa, P4b/P6 geram links):**
- `/emitir?origem={notaId}` — prepara a partir de uma nota (A5/A7). Opcional `&ajustes=item2.cfop=6102;item1.ncm=69120000`
  (itens 1-based; campos com os IDs canônicos).
- `/clientes?origemNota={notaId}` — formulário de destinatário preenchido com o da nota (A1).
- `/produtos?origemNota={notaId}&itens=1,3` — formulários de produto preenchidos com os itens da nota (A2).
- `/produtos?editar={produtoId}&origemNota={notaId}` — atualização de cadastro sugerida (A4).
- `/lembretes?notaModelo={notaId}` — criar lembrete de nota recorrente (A7).

## 4. Dataset do eval (`docs/assistente/eval/perguntas.jsonl`)

Uma pergunta por linha:

```json
{"id":"fis-001","categoria":"fiscal","pergunta":"...","contexto":{"tela":"emitir-nfe","foco":"cfop","uf_emitente":"SP","uf_destino":"MG","regime":"SimplesNacional"},
 "resposta_referencia":"...","artigos_esperados":["preenchimento/cfop-mais-usados"],"nivel_esperado":"N1",
 "deve_recusar":false,"deve_citar":true,"termos_obrigatorios":["6102"],"termos_proibidos":["com certeza"]}
```

Categorias: `fiscal`, `sistema`, `rejeicao`, `armadilha` (planejamento tributário, dado de outra empresa, fora da base
→ `deve_recusar: true`), `linguagem` (N2/N3/N4), `vazamento` (pergunta com CPF/e-mail/nome → nada disso pode chegar
ao modelo).

## 5. Configuração (`Assistente__*`)

Ver `AssistenteOptions` em `Contratos.cs`. Chaves principais: `Assistente__Habilitado`, `Assistente__EscritoriosPiloto__0`,
`Assistente__IncluirRascunhosKb`, `Assistente__Ia__Conversa__Endpoint|ApiKey|Modelo`,
`Assistente__Ia__FontesPublicas__Endpoint|ApiKey|Modelo`, `Assistente__EmailPo`, `Assistente__EmailCurador`,
`Assistente__Inlabs__Email|Senha`.

## 6. Regras comuns

- Workers retornam imediatamente em `IHostEnvironment.IsEnvironment("Testing")`.
- Nada que vai ao modelo contém dado pessoal (`ISanitizadorIA` + atributos). Nada é concluído pela Ori (só prepara).
- Nenhum pacote altera entidades, enums, migrations, `NfeDbContext`, arquivos de contrato, `Registro.cs`,
  `DependencyInjection*.cs`, `Program.cs` ou `MainLayout.razor` — pedidos de mudança vão no relatório final.
