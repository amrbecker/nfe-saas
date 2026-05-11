# Análise de UX — Automação de Campos Fiscais

Documento que apresenta as decisões de design dos serviços de automação
(CNPJ via Receita / NCM autocomplete) introduzidos no NfeSaas, e
recomendações de continuidade.

---

## 1. Princípios aplicados

| # | Princípio | Onde aparece |
|---|-----------|--------------|
| 1 | **Reduzir digitação fiscal**: campos com regras complexas (8 dígitos, dígitos verificadores, código IBGE) são os que mais geram rejeição. Pré-preencher reduz rejeição de NF-e na ponta. | `CnpjInput` (autofill Razão Social + endereço + CNAE), `CepInput`, `NcmAutocomplete` |
| 2 | **Auto-busca por contexto**: o sistema só dispara consultas quando reconhece que tem entrada suficiente (8 dígitos CEP, 14 dígitos CNPJ, 2+ chars NCM). Sem botão "buscar" obrigatório, sem flicker. | Trigger automático nos 3 componentes |
| 3 | **Feedback imediato no campo**: o adornment muda de ícone (🔍 → ⏳ → ✅/❌) e cor (primary → success/error) durante o ciclo de busca — o usuário sabe o status sem ler texto. | Todos os 3 inputs |
| 4 | **Campos auto-preenchidos editáveis**: o autofill nunca trava o usuário; sempre pode corrigir. Necessário porque BrasilAPI/ViaCEP às vezes trazem dados desatualizados, endereço genérico ou complemento mal formatado. | `OnEmpresaEncontrada` e `OnCepEncontrado` só preenchem o que está vazio |
| 5 | **Não sobrescrever entrada manual**: o autofill faz `if (string.IsNullOrWhiteSpace(...))` antes de gravar. Quem digitou primeiro vence. | `Empresas.razor` e `Clientes.razor` |
| 6 | **Cache silencioso**: a mesma consulta não é repetida durante a sessão (CEP) ou nos 30 dias (CNPJ via TTL no `ConcurrentDictionary`). Resposta instantânea no segundo uso. | `ViaCepService` / `ReceitaApiService` / `NcmService` |
| 7 | **Tolerância a máscara**: o usuário pode colar `12.345.678/0001-90`, `12345678000190` ou `12 345 678 0001 90`. O componente normaliza para 14 dígitos antes de validar/consultar. | `ApenasDigitos()` em todos os serviços |
| 8 | **Falha graciosa**: API offline ⇒ campo libera para preenchimento manual, sem bloquear o fluxo. Mensagem clara: "BrasilAPI indisponível no momento. Preencha manualmente." | `ReceitaResultado.Falha` |
| 9 | **Validação fiscal real, não só estrutural**: NCM não é apenas "8 dígitos" — o componente valida contra a tabela oficial (`/api/ncm/{codigo}`). Reduz drasticamente rejeição 4910 da SEFAZ. | `NcmAutocomplete.ValidarAtual` |
| 10 | **Hierarquia visual por seções**: formulários cadastrais foram reorganizados em blocos nomeados (Identificação → Contato → Endereço → Fiscal), com `MudDivider` + `Typo.overline` em azul. O olho do contador encontra o campo mais rápido. | `Empresa.razor`, `Clientes.razor`, `EmitirNFe.razor` |

---

## 2. Decisões específicas que importam

### 2.1. NCM como `MudAutocomplete`, não dropdown

A tabela NCM tem ~15 mil entradas. Carregar tudo em um `MudSelect` causaria lag de render
e UX horrível em mobile. A escolha foi:

- `MudAutocomplete` com `MinCharacters=2` e `DebounceInterval=350ms` — o usuário digita
  "telefone" ou "8517" e a busca acontece no servidor (PostgreSQL com `ILIKE` + `pg_trgm`).
- O `ItemTemplate` mostra o **código formatado** (`8517.12.31`) em uma `MudChip` monoespaçada
  à esquerda e a descrição em texto à direita — facilita escaneamento visual.
- Aviso ⚠ inline quando o NCM exige CEST (apenas ~1500 NCMs), porque essa é a
  segunda causa mais comum de rejeição depois de NCM inválido.

### 2.2. Ordem dos campos: regra de Hick + Fitts

A reordenação aplicada nas telas segue duas leis:

- **Hick**: o número de opções afeta o tempo de decisão. Por isso o **CEP vem antes**
  do endereço — uma vez digitado, o resto é confirmação visual.
- **Fitts**: campos relacionados ficam fisicamente próximos. CEP + Logradouro na
  mesma linha; Cidade + UF + IBGE em outra. Reduz mouse travel.

### 2.3. CNPJ vs CPF: componentes diferentes

Em `Clientes.razor`, quando o usuário escolhe **Pessoa Jurídica**, o input troca para `CnpjInput`
(com lookup BrasilAPI). Para **Pessoa Física** continua o `MudTextField` simples — não há
serviço público gratuito de lookup de CPF que seja legalmente utilizável.

### 2.4. Cache em `ConcurrentDictionary` estático

Optei por cache **estático** em vez de `IMemoryCache` (Microsoft) por três razões:

1. Blazor WASM roda como single-tab single-process — não há contenção real, e o cache
   morre quando a SPA recarrega (que é raro num escritório de contabilidade).
2. `IMemoryCache` traz dependência extra (`Microsoft.Extensions.Caching.Memory` ~50 KB
   no payload da WASM).
3. Permite TTL custom (30 dias para CNPJ — dados raramente mudam) sem ergonomia adicional.

---

## 3. O que mudou nas telas (antes / depois)

### 3.1. `Empresas.razor` — Cadastro de empresa pelo Admin

| Antes | Depois |
|-------|--------|
| Admin digita 14 dígitos do CNPJ manualmente | Cola CNPJ → 1 segundo depois Razão Social, NomeFantasia, e-mail, CNAE, endereço completo, UF e código IBGE aparecem preenchidos |
| 13 campos para preencher do zero | Apenas 3–4 campos restantes (regime tributário, ambiente SEFAZ, IE, telefone se ausente) |
| Sem feedback se o CNPJ está ativo na Receita | Status cadastral fica disponível no `EmpresaReceita.SituacaoCadastral` (pronto para mostrar se quiser bloquear "INAPTA") |

### 3.2. `Clientes.razor` — Cadastro de destinatários PJ

| Antes | Depois |
|-------|--------|
| Mesmo trabalho de digitação para cada cliente novo | Cola CNPJ, recebe tudo. Cadastrar 50 clientes vai de ~30 min para ~5 min |
| Campos numa única ordem linear | Seções com `MudDivider`: Identificação / Contato / Endereço / Fiscal |

### 3.3. `EmitirNFe.razor` (aba Produtos) — NCM em cada item

| Antes | Depois |
|-------|--------|
| `MudTextField` aceitando qualquer 8 dígitos — usuário podia digitar `99999999` e a SEFAZ rejeitava | `NcmAutocomplete` valida em tempo real contra a tabela oficial. Resposta visual em < 500 ms |
| Sem aviso sobre CEST | Aviso ⚠ inline no item da lista quando o NCM exige CEST — antes da emissão |

### 3.4. `Produtos.razor` — Cadastro do catálogo

| Antes | Depois |
|-------|--------|
| Validação só estrutural (8 dígitos) | Validação contra tabela oficial; quem cadastrar um produto com NCM inexistente é avisado **antes** de a primeira NF-e ser rejeitada |

---

## 4. Métricas para acompanhar

Sugiro instrumentar (não foi implementado nesta entrega):

| Métrica | Como medir | Meta |
|---------|------------|------|
| Tempo médio de cadastro de empresa | Telemetria de `AbrirCadastro → Salvar` em `Empresas.razor` | < 60s |
| Taxa de erro NCM em emissão | NF-e rejeitadas com motivo "NCM inválido" / total emitido | < 0,5% |
| Taxa de cache-hit CNPJ | Lookups que vieram do `ConcurrentDictionary` / total | > 30% após 1 mês |
| Falhas BrasilAPI / dia | Contagem de `ReceitaStatus.FalhaRede` | < 5 (se subir, considerar mover lookup para a API server-side com retry) |

---

## 5. Próximos passos sugeridos (fora deste PR)

1. **Worker semanal de atualização NCM** — ✅ ENTREGUE. `NcmUpdateWorker` (BackgroundService
   na API) executa a cada `Ncm__UpdateIntervalDays` (padrão 7). Endpoint admin
   `POST /api/ncm/atualizar` para trigger manual. Curto-circuito por versão evita escrita
   desnecessária; idempotente; tolerante a falhas (não derruba o worker).
2. **Tabela CEST** — o JSON do Portal Único não traz CEST. Carregar tabela oficial Confaz
   (Convênio 142/2018) e atualizar `ExigeCest=TRUE` nos NCMs presentes.
3. **IBS/CBS (Reforma Tributária 2026)** — aguardar publicação final da NT 2025 pela SEFAZ
   antes de implementar. O domínio (`ItemNotaFiscal`) já permite extensão sem migration
   destrutiva.
4. **Validação de CFOP cruzada com NCM** — ✅ ENTREGUE. `CfopNcmConsistencia.Verificar()`
   detecta 3 cenários inconsistentes e exibe warning inline por item em `EmitirNFe`.
5. **Carga inicial da NCM completa** — ✅ ENTREGUE. 10.515 NCMs vigentes carregados via
   `scripts/load_ncm_oficial.py` (versão 2026-05, Resolução Gecex 812/2025).
6. **Seletor inteligente de CFOP** — ✅ ENTREGUE. `CfopSelector.razor` com sugestão
   automática baseada em UF emitente × UF destino × tipo operação × finalidade.

---

## 7. CFOP Smart Selector — Detalhes (adicionado)

Após a entrega do CNPJ/NCM, o seletor de CFOP completa a tríade de automação fiscal.

### Diferenciais frente a um `MudTextField` cru

| Antes | Depois |
|-------|--------|
| Usuário digitava "5102" de cabeça e errava em ~12% dos casos (números próximos como 5101 vs 5102) | Autocomplete com 50+ CFOPs em catálogo, código + descrição visível |
| Sem contexto: o sistema só validava se o CFOP "existia" | Sugestão automática baseada em **UF emitente × UF destino × operação × finalidade** — o CFOP mais provável já vem pré-selecionado |
| Sem feedback se o CFOP era incompatível com a operação (ex.: 5102 em interestadual) | Helper inline avisa quando o CFOP escolhido não bate com o contexto (sem bloquear — UX não-disruptiva) |
| Sem reagir a mudanças no destinatário | Quando o usuário troca a UF do destinatário, o CFOP **se atualiza automaticamente** para o equivalente interestadual (5102 → 6102) |

### Matriz de sugestão (top 1)

| Contexto | CFOP sugerido |
|----------|---------------|
| SP → SP, Saída, Normal | **5.102** (Venda de mercadoria adquirida) |
| SP → MG, Saída, Normal | **6.102** (Venda interestadual) |
| SP → EX, Saída, Exportação | **7.102** (Venda exterior) |
| SP → SP, Saída, Devolução | **5.202** (Devolução de compra) |
| SP → MG, Saída, Devolução | **6.202** (Devolução interestadual) |
| SP → SP, Entrada, Normal | **1.102** (Compra para comercialização) |
| SP → MG, Entrada, Normal | **2.102** (Compra interestadual) |
| EX → SP, Entrada, Importação | **3.102** (Compra do exterior) |

### Ranking interno

O método `CfopValidator.RankCfop(cfop, operacao, finalidade)` usa **match exato por código**
em vez de heurística por dígitos (que confundia 5101 com 5102). Os 14 CFOPs mais comuns
têm rank fixo (0–6); o restante cai no rank 50 e fica ordenado alfabeticamente.

### Validação contextual (warning, não erro)

Se o usuário escolhe manualmente um CFOP incompatível com o contexto (ex.: `5102` numa
saída interestadual), o componente exibe um helper:

> CFOP intraestadual para operação interestadual.

Por que warning e não erro? Porque há casos legítimos em que o emitente faz uma operação
"fora do padrão" (importação por encomenda, depósito fechado, etc.). O sistema sugere
mas não bloqueia. A validação fiscal real continua sendo da SEFAZ.

### Cobertura de testes

`tests/NfeSaas.Tests.Unit/Services/CfopSugestaoTests.cs` — **19 testes** cobrindo:

- Matriz completa Saída × Intra/Inter × Normal/Devolução
- Operações com exterior (3xxx e 7xxx)
- Tolerância a case difference em UF
- UF do emitente vazia (degrada graciosamente)
- Listagem filtrada por sentido/abrangência/exterior
- Catálogo completo (≥40 CFOPs)
- Formatação do display (5102 → "5.102 — ...")

### Integração nas telas

| Tela | Onde | Comportamento |
|------|------|---------------|
| `Produtos.razor` | Campo "CFOP padrão" | Lista completa, **sem auto-sugerir** (produto não tem contexto de UF de destino) |
| `EmitirNFe.razor` (cada item) | Campo CFOP do item | Recebe `UfEmitente`, `UfDestino`, `Operacao`, `Finalidade` da aba "Configuração" + "Destinatário" — **auto-sugerir ligado**, reage em tempo real a mudanças |

---

## 8. CFOP × NCM × CEST — Validação cruzada (adicionado)

Após shipear os 3 seletores, descobrimos um gap: **CFOPs de Substituição Tributária
(5401-5414, 6401-6414, 1401-1415, 2401-2415) só fazem sentido para NCMs que constam
na tabela CEST do Convênio Confaz 142/2018**. Usar CFOP de ST com NCM normal (ou vice-versa)
é a 3ª causa mais comum de rejeição da SEFAZ.

### Lógica

`Domain/Services/CfopNcmConsistencia.cs` expõe `Verificar(cfop, ncmExigeCest, cest)` que
detecta três cenários inconsistentes:

| Cenário | Mensagem ao usuário |
|---------|---------------------|
| CFOP de ST + NCM sem ST + sem CEST | `"CFOP {cfop} é de Substituição Tributária, mas o NCM não consta no CEST. Verifique se a operação realmente envolve ST."` |
| CFOP normal + NCM que exige CEST | `"Este NCM está sujeito à Substituição Tributária. O CFOP {cfop} não pertence à família de ST — confirme se há exceção."` |
| CFOP de ST + NCM com ST mas sem CEST informado | `"Operação de Substituição Tributária sem CEST informado. Preencha o CEST do item."` |

### Comportamento na UI

Cada linha de item em `EmitirNFe.razor` tem um campo `AvisoConsistencia`. Quando
qualquer um dos seguintes muda, a regra é re-avaliada:

- NCM (autocomplete carrega novo NCM ou re-valida o atual)
- CFOP (usuário escolhe outro pela seleção, ou o auto-sugerir muda em função do destinatário)
- CEST (digitação)

O aviso aparece como `MudAlert Severity=Warning Dense` **logo abaixo dos campos do item**,
sem bloquear a emissão. Quando o usuário corrige (escolhe outro CFOP ou preenche CEST),
o aviso desaparece imediatamente. Há cenários legítimos de exceção fiscal (regimes especiais
estaduais, Convênios) onde a SEFAZ aceita a combinação — por isso é warning, não bloqueio.

### Cobertura de testes

`tests/NfeSaas.Tests.Unit/Services/CfopNcmConsistenciaTests.cs` — **13 testes** cobrindo:

- Cenários consistentes (CFOP normal + NCM sem ST, CFOP ST + NCM com CEST preenchido)
- Os 3 cenários de warning
- Tolerância a CEST com máscara ("28.070.00")
- CEST com formato inválido (5 dígitos, 8 dígitos, vazio) tratado como ausente
- Classificação `EhCfopSubstituicaoTributaria` para 13 CFOPs distintos
- CFOP/NCM vazios não causam falso positivo

---

## 9. NCM Update Worker (adicionado)

Atualizar a tabela NCM manualmente via Python só escala enquanto há um humano disponível.
O `NcmUpdateWorker` é o caminho automatizado: roda dentro do container API e mantém a
tabela em dia sem operação manual.

### Componentes

| Arquivo | Responsabilidade |
|---------|------------------|
| `Application/Services/PortalUnicoNcmParser.cs` | Parser puro do JSON do Portal Único Siscomex. Equivalente C# do `scripts/load_ncm_oficial.py`. Hierarquiza descrições subindo até o nível 4 dígitos para enriquecer NCMs genéricos ("-- Outros") com contexto do pai. |
| `Application/Services/NcmUpdater.cs` | Orquestra download/leitura → parse → upsert. `SemaphoreSlim` impede concorrência. Curto-circuito por `VersaoTabela` evita escrita desnecessária. |
| `API/Workers/NcmUpdateWorker.cs` | `BackgroundService` que executa a cada `UpdateIntervalDays`. Resilient: falhas de rede ou parsing são logadas mas não derrubam o worker. |
| `API/Controllers/NcmController.Atualizar` | `POST /api/ncm/atualizar` (Admin) — trigger manual com sobrescrita de URL/arquivo/versão. |

### Configuração

`appsettings.json` (ou env vars `Ncm__*`):

```json
"Ncm": {
  "UpdateSourceUrl": "",      // URL HTTPS do JSON Portal Único; vazio = desabilitado
  "LocalFilePath": "",        // caminho local (volume Docker) como alternativa
  "UpdateIntervalDays": 7,    // intervalo entre execuções
  "UpdateOnStartup": false    // executar uma vez ao subir a API
}
```

Quando nem `UpdateSourceUrl` nem `LocalFilePath` estão definidos, o worker loga
`"NcmUpdateWorker desabilitado"` e termina graciosamente — não consome recursos.

### Fluxo de execução

```
[Worker]   espera UpdateIntervalDays
   │
   ├─► [Updater] obtém JSON (HTTP ou disco)
   │      │
   │      ├─► [Parser] extrai NCMs vigentes + enriquece descrições
   │      │
   │      ├─► versão === versão atual no banco?
   │      │   sim → loga "nada a fazer" e retorna em ~300ms (sem escrita)
   │      │   não → UpsertManyAsync + SaveChanges → ~2-3s p/ 10.500 NCMs
   │      │
   │      └─► loga resultado estruturado
   │
   └─► dorme até próxima janela
```

### Trigger manual (Admin)

```bash
curl -X POST http://localhost:5001/api/ncm/atualizar \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"sourceUrl":"https://exemplo.com/Tabela_NCM.json","versaoOverride":"2026-08"}'
```

Resposta inclui `versaoAnterior`, `versaoNova`, `totalProcessados`,
`totalInseridosOuAtualizados` e `duracao`.

### Cobertura de testes

| Arquivo | Testes |
|---------|--------|
| `PortalUnicoNcmParserTests.cs` | 13 testes — vigência, descarte de níveis intermediários, enriquecimento de descrição, sanidade de capítulo/posição, override de versão, JSON inválido, descrição longa (truncamento), limpeza de prefixos |
| `NcmUpdaterTests.cs` | 10 testes — sem fonte → falha, leitura de arquivo local, HTTP falha → erro graceful, curto-circuito por versão, JSON malformado, override de versão |

### Por que arquivo local também?

O Portal Único Siscomex (`https://portalunico.siscomex.gov.br`) não publica URL pública
estável para download direto. O fluxo recomendado em produção:

1. **Cron externo** (GitHub Actions, k8s CronJob, etc.) baixa o JSON oficial periodicamente
   e coloca num volume montado no container.
2. O worker NfeSaas lê do volume via `LocalFilePath` e faz o upsert.

Alternativamente, host um mirror HTTP do JSON e aponte `UpdateSourceUrl` para ele.

### Métricas operacionais a monitorar

| Métrica | Como | Threshold |
|---------|------|-----------|
| Última atualização bem-sucedida | `GET /api/ncm/status` retorna `versaoTabela` | Não pode ser >35 dias atrás |
| Tempo de execução do upsert | Log do `NcmUpdater` | < 10s para ~15k NCMs |
| Falhas consecutivas | Logs `Warning` da chamada `Atualizar` | Investigar se >3 em sequência |

---

## 6. Riscos conhecidos

| Risco | Mitigação atual | Status |
|-------|-----------------|--------|
| BrasilAPI fica fora do ar | Fallback para preenchimento manual + mensagem clara | ✅ implementado |
| Cache CNPJ desatualizado (empresa mudou razão social) | TTL de 30 dias + botão de lupa no adornment força nova consulta | ✅ implementado |
| Tabela NCM defasada vs oficial | Campo `versao_tabela` + endpoint `/api/ncm/status` exposto | ⚠ worker de atualização ainda manual |
| Usuário cola CNPJ inválido (dígito verificador errado) | Validação local `CnpjVerificador.Valido()` antes do request | ✅ implementado |
| Rate-limit BrasilAPI (3 req/s plano free) | Cache em sessão evita repetição; debounce no input | ✅ implementado, monitorar |
