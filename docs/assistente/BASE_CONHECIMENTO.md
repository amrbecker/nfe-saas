# Base de Conhecimento do Assistente — Fontes e Governança

> A base é o ativo mais importante do assistente. O modelo de IA pode ser trocado; uma base curada, citada e
> atualizada é o que torna as respostas confiáveis para contadores.

---

## 1. Princípios

1. **Somente fontes oficiais ou primárias** para regra fiscal. Blogs, fóruns e vídeos servem para
   descobrir dúvidas frequentes — nunca como fonte citada.
2. **Todo artigo tem proveniência:** URL oficial, documento e versão (ex.: MOC versão X, NT AAAA.NNN
   vN.NN), data de verificação e data-limite de revisão.
3. **Vigência explícita.** Regras com data de início/fim (Reforma Tributária, NTs com cronograma de
   produção/homologação) registram as duas datas. O agente compara com a data atual antes de responder.
4. **Resumo próprio, não cópia integral.** O artigo resume em linguagem clara e cita o trecho/seção
   oficial. Documentos oficiais completos ficam arquivados em PDF fora do prompt, para consulta do curador.
5. **Lacuna registrada, não inventada.** Pergunta sem cobertura vira item em `SinalProduto` do tipo
   `LacunaConhecimento` e entra na fila de curadoria.

---

## 2. Catálogo de fontes

### 2.1 Nacionais — NF-e / NFC-e (prioridade máxima)

| Fonte | Onde | Uso no assistente | Frequência de checagem |
|-------|------|-------------------|------------------------|
| Manual de Orientação do Contribuinte (MOC) — leiaute e regras de validação | Portal Nacional da NF-e (`www.nfe.fazenda.gov.br`) → Documentos → Manuais | Regras de preenchimento campo a campo; **tabela de códigos de rejeição (cStat/xMotivo)** | Mensal |
| Notas Técnicas (NTs) vigentes e em homologação | Portal Nacional da NF-e → Documentos → Notas Técnicas | Novos campos, novas regras de validação, cronogramas (incluindo as NTs da Reforma Tributária: grupo IBS/CBS, `cClassTrib`) | **Quinzenal** |
| Schemas XSD (pacotes de liberação) | Portal Nacional da NF-e → Documentos → Esquemas | Já versionados em `docs/schemas/`; o assistente explica erros de validação de schema | A cada novo pacote |
| Tabelas de CST/cClassTrib do IBS/CBS | Portal Nacional da NF-e (tabelas publicadas com as NTs da RTC) | Explicar CST e cClassTrib que o usuário informa na emissão | Mensal durante a transição |
| Manual e NTs da NFC-e (QR Code, CSC, contingência offline) | Portal Nacional da NF-e + portais das SEFAZ estaduais | NFC-e, CSC, QR Code | Mensal |
| Disponibilidade dos autorizadores / SVC-AN / SVC-RS | Portal Nacional da NF-e → Disponibilidade | Explicar "SEFAZ indisponível" e contingência (casar com `SefazService.cs`) | Tempo real (via ferramenta, não pela base) |

### 2.2 Legislação tributária

| Fonte | Onde | Uso |
|-------|------|-----|
| Ajuste SINIEF 07/05 (institui a NF-e) e Ajuste SINIEF 19/16 (NFC-e) | CONFAZ (`www.confaz.fazenda.gov.br`) | Base legal de emissão, cancelamento, CC-e, inutilização, prazos |
| Convênio ICMS 142/18 (substituição tributária / CEST) | CONFAZ | CEST obrigatório, ST |
| LC 87/1996 (Lei Kandir) | Planalto (`www.planalto.gov.br`) | Conceitos de ICMS |
| LC 123/2006 (Simples Nacional) + Resoluções CGSN | Planalto / Receita Federal | CSOSN, crédito de ICMS no Simples |
| EC 132/2023 e LC 214/2025 (Reforma Tributária — IBS/CBS/IS) | Planalto | Cronograma de transição 2026–2033, conceitos |
| Tabela NCM (TEC) | Receita Federal / Siscomex | Já importada (`Ncm`, `NcmUpdateWorker`) — o assistente consulta via ferramenta |
| Tabela CNAE | IBGE/CONCLA | Já importada (`Cnae`) |
| Tabela de municípios (código IBGE) | IBGE | Rejeições de código de município |
| RICMS das UFs dos clientes ativos | Portais das SEFAZ estaduais | Particularidades estaduais — **priorizar por UF com mais empresas cadastradas** |

### 2.3 Certificado digital e LGPD

| Fonte | Onde | Uso |
|-------|------|-----|
| ICP-Brasil — normas e cadeia de ACs | ITI (`www.gov.br/iti`) | Certificado A1, validade, cadeia (ver commit que instalou a cadeia ICP-Brasil na imagem) |
| LGPD — Lei 13.709/2018 | Planalto | Respostas sobre privacidade; política do próprio assistente |
| Guias da ANPD | `www.gov.br/anpd` | Orientações de tratamento |

### 2.4 Fontes internas (verdade sobre o próprio produto)

| Fonte | Uso |
|-------|-----|
| `CLAUDE.md`, `docs/README.md`, `docs/UX_AUTOMACAO_FISCAL.md` | Como o sistema funciona, fluxos, trial/plano |
| Páginas em `src/WebUI/Pages/*.razor` | Passo a passo de cada tela (nomes de botões e menus reais) |
| Mensagens de erro da API (`ExceptionMiddleware`, validators FluentValidation) | Mapear erro técnico → explicação e ação |
| Histórico do git (commits `fix:`) | Problemas conhecidos e corrigidos (evita reabrir chamado de bug já resolvido) |
| Chamados resolvidos | Viram artigos "Problema conhecido / solução" |

---

## 3. Estrutura de arquivos

```
docs/assistente/kb/
  _INDICE.md                    # índice e status de cada artigo (gerado/validado por script)
  _TEMPLATE.md                  # modelo de artigo
  sistema/                      # como usar o NfeSaas
  rejeicoes/                    # um arquivo por faixa/família de cStat (ex.: 2xx-emitente.md)
  preenchimento/                # regras de campos (CST, CSOSN, CFOP, NCM/CEST, IBS/CBS...)
  eventos/                      # cancelamento, CC-e, inutilização, manifestação
  certificado/
  nfce/
  reforma-tributaria/
  problemas-conhecidos/
```

Formato: Markdown com front matter YAML (ver `kb/_TEMPLATE.md`). Motivos: versionado no git, revisável
em PR, diff legível, carregável no prompt sem conversão, migrável para pgvector depois.

---

## 4. Governança (RACI)

| Atividade | Responsável | Aprova | Frequência |
|-----------|-------------|--------|------------|
| Monitorar Portal NF-e (NTs, MOC, schemas) | Curador fiscal | PO | Quinzenal |
| Escrever/atualizar artigo | Curador fiscal ou dev (artigos `sistema/`) | PO (via PR) | Contínuo |
| Revisão de artigos com `revisar_ate` vencido | Curador fiscal | — | Semanal (lista automática) |
| Triar lacunas de conhecimento | PO | — | Semanal |
| Rodar eval após mudança de base/prompt/modelo | Dev | PO | A cada PR que toca `kb/` ou prompt |

**Regra de merge:** PR que altera `docs/assistente/kb/` ou o prompt do agente só entra com o eval verde
(precisão ≥ 95%, zero respostas classificadas como perigosas).

---

## 5. Priorização da carga inicial (Fase 0)

Ordem por impacto medido em rejeições reais (ajustar com os dados de `NotaFiscal.MotivoRejeicao` em produção):

1. **Rejeições mais frequentes** — extrair o top 30 de `MotivoRejeicao` do banco de produção e escrever um
   artigo por família (causa, como corrigir no NfeSaas, fonte no MOC).
2. **Uso do sistema** — 1 artigo por página da WebUI (emitir NF-e, cancelar, CC-e, inutilizar, certificado,
   configuração inicial, cadastrar escritório como empresa, trial/plano).
3. **Certificado A1** — upload, senha, validade, erro de cadeia.
4. **CST/CSOSN/CFOP** — tabelas resumidas com os casos mais usados pelos perfis de `PerfilCliente`.
5. **IBS/CBS** — o que já é obrigatório, o que é informativo, datas.
6. **NFC-e** — CSC, QR Code, contingência.
