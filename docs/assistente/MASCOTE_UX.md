# Mascote — Ori, a Coruja do NFeFlow

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
- **Nome: Ori** (decidido em 2026-09-23), de "orientar". Tratamento no feminino: "Pergunte à Ori". Opções que foram avaliadas:

| Nome | Leitura |
|------|---------|
| **Ori** ✅ | De "orientar"; curto, neutro, fácil de chamar ("Pergunte à Ori") |
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

---

## 7. Produção do desenho com IA (MCPs pesquisados em 2026-09-23)

### 7.1 Ferramentas encontradas

| Ferramenta | Tipo | O que faz de útil para a Ori | Limitações e custo | Fonte |
|------------|------|------------------------------|--------------------|-------|
| **Recraft MCP** (oficial, remoto: `https://mcp.recraft.ai/mcp`) — *plano pago; o grátis não permite uso comercial (§7.1b)* | Geração de imagem **vetorial** | Modelo V4 Vector gera **SVG nativo**; controle de paleta (dá para fixar `#C08A2E`); **estilo personalizado a partir de imagens de referência**, o que ajuda a manter a mesma coruja em todas as poses; vetorização de raster; remoção de fundo | Pago (créditos da assinatura no servidor remoto ou unidades de API no local). O pacote local antigo foi arquivado em jul/2026 | [Recraft MCP (LobeHub)](https://lobehub.com/mcp/recraft-ai-mcp-recraft-server), [Feluda](https://feluda.ai/mcp-servers/recraft) |
| **svgapp MCP** (remoto: `https://web.svgapp.ai/mcp`) | Gerador de **mascotes** SVG | Feito para esse caso de uso: um personagem, **poses por estado** (onboarding, erro, vazio, sucesso), fundo transparente, legível em 64 px, **animação** (`create_mascot_animation`). Mantém a consistência do personagem entre sessões via `svgapp.conf.json` | Serviço menor e mais novo: **verificar termos de licença comercial** e preços antes de adotar | [svgapp — mascote no Claude Code](https://svgapp.ai/blog/claude-code-mascot-svg/) |
| **Lottie Creator MCP** (oficial LottieFiles) | **Animação** | Cria e edita animações Lottie (formas, keyframes, easing, exportação) por linguagem natural — serve para as animações ricas da Fase 5 | Exige conta no LottieFiles Creator; Lottie no Blazor precisa do `lottie-web` (JS) | [Lottie Creator MCP](https://docs.lottiefiles.com/en/creator/13_ai-tools/lottie-creator-mcp), [LottieFiles MCP](https://lottiefiles.com/mcp) |
| **Figma MCP** (oficial, escrita no canvas desde 2026) | Organização e acabamento | Levar o SVG para o Figma com uma camada por parte (olhos, asas, xícara) e um frame por estado — handoff limpo para o CSS | Escrita no canvas exige assento **Full ou Dev em plano pago** | [Figma — agentes no canvas](https://www.figma.com/blog/the-figma-canvas-is-now-open-to-agents/), [write to canvas](https://developers.figma.com/docs/figma-mcp-server/write-to-canvas) |
| **Canva MCP** (oficial, `mcp.canva.com/mcp`) | Peças de marketing | Aplicar a Ori já pronta em materiais (posts, apresentação comercial, e-mail de onboarding) com o brand kit | Não serve para criar o personagem em si | [Canva MCP](https://www.canva.dev/docs/apps/mcp/) |
| Replicate / fal / ImagineArt / geradores de imagem genéricos | Raster | Exploração rápida de conceito (moodboard) | Saída em **raster**: precisa vetorizar depois | [ImagineArt](https://www.imagine.art/blogs/best-mcp-servers-claude-code-image-prompting), [replicate-flux-mcp](https://github.com/awkoy/replicate-flux-mcp) |
| **Sem MCP:** SVG + CSS escritos à mão pelo Claude Code, com prévia publicada como Artifact | Protótipo | Custo zero, cores do tema, animações CSS dos 9 estados; ótimo para validar postura, tamanho e posição **antes** de pagar ferramenta | Traço mais geométrico e simples que o de um ilustrador | — |

### 7.1b Opções gratuitas e o que a comunidade usa (pesquisa de 2026-09-23)

| Opção | Custo | Uso na comunidade | Licença da imagem gerada | Veredito para a Ori |
|-------|-------|-------------------|--------------------------|---------------------|
| **SVG + CSS escritos pelo Claude Code** | Grátis | Muito comum para ícones e mascotes geométricos; sai direto em vetor, com camadas nomeadas e animável | Sua (autoria humana + ferramenta) | ✅ **Protótipo e possivelmente a versão 1** — traço limpo combina com o logo NFeFlow |
| **ComfyUI local + MCP** (oficial `Comfy MCP`, lançado em ago/2026, e versões da comunidade) | Grátis (roda no seu Mac M5 com 32 GB) | É a opção mais citada pela comunidade para geração local ligada ao Claude Code | Depende do modelo: **FLUX.1 [schnell] e Qwen-Image são Apache-2.0** (uso comercial livre) | ✅ **Arte final gratuita**: gerar a coruja em raster, depois vetorizar |
| **Hugging Face MCP** (oficial, Spaces com ZeroGPU: FLUX.1 Krea, Qwen-Image) | Créditos grátis da conta HF (limitados) | Divulgado no blog oficial do HF; bom para testar conceitos sem instalar nada | Conforme o modelo — prefira **Qwen-Image** (Apache-2.0) | ✅ Moodboard e exploração rápida |
| **Pollinations MCP** (sem chave) | Grátis | Popular em listas de "MCP sem API key" | Verificar os termos | ⚠️ Anônimo limitado a ~1 imagem por hora; serve só para testar |
| **VTracer** (open source; `pip install vtracer`) | Grátis | Padrão da comunidade para converter PNG em SVG colorido (o Potrace só faz preto e branco) | Ferramenta; não afeta a licença | ✅ Etapa obrigatória depois da geração raster |
| **Inkscape** (open source) | Grátis | Acabamento manual de vetores | — | ✅ Limpeza e separação em camadas |
| **Recraft — plano grátis** | Grátis (50 créditos/dia) | — | ❌ **Imagem pública, de propriedade do Recraft, sem uso comercial** | ❌ Não serve para marca |
| Recraft / svgapp pagos | Pago | — | Comercial no plano pago | Só se as opções grátis não chegarem ao nível desejado |

**Cuidado com a licença do modelo:** modelos com licença "dev" ou não comercial (ex.: FLUX.1 [dev], FLUX.1 Krea [dev])
têm regras próprias para as imagens geradas. Para uma marca, **prefira modelos Apache-2.0** (FLUX.1 [schnell],
Qwen-Image) e registre qual modelo gerou a versão final.

### 7.2 Pipeline recomendado

```
Caminho 100% gratuito (recomendado)
1. Protótipo            Claude Code escreve SVG + CSS da Ori em 9 estados → Artifact para o PO avaliar.
                        Se o traço geométrico agradar, ele já pode ser a versão 1 do produto.
2. Conceito ilustrado   Hugging Face MCP (Qwen-Image) para explorar variações; depois ComfyUI local no Mac M5
   (opcional)           (FLUX.1 [schnell] ou Qwen-Image, Apache-2.0) com prompt e seed fixos para as 9 poses.
3. Vetorização          VTracer (PNG → SVG colorido), com paleta reduzida (6–8 cores, incluindo #C08A2E).
4. Acabamento humano    Inkscape: limpar nós, separar camadas nomeadas (olho-esq, asa-dir, xicara…), < 30 KB.
5. Animação             CSS sobre as camadas (Fase 1). Lottie Creator MCP só se for necessário depois.
6. Marketing            Canva (plano grátis) com a Ori aprovada.

Caminho pago (se o gratuito não atingir o nível desejado): Recraft ou svgapp nos passos 2–3.
```

Instalação (caminho gratuito):

```bash
pip install vtracer                                     # vetorização
brew install --cask inkscape                            # acabamento
# ComfyUI local: instalar ComfyUI (app desktop para macOS), baixar FLUX.1 [schnell] ou Qwen-Image
# e registrar o servidor MCP oficial (Comfy MCP) ou um da comunidade no Claude Code (`claude mcp add ...`).
# Hugging Face: conta grátis → huggingface.co/settings/mcp → adicionar o Space mcp-tools/qwen-image.
```

Caminho pago, se necessário:

```bash
claude mcp add --transport http recraft https://mcp.recraft.ai/mcp
claude mcp add --transport http svgapp https://web.svgapp.ai/mcp --header "Authorization: Bearer $SVGAPP_MCP_TOKEN"
```

### 7.3 Cuidados de propriedade intelectual

- **Licença comercial:** confirmar nos termos de cada serviço que a imagem gerada pode ser usada comercialmente e
  como marca.
- **Proteção da marca:** imagens geradas só por IA têm proteção autoral incerta. O **acabamento humano** do passo 4
  reforça a autoria e a originalidade. Considerar o **registro da Ori como marca figurativa no INPI** junto com o
  NFeFlow.
- **Originalidade:** não usar como referência de estilo corujas de outras marcas (ex.: mascotes de apps de idiomas).
  Descrever a Ori pelos atributos da §2.
- Guardar prompts, versões e arquivos-fonte em `docs/assistente/mascote/` para comprovar a criação.

