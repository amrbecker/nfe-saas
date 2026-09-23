# Fase de Pesquisa e Refinamento — Assistente NFeFlow

> Registro de decisões e achados da pesquisa de 2026-09-23. Atualiza a estratégia original
> ([`ESTRATEGIA.md`](ESTRATEGIA.md)) e aponta para os documentos detalhados:
>
> | Tema | Documento |
> |------|-----------|
> | Mascote, animações, posicionamento, dicas silenciosas | [`MASCOTE_UX.md`](MASCOTE_UX.md) |
> | O que capturar da sessão para "adivinhar" a dúvida | [`CAPTURA_CONTEXTO.md`](CAPTURA_CONTEXTO.md) |
> | Sugestões pós-ação, padrões, lembretes, preenchimento | [`AUTOMACOES.md`](AUTOMACOES.md) |
> | Monitoramento automático de fontes | [`MONITORAMENTO_FONTES.md`](MONITORAMENTO_FONTES.md) |
> | Guarda-corpos e linguagem por nível de fonte | [`CONTEXTO_AGENTE.md`](CONTEXTO_AGENTE.md) §3 e [`BASE_CONHECIMENTO.md`](BASE_CONHECIMENTO.md) §1.1 |

---

## 1. Decisões registradas

| # | Decisão | Origem | Status |
|---|---------|--------|--------|
| D1 | Produto se chama **NFeFlow** (código segue com namespaces `NfeSaas.*` — renomear código não traz valor) | PO | ✅ Aplicada nos docs |
| D2 | Assistente tem **mascote animado**, na linha afetiva do Clippy do Office, mas sem os erros dele | PO | ✅ Detalhada em `MASCOTE_UX.md` |
| D3 | Mascote = **coruja** (sabedoria, vigilância, "trabalha até tarde no fechamento do mês"). Evitar o **leão**, que remete à Receita/IR e tem conotação de cobrança | PO + análise | ✅ Nome: **Ori** (de "orientar") |
| D4 | Ao ser chamada, a coruja **primeiro levanta hipóteses da dúvida** a partir do contexto da sessão | PO | ✅ Viável — ver `CAPTURA_CONTEXTO.md` |
| D5 | **Nunca executa.** No máximo preenche e prepara; o botão de conclusão é sempre do usuário | PO | ✅ Regra nº 1 do agente |
| D6 | Propõe automações pós-ação (salvar destinatário/produto, resgatar dados, padrões, lembretes de nota periódica) | PO | ✅ Catálogo em `AUTOMACOES.md` |
| D7 | Modelo **DeepSeek** (bom e barato), **cota por usuário** e prompts otimizados | PO | ✅ Hospedado no **Microsoft Foundry** (decidido); API direta só para fontes públicas — ver §2 |
| D8 | Respostas com guarda-corpos, linguagem neutra, citação de fonte oficial e termos adequados para fonte não oficial; devem **transmitir segurança** | PO | ✅ Níveis de fonte N1–N4 |
| D9 | Rotina automática de verificação de fontes oficiais + monitoramento de referências respeitadas | PO | ✅ Com publicação sempre revisada por humano |
| D10 | **O modelo não recebe dado pessoal** — pergunta do PO: "por que o modelo precisa do CPF?" Resposta: não precisa. Recebe apenas **atributos e resultados de validação** (§2.6) | PO | ✅ Princípio de arquitetura |
| D11 | **Curadoria** da base de conhecimento feita por um **escritório parceiro** | PO | ✅ Regras de acesso e acordo em §4 |
| D12 | Termos de uso revisados pelo **escritório parceiro** | PO | ✅ Com ressalva em §4 |
| D13 | Desenho do mascote com **ferramentas de IA**, com acabamento humano, **preferencialmente gratuitas** | PO | ✅ Caminho gratuito em `MASCOTE_UX.md` §7.1b (plano grátis do Recraft descartado: sem uso comercial) |

---

## 2. DeepSeek — análise (pesquisa feita em 2026-09-23)

### 2.1 O que a pesquisa encontrou

**Modelos e preço (API oficial, US$ por 1M tokens; faixa = fora do pico → pico):**

| Modelo | Contexto | Entrada com cache | Entrada sem cache | Saída | Recursos |
|--------|----------|-------------------|-------------------|-------|----------|
| `deepseek-flash` | 1M | 0,003 – 0,006 | 0,15 – 0,30 | 0,60 – 1,20 | tool calls, JSON, visão, modo de raciocínio |
| DeepSeek-V4-Pro | 1M | 0,022 – 0,044 | 0,66 – 1,32 | 1,98 – 3,96 | tool calls, JSON, modo de raciocínio |

- **O horário de pico da DeepSeek é 01–04h e 06–10h UTC** (22h–01h e 03h–07h em Brasília), de segunda a sexta.
  **O horário comercial brasileiro cai inteiro na faixa de meio preço.**
- O cache de contexto é **automático** (em disco, por prefixo idêntico) — não precisa de marcação como no Claude, mas
  o prefixo precisa ser estável do mesmo jeito.
- API compatível com o formato OpenAI (e com o formato Anthropic).

**Custo estimado por conversa** (5 turnos, prefixo fixo de 8k em cache, 1,5k novos + 1,5k de saída por turno, fora do pico):
`deepseek-flash` ≈ **US$ 0,006** · V4-Pro ≈ **US$ 0,02**. Isso é de 20 a 60 vezes mais barato que a estimativa anterior.
**Conclusão: com DeepSeek, custo deixa de ser o fator limitante.** A cota continua necessária, mas por
**abuso e previsibilidade**, não por margem.

### 2.2 O ponto crítico: onde os dados ficam (LGPD)

| Fato | Fonte |
|------|-------|
| A política de privacidade da DeepSeek declara que coleta, processa e **armazena dados pessoais na República Popular da China** | [Política de privacidade DeepSeek](https://cdn.deepseek.com/policies/en-US/deepseek-privacy-policy.html) |
| A retenção é indefinida enquanto a conta existir; os dados são usados para treinar modelos, com opção de opt-out | Idem |
| Só EEE, Suíça e Reino Unido têm termos específicos de transferência. **O Brasil não é mencionado** | Idem |
| Reguladores de vários países (Itália, Coreia do Sul, Austrália, Taiwan, Holanda) bloquearam, investigaram ou restringiram a DeepSeek, sobretudo no setor público | [DPO Centre](https://www.dpocentre.com/news/deepseek-under-scrutiny-privacy-concerns/), [DataNorth](https://datanorth.ai/blog/deepseek-data-privacy-and-security-key-insights-on-protection-and-risks) |
| **DeepSeek V4-Flash e V4-Pro estão disponíveis no Microsoft Foundry** (Azure), com residência de dados na região escolhida e o contrato de tratamento de dados da Microsoft | [Foundry — V4-Flash](https://ai.azure.com/catalog/models/DeepSeek-V4-Flash), [Foundry — V4-Pro](https://ai.azure.com/catalog/models/DeepSeek-V4-Pro) |
| O AWS Bedrock também oferece modelos DeepSeek; a V4 chega primeiro às regiões dos EUA | [innFactory](https://innfactory.ai/en/ai-models/deepseek/) |

A LGPD (art. 33) só permite transferência internacional com mecanismo válido. Um deles são as cláusulas-padrão
contratuais da ANPD (Resolução CD/ANPD nº 19/2024). O NFeFlow trata dados de terceiros: destinatários
pessoa física (CPF, endereço) e dados comerciais dos clientes dos escritórios. Mandar isso para um servidor na
China, sem contrato com cláusulas e com possibilidade de uso para treino, é **risco jurídico e comercial**.
Um escritório contábil que ler "seus dados vão para a China" no termo de uso pode cancelar.

> **Atualização (D10):** com a minimização da §2.6, o modelo **não recebe dado pessoal**, o que reduz muito esse risco. O Foundry continua como endpoint por defesa em profundidade e por causa do dado comercial.

### 2.3 Endpoint — decidido: Microsoft Foundry (2026-09-23)

**DeepSeek como modelo, hospedado no Foundry.** Opções avaliadas:

| Opção | Onde o dado fica | Custo | Recomendação |
|-------|------------------|-------|--------------|
| **A. DeepSeek V4 via Microsoft Foundry** | Região Azure escolhida (verificar disponibilidade em Brazil South; senão, EUA ou UE) | Maior que a API direta (confirmar preço no Foundry) — mesmo assim uma fração do Claude | ✅ **Escolhido** para tudo que envolve dado de cliente |
| B. API oficial DeepSeek com **payload sanitizado** | China | O mais baixo | ⚠️ Aceitável **só** para fluxos sem dado pessoal nem comercial: monitoramento de fontes públicas (DOU, NTs), geração de rascunhos da base de conhecimento e sumarização de sinais já anonimizados |
| C. API oficial DeepSeek para tudo | China | O mais baixo | ❌ Não recomendado |

**Implementação independente de provedor:** interface `IAssistenteIA` em `Application/` com implementação via
`Microsoft.Extensions.AI` (`IChatClient`) e cliente compatível com OpenAI. Trocar entre Foundry, API DeepSeek ou
outro provedor vira **configuração** (`Assistente__Endpoint`, `Assistente__Modelo`, `Assistente__ApiKey`), sem
mudar handlers. Rota por finalidade: `Assistente__Rotas__Conversa` (A) e `Assistente__Rotas__FontesPublicas` (B).

**Qualidade:** o DeepSeek precisa passar pelo **mesmo eval fiscal** (≥ 95% de precisão e zero resposta perigosa,
em português). Ponto a medir com cuidado: confiabilidade das chamadas de ferramenta em PT-BR. Se o `flash` não
atingir a meta, usar o V4-Pro nas perguntas fiscais e o `flash` no resto (roteamento, dicas, sumarização).

### 2.4 Economia de tokens — técnicas adotadas

| # | Técnica | Economia esperada |
|---|---------|-------------------|
| E1 | **Primeira tela sem LLM**: hipóteses de dúvida geradas por regras a partir do contexto (`CAPTURA_CONTEXTO.md` §4) | 100% nas aberturas em que o usuário clica numa hipótese já respondida pela base |
| E2 | **Respostas prontas**: se a hipótese mapeia para um artigo (rejeição 778 → artigo X), a coruja mostra o resumo do artigo sem chamar o modelo | Alta — cobre boa parte das dúvidas de rejeição |
| E3 | **Base injetada por roteamento, não inteira**: prefixo fixo curto (persona + regras ≈ 2–3k tokens) + 1 a 3 artigos escolhidos por código de rejeição, tela ou campo | −80% de tokens por turno em relação a mandar a base inteira |
| E4 | **Contexto compacto** em JSON com chaves curtas e só o necessário (≤ 600 tokens) | — |
| E5 | **Resultados de ferramenta projetados** (só os campos úteis, itens resumidos) | −50–70% nos turnos com ferramenta |
| E6 | **Histórico com janela**: últimos 6 turnos + resumo curto dos anteriores | Limita o crescimento das conversas longas |
| E7 | **Modo de raciocínio só quando precisa**: desligado para dicas, roteamento e reformulação; ligado para perguntas fiscais | −30–60% de saída |
| E8 | `max_tokens` por rota (dica: 150; explicação: 600; conversa: 1.200) | Evita respostas longas demais |
| E9 | Prefixo byte-idêntico (nada de data ou nome no início) para maximizar acerto do cache automático | Até −90% na entrada |
| E10 | Tarefas de fundo (fontes, insights) em lote fora do pico | −50% |

### 2.5 Cotas

| Nível | Limite padrão (ajustável por plano) | Ao atingir |
|-------|--------------------------------------|------------|
| Por usuário / dia | 40 mensagens ao modelo | A coruja "vai dormir": continua mostrando hipóteses e artigos (E1/E2, sem custo), mas não chama o modelo até o dia seguinte |
| Por usuário / mês | Básico 150 · Profissional 600 · Enterprise 2.000 | Mensagem amigável + aviso ao Admin do escritório |
| Por escritório / mês (teto) | Soma dos usuários × 1,5 | Alerta interno ao PO |
| Por conversa | 20 turnos ou 60k tokens | Sugere começar nova conversa (com resumo) |

Implementação: tabela `UsoAssistente` (`UsuarioId`, `EscritorioId`, `Dia`, `Mensagens`, `TokensEntrada`,
`TokensCache`, `TokensSaida`, `CustoEstimado`) com incremento atômico; checagem antes de chamar o modelo.
Contador visível para o usuário ("12 de 40 perguntas hoje").

---

### 2.6 Minimização: o modelo não recebe dado pessoal (D10)

**Pergunta do PO:** *por que o modelo precisa receber dados do usuário? Saber o CPF não ajuda.*

**Resposta: correto, não precisa.** Para explicar uma rejeição ou orientar um preenchimento, o modelo precisa saber
**como** o dado é e **se ele passa nas validações**, não **qual** é o dado. As rejeições ligadas à identidade (dígito
verificador de CPF/CNPJ, IE incompatível com a UF, destinatário não contribuinte, CNPJ inexistente) são verificadas
**antes, no servidor**, por validadores determinísticos. O modelo recebe só o resultado.

| Dado real | O que o modelo recebe | Por que basta |
|-----------|-----------------------|---------------|
| CPF `123.456.789-09` | `dest.tipo: PF`, `dest.doc_valido: true` | A regra muda conforme PF ou PJ, não conforme o número |
| CNPJ do destinatário | `dest.tipo: PJ`, `dest.doc_valido: true`, `dest.situacao_receita: ATIVA` (quando consultado) | Idem |
| Razão social ou nome | `[DESTINATARIO]` (marcador) | Não influencia nenhuma regra fiscal |
| Endereço completo | `dest.uf: MG`, `dest.cmun_compativel_uf: true` | A operação interna × interestadual depende só da UF |
| IE | `dest.ie: presente`, `dest.ie_formato_valido_uf: false`, `dest.ind_ie: 1` | É isso que explica a rejeição de IE |
| E-mail e telefone | nada | Irrelevantes |
| CNPJ, nome e IE do **emitente** | `emit.uf`, `emit.regime`, `emit.ie_valida` | Idem |
| Descrição do produto | enviada (é dado comercial, necessária para orientar NCM/CFOP) | Filtro remove padrões de CPF, e-mail e telefone que apareçam no texto |
| Valores (itens, totais) | enviados **só** quando a rejeição é de totais ou cálculo | Dado comercial, útil apenas nesses casos |
| Pergunta livre do usuário | passa pelo mesmo filtro (CPF, CNPJ, e-mail, telefone, chave de acesso → marcadores) | O usuário pode colar um CPF sem perceber; a Ori avisa "removi dados pessoais da sua pergunta" |

**Reidratação local:** a resposta do modelo usa marcadores (`[DESTINATARIO]`, `[PRODUTO_2]`). O servidor guarda o
mapa marcador → valor real **apenas durante a requisição** e troca os marcadores antes de mostrar. O usuário lê o nome
real; o modelo nunca viu.

**Implementação:** `SanitizadorIA` em `Application/Services/Assistente/`, com validadores reaproveitados (dígito
verificador de CPF/CNPJ, formato de IE por UF, código IBGE × UF) e testes unitários com corpus de CPFs, CNPJs,
e-mails e chaves de acesso. Todo texto que sai para o modelo — contexto, resultado de ferramenta, pergunta e
histórico — passa por ele. **Nenhuma ferramenta retorna documento ou nome cru.**

**Consequências:**
1. O risco de transferência internacional de dado pessoal (R9) cai muito. O Foundry continua sendo o endpoint
   (decisão D7) como **defesa em profundidade** para dado comercial (produtos, valores) e para falhas do filtro.
2. As conversas gravadas (`Conversa`/`MensagemConversa`) guardam a **versão sanitizada** enviada ao modelo. A versão
   reidratada existe só na tela do usuário.
3. Os chamados de bug e o relatório de insights já nascem sem dado pessoal.
4. O eval ganha casos de vazamento: perguntas com CPF, nome e e-mail embutidos devem sair sanitizadas (100%).

---

## 3. Refinamentos sobre a `ESTRATEGIA.md`

### 3.1 Regulatório / jurídico → de 🟡 para 🟢 (condicionado)

As mitigações pedidas pelo PO elevam a nota, desde que implementadas. Resumo (detalhe em `CONTEXTO_AGENTE.md` §3):

1. **Níveis de fonte com linguagem própria**:
   - **N1** — norma oficial, texto assertivo com citação.
   - **N2** — orientação oficial, com "orienta" e a indicação da UF ou órgão.
   - **N3** — referência técnica reconhecida, com "segundo análise de…" e aviso de que é interpretação.
   - **N4** — sem fonte: não responde a regra.
2. **Revisão automática antes de exibir** (verificador determinístico): toda alíquota, prazo, código ou data na
   resposta precisa aparecer nos artigos citados. Se não aparecer, a resposta volta ao modelo com a instrução de
   corrigir ou é trocada por "não encontrei base oficial para isso".
3. **Selo de confiança visível** em cada resposta: "Norma oficial", "Orientação oficial" ou "Interpretação de mercado".
4. **Linguagem neutra**: sem "com certeza", "nunca será autuado" ou "pode ficar tranquilo". A segurança vem da
   **fonte e da clareza**, não de adjetivos. Formato fixo: *O que fazer → Por quê → Base → Próximo passo*.
5. Revisão humana por amostragem (20 conversas por semana) e eval em todo PR.

### 3.2 Operacional → de 🟡 para 🟢 (condicionado)

Monitoramento automático das fontes (`MONITORAMENTO_FONTES.md`):

- **O que muda:** o assistente **detecta, resume e propõe**; o curador **aprova e publica**.
- **Por que não publicar direto:** uma NT mal interpretada vira resposta errada para todos os clientes.
- **O que a pesquisa encontrou sobre os canais:**
  - O Portal Nacional da NF-e não publica RSS; a lista de NTs precisa ser monitorada por mudança de página.
  - O DOU tem download oficial e gratuito em XML pelo **INLABS** da Imprensa Nacional.
  - Existe uma ferramenta pública de clipping do DOU mantida pelo governo, o **Ro-DOU**, que serve de referência de implementação.

### 3.3 Riscos novos

| # | Risco | Mitigação |
|---|-------|-----------|
| R9 | Transferência internacional de dados para a China (LGPD art. 33) | Minimização: nenhum dado pessoal vai ao modelo (§2.6) + endpoint Foundry + API direta só para dado público (§2.3) |
| R10 | Mascote vira "Clippy irritante" | Regras anti-intrusão em `MASCOTE_UX.md` §5; métrica de "silenciar" como alarme |
| R11 | Automação preenche errado e o usuário emite sem revisar | Campos preenchidos pela coruja ficam destacados até o usuário editar ou confirmar; resumo do que foi preenchido; botão "desfazer preenchimento" |
| R12 | Captura de contexto vira vigilância percebida | Rastro só na memória do navegador, enviado **apenas quando o usuário chama a coruja**, com "o que a coruja está vendo" visível e editável (`CAPTURA_CONTEXTO.md` §5) |
| R13 | Monitoramento de sites de terceiros quebra quando o HTML muda | Detector por hash + alerta de "fonte não lida há X dias"; frequência baixa e respeito aos termos de uso |

---

## 4. Escritório parceiro: curadoria e termos (D11, D12)

**Papel:** curar a base de conhecimento (revisar a fila do monitor de fontes, escrever e revisar artigos, revisar o
eval) e revisar os termos de uso.

**Regras de acesso:**

| O parceiro acessa | O parceiro **não** acessa |
|-------------------|---------------------------|
| Fila de curadoria (publicações detectadas, rascunhos, artigos vencidos) | Dados de nenhum outro escritório cliente |
| Lacunas de conhecimento **anonimizadas e agregadas** ("12 perguntas sobre cClassTrib em operações com SP esta semana") | Texto integral das conversas, nomes de escritórios ou empresas |
| Métricas agregadas do eval | Chamados de bug com contexto técnico |

Implementação: papel novo `Curador` (claim próprio, **fora** da hierarquia Escritório → Empresa), com acesso apenas
às telas de curadoria. As lacunas passam pelo `SanitizadorIA` e são agrupadas antes de aparecer.

**Acordo formal recomendado** (antes da Fase 0.4):
- Confidencialidade (o parceiro vê o roadmap e as lacunas do produto).
- Responsabilidade técnica: artigos assinados pelo contador responsável (nome e CRC em `curador:` no front matter),
  o que também reforça a confiança dos usuários ("revisado por contador").
- Independência: se o parceiro também for cliente do NFeFlow, a conta de cliente e o papel de curador ficam separados.
- Remuneração ou contrapartida (ex.: plano Enterprise gratuito) e SLA da fila de curadoria (2× por semana).

**Termos de uso:** o escritório parceiro conhece bem a relação com os contribuintes e o sigilo profissional, o que
ajuda na revisão. **Ressalva:** as cláusulas de LGPD (operador e controlador, transferência internacional pelo
Foundry, retenção, direitos do titular) e a limitação de responsabilidade pela orientação fiscal são matéria
jurídica. Recomendo que o parceiro envolva o advogado que o assessora nessas cláusulas específicas, ou que elas
passem por uma revisão jurídica pontual.

---

## 5. Fontes consultadas nesta pesquisa

- DeepSeek — preços: https://api-docs.deepseek.com/quick_start/pricing
- DeepSeek — política de privacidade: https://cdn.deepseek.com/policies/en-US/deepseek-privacy-policy.html
- Microsoft Foundry — DeepSeek-V4-Flash: https://ai.azure.com/catalog/models/DeepSeek-V4-Flash
- Microsoft Foundry — DeepSeek-V4-Pro: https://ai.azure.com/catalog/models/DeepSeek-V4-Pro
- innFactory — DeepSeek em hyperscalers: https://innfactory.ai/en/ai-models/deepseek/
- DPO Centre — escrutínio regulatório: https://www.dpocentre.com/news/deepseek-under-scrutiny-privacy-concerns/
- DataNorth — privacidade DeepSeek: https://datanorth.ai/blog/deepseek-data-privacy-and-security-key-insights-on-protection-and-risks
- Portal Nacional da NF-e — Notas Técnicas: https://www.nfe.fazenda.gov.br/portal/listaConteudo.aspx?tipoConteudo=04BIflQt1aY%3D
- Portal DF-e SVRS — Notícias: https://dfe-portal.svrs.rs.gov.br/Nfe/Noticias
- INLABS (Imprensa Nacional): https://inlabs.in.gov.br/ · https://github.com/Imprensa-Nacional/inlabs
- Ro-DOU (clipping do DOU): https://gestaogovbr.github.io/Ro-dou/como_funciona/pesquisa_dou/

> Preços e disponibilidade mudam: revalidar antes da contratação e registrar a data aqui.
