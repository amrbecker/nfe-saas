# Mascote — A Coruja do NFeFlow

> Especificação de UX do mascote. Objetivo: memória afetiva do Clippy **sem** os erros que fizeram o Clippy ser
> odiado.

---

## 1. Lições do Clippy (Office 97–2003) e do "Mago" (Office Assistant)

| O que o Clippy fazia | Por que irritava | Regra da coruja |
|----------------------|------------------|-----------------|
| Aparecia sozinho ("Parece que você está escrevendo uma carta…") | Interrompia o trabalho e roubava o foco | **Só fala quando chamada.** Proatividade apenas por balão de pensamento silencioso, sem roubar o foco |
| Adivinhava errado e insistia | Parecia burro | Mostra **hipóteses** ("Sua dúvida é sobre…?"), nunca afirma o que o usuário quer |
| Ocupava espaço em cima do documento | Cobria o conteúdo | Vive num espaço **que já é vazio** (rodapé do menu lateral) |
| Difícil de desligar de vez | Usuário se sentia refém | "Silenciar dicas" com um clique, por usuário, persistente; a coruja continua disponível se chamada |
| Não aprendia | Repetia a mesma dica | Dica dispensada não volta; dica ignorada 3 vezes some por 30 dias |
| Genérico | Não sabia do documento | Conhece a tela, a nota e a rejeição (`CAPTURA_CONTEXTO.md`) |

O que o Clippy tinha de bom e vamos manter: **personagem com expressão**, animações de espera que dão vida à tela e
a sensação de ter "alguém ali".

---

## 2. Personagem

- **Espécie:** coruja-buraqueira (*Athene cunicularia*). É brasileira, pequena, fica acordada de dia e vive no chão,
  "no seu canto" — combina com a ideia de morar no rodapé do menu. Olhos grandes e expressivos facilitam animar
  emoções com poucos traços.
- **Visual:** traço limpo e geométrico, coerente com o logo NFeFlow. Corpo em tons terrosos neutros; **detalhes em
  latão `#C08A2E`** (a cor `--mud-palette-tertiary` do tema): óculos redondos, caneta atrás da orelha, xícara.
  Funciona no tema claro e no escuro (contorno visível nos dois).
- **Personalidade:** colega experiente, calma, levemente espirituosa só nos estados de espera. Nas respostas
  fiscais, sóbria (`CONTEXTO_AGENTE.md`).
- **Nome** — decisão do PO. Opções:

| Nome | Leitura |
|------|---------|
| **Ori** | De "orientar"; curto, neutro, fácil de chamar ("Pergunte à Ori") |
| **Flora** | Coruja-buraqueira, brasileira, simpática |
| **Dona Coruja** | Afetivo, "a experiente do escritório" |
| **Nota** | Trocadilho com o produto; pode confundir na interface ("a Nota disse sobre a nota") |

---

## 3. Posicionamento

| Contexto | Onde fica | Tamanho |
|----------|-----------|---------|
| Desktop, menu aberto | **Rodapé do `MudDrawer`** (abaixo de "Certificado Digital"/"Administração"), "empoleirada" na borda. É espaço hoje vazio e nunca sobrepõe formulários | ~72×72 px |
| Desktop, menu mini (56 px) | Mesmo lugar, só a cabeça | 40×40 px |
| Tela ≤ 600 px | Botão flutuante no canto inferior direito, acima da área segura | 48×48 px |
| Painel aberto | `MudDrawer` à direita (`Anchor.End`), 380 px, sem cobrir o menu; a coruja "voa" do rodapé para o topo do painel | — |

**Atalho de teclado:** `Ctrl+Shift+Espaço` abre e fecha. O F1 fica com o navegador.
**Acessibilidade:** botão com `aria-label="Abrir assistente"`; balões anunciados em `aria-live="polite"`; foco volta
ao campo de origem ao fechar o painel.

---

## 4. Estados e animações

| Estado | Quando | Animação | Duração / loop |
|--------|--------|----------|----------------|
| **Tomando café** | Padrão ocioso | Leva a xícara ao bico a cada ~12 s, vapor sobe | Loop lento |
| **Cochilando** | Sem interação há 3 min | Olhos fechados, "Z" pequenos, respiração | Loop; acorda ao mover o mouse perto |
| **Atenta** | Usuário focou um campo fiscal (CFOP, CST, NCM…) | Vira a cabeça para o conteúdo, olhos abertos | Uma vez, depois parada |
| **Pensando** | Esperando o modelo responder | Olhos para cima, pena no queixo, reticências | Loop enquanto espera |
| **Falando** | Resposta chegando (streaming) | Bico leve, gestos com a asa | Enquanto chega o texto |
| **Preocupada** | Nota rejeitada ou erro da API | Sobrancelhas franzidas, penas arrepiadas | Uma vez + balão |
| **Comemorando** | 1ª nota autorizada da empresa, marcos (100ª nota) | Pulo curto, confete dourado | Uma vez; **nunca** em toda nota |
| **Dormindo profundo** | Cota diária esgotada | Pijama e touca | Estático; clicar mostra "volto amanhã, mas os artigos estão aqui" |
| **Silenciada** | Usuário desligou as dicas | Fones de ouvido | Estático |

**Tecnologia:**
- **Fase 1:** SVG em camadas + animações CSS (keyframes com `transform` e `opacity`). Leve (< 30 KB), usa as
  cores do tema via `var(--mud-palette-*)` e não adiciona dependência.
- **Fase 2 (opcional):** Rive ou Lottie, se um ilustrador entregar animações ricas. Avaliar o tamanho do pacote no
  WASM.
- `@media (prefers-reduced-motion: reduce)` → todos os estados viram pose estática.
- Animação pausa quando a aba está em segundo plano (`visibilitychange`).

---

## 5. Dicas silenciosas (balões de pensamento)

Um balão de pensamento (nuvem com bolinhas) sai da coruja **sem roubar o foco**, com texto de até 90
caracteres e duas ações: **"Mostrar"** e **"✕"**.

**Regras anti-intrusão:**
1. No máximo **1 balão a cada 10 minutos** e **3 por sessão**.
2. Nunca durante digitação ativa: esperar 4 s sem teclado.
3. Nunca por cima de diálogo modal nem durante o envio de uma nota.
4. Some sozinho em 12 s e fica como um ponto dourado na coruja até ser visto.
5. "✕" dispensa aquela dica para sempre. Ignorada 3 vezes → suspensa por 30 dias.
6. Prioridade: **bloqueio** (certificado vence em ≤ 7 dias, rejeição) > **economia de tempo** (automação) >
   **aprendizado** (dica de uso).
7. Métrica de alarme: se mais de 15% dos usuários ativos silenciarem as dicas, rever a frequência.

**Exemplos:**

| Gatilho | Balão |
|---------|-------|
| Digitou um destinatário novo à mão | "Quer guardar este destinatário para as próximas notas?" |
| 3ª nota igual para o mesmo cliente | "Essa nota se repete todo mês. Quer um lembrete?" |
| Foco no CFOP em operação interestadual | "Operação para outra UF: o CFOP começa com 6." |
| Certificado vence em 7 dias | "Seu certificado vence em 7 dias." |
| Nota rejeitada | "Sei o que foi. Quer ver?" |

---

## 6. Abertura do painel (fluxo)

```
Usuário clica na coruja
   └─▶ Coruja "acorda" (0,3 s) e voa para o painel
        └─▶ Mostra "Estou vendo que você está em Emitir NF-e › Produtos, no campo CFOP."  [editar ✎]
             └─▶ Até 3 hipóteses (sem chamar o modelo):
                  • "Qual CFOP usar nesta venda?"
                  • "Por que a nota anterior foi rejeitada?"
                  • "Outra dúvida…" (campo livre)
                  ├─ hipótese com artigo pronto → mostra o resumo do artigo (custo zero) + "Explicar melhor"
                  └─ texto livre / "Explicar melhor" → chama o modelo com o contexto
```

A linha "Estou vendo que…" é a **transparência** da captura: o usuário vê o que a coruja sabe e pode remover itens
antes de perguntar.
