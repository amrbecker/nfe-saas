# Automações Propostas pela Coruja

> Princípio inegociável: **a coruja prepara, o usuário conclui.** Ela pode preencher formulários, montar rascunhos e
> criar lembretes (com confirmação), mas **nunca** clica em Emitir, Cancelar, Enviar CC-e, Inutilizar, Salvar
> cadastro ou Enviar e-mail. O botão final é sempre do usuário.

A maioria das automações abaixo é **determinística** (SQL e regras): não usa o modelo, é instantânea e custa zero. O
LLM entra só para redigir texto, explicar ou lidar com casos ambíguos.

---

## 1. Catálogo

| # | Gatilho | Proposta da coruja | O que ela prepara | Conclusão pelo usuário | Motor | Existe hoje? |
|---|---------|--------------------|-------------------|------------------------|-------|--------------|
| A1 | Nota autorizada com **destinatário digitado à mão** (sem `Cliente` selecionado) | "Guardar {nome} nos destinatários?" | Formulário de `Cliente` preenchido com os dados da nota | Clica em **Salvar** | Regra | `EmitirNFe.razor` permite preencher o destinatário à mão; o cadastro não é oferecido |
| A2 | Nota autorizada com **item digitado à mão** (sem `Produto`) | "Guardar {n} produtos para reutilizar?" | Lista com checkboxes, formulários de `Produto` pré-preenchidos (NCM, CFOP, CST, unidade) | Clica em **Salvar selecionados** | Regra | Idem |
| A3 | Usuário começa a digitar um destinatário que **já existe** (mesmo CPF/CNPJ) | "Esse destinatário já está cadastrado. Usar os dados salvos?" | Seleciona o `Cliente` no autocomplete | Confirma | Regra | Não |
| A4 | Nota rejeitada por NCM/CST/CFOP do produto e **corrigida** na retransmissão | "Atualizar o cadastro do produto com o NCM corrigido, para não repetir?" | Formulário de `Produto` com o valor novo | Clica em **Salvar** | Regra | Não |
| A5 | Abrir **Emitir NF-e** para um destinatário com histórico | "Emitir igual à última nota para {cliente}?" | Novo formulário com itens, CFOP, frete e observações da última nota (sem número, datas e chave) | Revisa e clica em **Emitir** | Regra | Não existe "duplicar nota" — **ganho rápido** |
| A6 | **Padrão de preenchimento**: para o mesmo destinatário, UF e produto, a mesma combinação CFOP + CST + natureza em ≥ 80% das últimas 10 notas | Sugestão inline no campo: "Nas últimas 9 notas: 5102 · CSOSN 102" | Preenche ao aceitar (Tab ou clique) | Aceita campo a campo | SQL | Não |
| A7 | **Nota recorrente**: mesmo destinatário e itens com intervalo regular (ex.: todo mês ± 3 dias, ≥ 3 ocorrências) | "Essa nota se repete todo dia ~5. Quer um lembrete?" | Cria `LembreteEmissao` (periodicidade, dia, modelo da nota) | Confirma o lembrete; **no dia**, recebe o aviso e um **rascunho pronto** (`SituacaoNota.Rascunho`, que já existe) | SQL + regra | Enum `Rascunho` existe; recorrência não |
| A8 | Rascunho de lembrete com valores que costumam variar (quantidade, valor) | Destaca esses campos: "Confira: quantidade variou nos últimos meses" | Destaque visual | Ajusta e emite | SQL | Não |
| A9 | Empresa nova cadastrada | Checklist guiado: certificado → configuração inicial → nota de homologação → produção | Links diretos | Cada passo | Regra | Parcial (badge de configuração pendente) |
| A10 | NCM escolhido exige CEST | "Este NCM exige CEST. Sugestões: …" | Lista de CEST compatíveis | Escolhe | Tabela | Aviso existe no `NcmAutocomplete`; a sugestão não |
| A11 | Cancelamento feito perto do fim do prazo | Dica: "Cancelamentos fora do prazo exigem outro procedimento — veja o artigo" | Link do artigo | — | Regra | Não |
| A12 | Fim do mês com rascunhos parados | "Você tem 3 rascunhos de setembro não emitidos" | Lista | Emite ou descarta | SQL | Não |

---

## 2. Regras de UX das automações

1. **Momento certo:** a proposta aparece **depois** do sucesso da ação (nota autorizada), como balão ou cartão no
   próprio resultado, nunca bloqueando.
2. **Preenchimento visível:** campos preenchidos pela coruja ficam com fundo dourado claro e ícone 🦉 até serem
   editados ou confirmados. Um banner no topo resume: "A coruja preencheu 7 campos a partir da nota 1234.
   [Desfazer]".
3. **Desfazer sempre disponível** até o envio.
4. **Explicável:** toda sugestão diz de onde veio ("baseado nas suas últimas 9 notas para este cliente").
5. **Aprende com a recusa:** sugestão A6 recusada 3 vezes para a mesma combinação deixa de aparecer.
6. **Escopo:** padrões calculados **por empresa** (tenant), nunca entre empresas ou escritórios.

---

## 3. Modelo de dados novo

| Entidade | Campos principais | Observação |
|----------|-------------------|------------|
| `LembreteEmissao` | `EmpresaId`, `CriadoPorUsuarioId`, `NotaModeloId`, `Periodicidade` (Mensal, Quinzenal, Semanal), `Dia`, `ProximaEm`, `Ativo` | Worker diário cria o rascunho e notifica; **nunca transmite** |
| `SugestaoDispensada` | `UsuarioId`, `Chave` (ex.: `A6:cliente:produto:cfop`), `Vezes`, `SuspensaAte` | Controla a repetição |
| `PadraoPreenchimento` (view materializada ou query) | `EmpresaId`, `ClienteId`, `ProdutoId`, `UfDestino`, `Cfop`, `Cst`, `Frequencia` | Recalculada no worker noturno |

Commands novos: `PrepararNotaAPartirDeCommand(notaOrigemId)` (retorna DTO, **não persiste**),
`CriarLembreteEmissaoCommand`, `GerarRascunhosDoDiaCommand` (worker).
`EmitirNFe.razor` passa a aceitar `?origem={notaId}` e `?rascunho={notaId}` para abrir já preenchido.

---

## 4. Priorização (valor × esforço)

| Onda | Automações | Por quê |
|------|------------|---------|
| 1 (ganhos rápidos, sem IA) | A5 (emitir igual à última), A1, A2, A9 | Resolvem retrabalho diário; só UI e queries |
| 2 | A6, A3, A10, A4 | Precisam da query de padrões e das anotações de campo |
| 3 | A7, A8, A12 (recorrência e rascunhos) | Precisam de worker, entidade nova e notificação |
| 4 | A11 e outras dicas contextuais | Dependem da base de conhecimento madura |
