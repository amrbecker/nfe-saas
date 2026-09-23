# Monitoramento Automático de Fontes

> Rotina em que o assistente **vigia** as fontes oficiais e as referências, **resume** o que mudou e **propõe**
> atualizações na base de conhecimento. **Publicar é sempre ato humano** (curador), porque uma NT mal interpretada
> vira resposta errada para todos os clientes.

---

## 1. Fluxo

```
[Detectar] ──▶ [Coletar] ──▶ [Classificar] ──▶ [Propor] ──▶ [Revisar] ──▶ [Publicar]
 worker        baixa o doc    LLM: é relevante  rascunho de   curador       PR em kb/
 por fonte     novo (PDF/     para NF-e/NFC-e?  artigo + lista aprova/edita  + eval verde
 (hash/diff)   HTML/XML)      impacto? vigência? artigos afetados
                              ▼
                        Impacto técnico? ──▶ issue para dev (leiaute/XSD/URL SEFAZ)
```

- Artigos afetados por uma mudança detectada passam para o status **`em_revisao`**. Enquanto isso, a coruja continua
  citando o artigo com o aviso "regra em atualização — confira a publicação de {data}".
- Classificação e resumo usam **DeepSeek pela API direta** (fonte pública, sem dado pessoal) em lote, fora do pico.

---

## 2. Fontes, canais e frequência

### Nível N1 — Normativo oficial

| Fonte | Canal oficial | Mecanismo de detecção | Frequência |
|-------|---------------|-----------------------|------------|
| Portal Nacional da NF-e — Notas Técnicas | Página de lista de NTs (sem RSS) | Hash da lista; item novo ou versão nova (ex.: "NT 2025.002 v1.35") → baixa o PDF | **Diária** |
| Portal Nacional da NF-e — Informes e notícias | Página de notícias | Hash + diff | Diária |
| Portal Nacional — pacotes de schemas (XSD) | Página de documentos | Hash; novo pacote → issue técnica automática (comparar com `docs/schemas/`) | Diária |
| Portal DF-e SVRS — Notícias (autorizadora de várias UFs e da SVC-RS) | `dfe-portal.svrs.rs.gov.br/Nfe/Noticias` | Hash + diff | Diária |
| Diário Oficial da União — Seção 1 | **INLABS** (Imprensa Nacional): XML diário gratuito e oficial | Download do XML do dia; filtro por órgão (CONFAZ, Receita Federal, Comitê Gestor do IBS) e termos (`Ajuste SINIEF`, `Convênio ICMS`, `Nota Fiscal Eletrônica`, `NF-e`, `NFC-e`, `IBS`, `CBS`, `cClassTrib`) | **Diária** (dias úteis) |
| CONFAZ — atos (convênios, ajustes, protocolos, despachos) | Site do CONFAZ (também sai no DOU) | Hash das listas do ano corrente | Semanal (o DOU cobre o diário) |
| Receita Federal — atos normativos | Sistema de normas da RFB | Busca por termos | Semanal |
| SEFAZ estaduais (UFs com clientes ativos) | Portais de NF-e e legislação de cada UF | Hash das páginas de notícias/NF-e | Semanal; diária para as 3 UFs com mais empresas |
| Reforma Tributária (IBS/CBS) — Comitê Gestor e RFB | Portais oficiais | Hash + DOU | Semanal |

### Nível N2 — Orientação oficial

| Fonte | Mecanismo | Frequência |
|-------|-----------|------------|
| FAQs e perguntas e respostas das SEFAZ e da RFB | Hash | Mensal |
| Soluções de consulta (RFB e SEFAZ) com tema NF-e | Busca por termos | Mensal |

### Nível N3 — Referências técnicas reconhecidas (não oficiais)

Servem como **sinal antecipado** ("o mercado está discutindo X") e para **explicar** com outras palavras.
**Nunca** viram base de regra.

**Critérios de admissão** (o PO aprova cada uma): publicação editorial identificada, autoria técnica (contador,
advogado tributarista, auditor), histórico de correção de erros, citação das fontes oficiais nos próprios textos.

Candidatas a avaliar: portais de notícias contábeis de grande circulação, cadernos de legislação e tributos de
jornais econômicos, sites jurídicos com seção tributária e publicações de conselhos profissionais (CFC/CRCs).
Lista final em `docs/assistente/kb/_fontes_n3.md` após avaliação.

| Mecanismo | Frequência | Saída |
|-----------|------------|-------|
| RSS quando houver, senão hash da página de categoria | Semanal | Só **alerta de atenção** ao curador ("3 portais comentam possível adiamento de X") — nunca rascunho de artigo |

### Tempo real (fora da base)

| Informação | Canal | Uso |
|------------|-------|-----|
| Disponibilidade dos autorizadores | Página de disponibilidade do Portal NF-e e o próprio `ISefazService` | Ferramenta `consultar_status_sefaz`; não entra na base |

---

## 3. Boas práticas de coleta

- Identificar o robô (`User-Agent: NFeFlow-Monitor/1.0 (+contato)`), respeitar `robots.txt` e os termos de uso, no
  máximo 1 requisição por página por execução.
- Guardar o documento original (PDF/XML) com o hash e a data, para auditoria ("o artigo X foi baseado na NT Y
  v1.35, baixada em …").
- **Alerta de fonte cega:** fonte sem leitura bem-sucedida há 3 execuções → aviso ao dev (o HTML mudou).
- Referência de implementação para o DOU: o projeto oficial INLABS (`github.com/Imprensa-Nacional/inlabs`) e o
  Ro-DOU (clipping do governo federal).

---

## 4. Implementação

| Peça | Onde | Observação |
|------|------|-----------|
| `FonteMonitorada` (id, nome, nível, url, mecanismo, frequência, último hash, última leitura, falhas seguidas) | Domain + migration | Cadastro das fontes da §2 |
| `PublicacaoDetectada` (fonte, título, url, hash, data, relevância, resumo, vigência, artigos afetados, status: `nova`, `descartada`, `em_curadoria`, `incorporada`) | Domain | Fila do curador |
| `MonitorFontesWorker` | `API/Workers` (mesmo padrão do `NcmUpdateWorker`) | Roda às 6h de Brasília (09h UTC, fora do pico DeepSeek) |
| Tela "Fila de curadoria" (interna) | WebUI, restrita | Aprovar → gera rascunho de PR em `kb/` (ou o curador edita manualmente) |
| E-mail semanal ao curador e ao PO | `IEmailService` | Resumo: novidades N1/N2, alertas N3, artigos vencidos |

**Relação com o código:** mudanças de leiaute ou regra de validação detectadas numa NT devem virar **issue técnica**
(ex.: novo campo no XML, nova regra de validação que o `XsdValidationService` ou o `XmlNFeService` precisam
refletir, URLs em `SefazService.cs`). O monitor transforma a revalidação periódica, hoje manual, em processo.
