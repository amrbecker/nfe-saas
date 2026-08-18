# Setup NfeSaas — macOS Apple Silicon (M-series)

> Guia completo para rodar o projeto em um MacBook Pro com chip M1/M2/M3/M4 do zero.

---

## Sumário

1. [Aviso: arquivos que precisam de transferência manual](#1-aviso-arquivos-que-precisam-de-transferência-manual)
2. [Pré-requisitos do sistema via Homebrew](#2-pré-requisitos-do-sistema-via-homebrew)
3. [Configuração do terminal](#3-configuração-do-terminal)
4. [GitHub via SSH](#4-github-via-ssh)
5. [Clonar o repositório](#5-clonar-o-repositório)
6. [Setup do projeto](#6-setup-do-projeto)
7. [Como rodar o projeto](#7-como-rodar-o-projeto)
8. [Executar os testes](#8-executar-os-testes)
9. [IDE recomendada e extensões](#9-ide-recomendada-e-extensões)
10. [URLs e comandos do dia a dia](#10-urls-e-comandos-do-dia-a-dia)
11. [Gotchas específicos do Mac / Apple Silicon](#11-gotchas-específicos-do-mac--apple-silicon)
12. [Ferramentas opcionais](#12-ferramentas-opcionais)
13. [Resumo rápido — copy & paste](#13-resumo-rápido--copy--paste)

---

## 1. Aviso: arquivos que precisam de transferência manual

Estes arquivos **não estão no git** e precisam ser copiados manualmente do Windows antes de desligar a máquina:

| Arquivo / dado | Localização no Windows | O que fazer no Mac |
|---|---|---|
| **`.env`** | `C:\dev\nfe\nfe-saas\.env` | Copiar para `~/dev/nfe-saas/.env` (ou recriar com `openssl rand`) |
| **Volume `dp_keys`** | Volume Docker `nfesaas_dp_keys` | Exportar se precisar manter certificados já cifrados (ver abaixo) |

### Sobre o `.env`

Contém `POSTGRES_PASSWORD` e `JWT_SECRET` com valores reais gerados. Se você **não** precisa migrar dados do banco, pode simplesmente recriar com novos valores fortes no Mac:

```bash
cp .env.example .env
# Gere novos secrets:
openssl rand -base64 48   # use como JWT_SECRET
openssl rand -base64 24   # use como POSTGRES_PASSWORD
```

### Sobre o volume `dp_keys`

Este volume contém as chaves ASP.NET Data Protection que cifram `CertificadoSenha` e `CscToken` no banco. Se for migrar o banco de dados junto com a aplicação (não apenas um ambiente de dev limpo), exporte o volume:

```powershell
# No Windows, antes de migrar:
docker run --rm -v nfesaas_dp_keys:/data -v ${PWD}:/backup alpine `
  tar czf /backup/dp_keys_backup.tar.gz -C /data .
```

```bash
# No Mac, após clonar e antes de subir pela primeira vez:
docker volume create nfesaas_dp_keys
docker run --rm -v nfesaas_dp_keys:/data -v $(pwd):/backup alpine \
  tar xzf /backup/dp_keys_backup.tar.gz -C /data
```

Se estiver criando um ambiente de dev limpo (sem migrar banco), não é necessário — um novo volume é criado automaticamente pelo `docker compose`.

**Não há .NET User Secrets** (`dotnet user-secrets`) em uso neste projeto — toda configuração vai pelo `.env`.

---

## 2. Pré-requisitos do sistema via Homebrew

### 2.1 Xcode Command Line Tools

```bash
xcode-select --install
```

### 2.2 Homebrew

```bash
/bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"
```

Após instalar, adicione o Homebrew ao PATH (Apple Silicon usa `/opt/homebrew`):

```bash
echo 'eval "$(/opt/homebrew/bin/brew shellenv)"' >> ~/.zshrc
source ~/.zshrc
```

### 2.3 .NET 8 SDK

O cask `dotnet-sdk` instala sempre a versão mais recente (hoje, .NET 10+), não o 8.x que o projeto usa. Instale a formula versionada:

```bash
brew install dotnet@8
echo 'export DOTNET_ROOT="/opt/homebrew/opt/dotnet@8/libexec"' >> ~/.zshrc
echo 'export PATH="/opt/homebrew/opt/dotnet@8/bin:$PATH"' >> ~/.zshrc
source ~/.zshrc
```

Verifique:

```bash
dotnet --version   # deve retornar 8.x.x
```

### 2.4 Docker Desktop (Apple Silicon)

Baixe a versão **Apple Silicon** em <https://www.docker.com/products/docker-desktop/> e instale normalmente.

Após abrir o Docker Desktop, verifique no terminal:

```bash
docker --version
docker compose version
```

### 2.5 Git

O macOS já inclui o Git via Xcode CLT. Verifique:

```bash
git --version
```

### 2.6 Node.js via fnm (para Claude Code CLI)

Instale o `fnm` (Fast Node Manager):

```bash
brew install fnm
echo 'eval "$(fnm env --use-on-cd)"' >> ~/.zshrc
source ~/.zshrc
```

Instale e use o Node LTS:

```bash
fnm install --lts
fnm use lts-latest
node --version
npm --version
```

### 2.7 Claude Code CLI

```bash
npm install -g @anthropic-ai/claude-code
claude --version
```

---

## 3. Configuração do terminal

### 3.1 iTerm2

```bash
brew install --cask iterm2
```

Abra o iTerm2 e configure (opcional mas recomendado):
- Preferences → Profiles → Colors → Color Presets → Solarized Dark
- Preferences → Profiles → Text → Font → JetBrains Mono ou Fira Code, tamanho 13

### 3.2 Oh My Zsh

```bash
sh -c "$(curl -fsSL https://raw.githubusercontent.com/ohmyzsh/ohmyzsh/master/tools/install.sh)"
```

Plugins úteis — edite `~/.zshrc`:

```bash
plugins=(git docker dotnet fnm zsh-autosuggestions zsh-syntax-highlighting)
```

Instale os plugins de terceiros:

```bash
git clone https://github.com/zsh-users/zsh-autosuggestions \
  ${ZSH_CUSTOM:-~/.oh-my-zsh/custom}/plugins/zsh-autosuggestions
git clone https://github.com/zsh-users/zsh-syntax-highlighting \
  ${ZSH_CUSTOM:-~/.oh-my-zsh/custom}/plugins/zsh-syntax-highlighting
source ~/.zshrc
```

### 3.3 fnm — integração final no .zshrc

Confirme que seu `~/.zshrc` contém estas linhas (na ordem):

```bash
eval "$(/opt/homebrew/bin/brew shellenv)"
eval "$(fnm env --use-on-cd)"
```

---

## 4. GitHub via SSH

### 4.1 Gerar chave ed25519

```bash
ssh-keygen -t ed25519 -C "ribeirobecker@gmail.com" -f ~/.ssh/id_ed25519_github
```

### 4.2 Adicionar ao ssh-agent

```bash
eval "$(ssh-agent -s)"
ssh-add --apple-use-keychain ~/.ssh/id_ed25519_github
```

Configure o `~/.ssh/config` para persistir entre reinicializações:

```
Host github.com
  AddKeysToAgent yes
  UseKeychain yes
  IdentityFile ~/.ssh/id_ed25519_github
```

### 4.3 Adicionar a chave pública ao GitHub

```bash
pbcopy < ~/.ssh/id_ed25519_github.pub
# Cole em: GitHub → Settings → SSH and GPG keys → New SSH key
```

### 4.4 Testar a conexão

```bash
ssh -T git@github.com
# Hi amrbecker! You've successfully authenticated...
```

---

## 5. Clonar o repositório

```bash
mkdir -p ~/dev
cd ~/dev
git clone git@github.com:amrbecker/nfe-saas.git
cd nfe-saas
```

> O remote já é SSH. Se por algum motivo aparecer HTTPS, troque:
> ```bash
> git remote set-url origin git@github.com:amrbecker/nfe-saas.git
> ```

---

## 6. Setup do projeto

### 6.1 Criar o `.env`

```bash
cp .env.example .env
```

Edite o `.env` com valores reais:

```bash
nano .env    # ou code .env / rider .env
```

Gere secrets fortes:

```bash
openssl rand -base64 48   # → JWT_SECRET (mín. 32 chars)
openssl rand -base64 24   # → POSTGRES_PASSWORD
```

O arquivo final deve parecer com:

```dotenv
POSTGRES_DB=nfesaas
POSTGRES_USER=nfesaas
POSTGRES_PASSWORD=<senha gerada acima>

JWT_SECRET=<chave gerada acima>
JWT_ISSUER=NfeSaas
JWT_AUDIENCE=NfeSaas.WebUI

ASPNETCORE_ENVIRONMENT=Production
WEBUI_BASE_URL=http://localhost:5002

DataProtection__KeysPath=/app/dpkeys
Ncm__UpdateSourceUrl=
Ncm__LocalFilePath=
Ncm__UpdateIntervalDays=7
Ncm__UpdateOnStartup=false
```

### 6.2 Instalar a ferramenta global `dotnet-ef`

Necessária para gerar scripts de migration fora dos containers:

```bash
dotnet tool install --global dotnet-ef
```

Verifique:

```bash
dotnet ef --version
```

Se `dotnet ef` não for encontrado após instalar, adicione o caminho ao PATH:

```bash
echo 'export PATH="$PATH:$HOME/.dotnet/tools"' >> ~/.zshrc
source ~/.zshrc
```

### 6.3 Instalar workload Blazor WASM (opcional para build local)

O `docker compose` já faz isso dentro do container. Instale localmente só se quiser `dotnet build` sem Docker:

```bash
dotnet workload install wasm-tools
```

### 6.4 Restaurar dependências NuGet (opcional)

```bash
dotnet restore NfeSaas.sln
```

### 6.5 Certificados de desenvolvimento HTTPS

Somente se quiser rodar a API/WebUI fora do Docker:

```bash
dotnet dev-certs https --trust
```

---

## 7. Como rodar o projeto

### 7.1 Via script `restart.sh`

```bash
# Inicialização padrão (build + migrations + seed + abre solution)
./restart.sh

# Resetar banco do zero
./restart.sh --clean

# Restart rápido (sem rebuild de imagens, sem migrations)
./restart.sh --no-build --skip-migrations --no-ide

# Rodar testes após subir
./restart.sh --test
```

> **Nota:** A flag `--no-ide` é útil se você não tiver Rider nem VS Code instalados — o script tenta abrir a solution nesses IDEs (nessa ordem) e avisa se nenhum for encontrado.

### 7.2 Via Docker Compose direto

```bash
# Subir tudo (build + start)
docker compose up -d --build

# Ver status
docker compose ps

# Logs em tempo real
docker compose logs -f api
docker compose logs -f webui

# Parar sem apagar volumes
docker compose down

# Parar e apagar banco (reset total)
docker compose down -v
```

### 7.3 Aplicar migrations manualmente (sem o restart.sh)

```bash
# 1. Gerar script SQL idempotente
dotnet ef migrations script --idempotent \
  -o migration.sql \
  --project src/Infrastructure \
  --startup-project src/API

# 2. Aguardar postgres ficar saudável
docker compose up -d postgres
until docker inspect nfesaas_postgres --format '{{.State.Health.Status}}' 2>/dev/null | grep -q healthy; do
  sleep 2
done

# 3. Copiar e aplicar
docker cp migration.sql nfesaas_postgres:/tmp/migration.sql
docker exec nfesaas_postgres psql -U nfesaas -d nfesaas -f /tmp/migration.sql

# 4. Limpar
rm migration.sql
```

---

## 8. Executar os testes

Os testes de integração e BDD usam **Testcontainers** — eles sobem o PostgreSQL automaticamente, sem precisar do `docker compose`.

```bash
# Unitários
dotnet test tests/NfeSaas.Tests.Unit

# Integração (precisa do Docker daemon rodando)
dotnet test tests/NfeSaas.Tests.Integration

# BDD (SpecFlow)
dotnet test tests/NfeSaas.Tests.BDD

# Todos
dotnet test

# Com verbosidade e sem rebuild
dotnet test --nologo --logger "console;verbosity=minimal"
```

---

## 9. IDE recomendada e extensões

### JetBrains Rider (recomendado)

O Rider tem suporte nativo ao .NET 8, Blazor, EF Core e Docker. Tem licença gratuita para projetos não-comerciais.

```bash
brew install --cask rider
```

Extensões úteis dentro do Rider:
- **Heap Allocations Viewer** — detecta alocações desnecessárias
- **.env files support** — syntax highlight para `.env`
- **Docker** — integrado nativamente

Para abrir o projeto:

```bash
rider NfeSaas.sln
```

O `restart.sh` também tenta abrir o Rider (ou VS Code, se o Rider não for encontrado) automaticamente ao final, a menos que rodado com `--no-ide`.

### VS Code (alternativa)

```bash
brew install --cask visual-studio-code
```

Extensões recomendadas (`code --install-extension <id>`):

```bash
code --install-extension ms-dotnettools.csharp           # C# Dev Kit
code --install-extension ms-dotnettools.csdevkit          # C# Dev Kit (novo)
code --install-extension ms-azuretools.vscode-docker      # Docker
code --install-extension mikestead.dotenv                 # .env support
code --install-extension formulahendry.dotnet-test-explorer # Test Explorer
code --install-extension esbenp.prettier-vscode           # Prettier
code --install-extension ms-dotnettools.blazorwasm-companion # Blazor WASM debug
```

Para abrir o projeto:

```bash
cd ~/dev/nfe-saas
code .
```

---

## 10. URLs e comandos do dia a dia

### URLs locais

| Serviço | URL |
|---|---|
| API | http://localhost:5001 |
| Swagger | http://localhost:5001/swagger |
| Health check | http://localhost:5001/health |
| WebUI (Blazor) | http://localhost:5002 |

### Credenciais de demo (seed)

| Campo | Valor |
|---|---|
| Usuário | `admin@nfesaas.com.br` |
| Senha | `Admin@123` |
| Empresa CNPJ | `00.000.000/0001-91` |
| Escritório CNPJ | `99.999.999/0001-91` |

### Comandos frequentes

```bash
# Subir ambiente completo
./restart.sh

# Restart rápido (sem rebuild)
./restart.sh --no-build --skip-migrations --no-ide

# Zerar banco e recriar
./restart.sh --clean

# Adicionar migration EF Core
dotnet ef migrations add NomeDaMigration \
  --project src/Infrastructure \
  --startup-project src/API

# Ver logs da API
docker compose logs -f api

# Acessar banco via psql
docker exec -it nfesaas_postgres psql -U nfesaas -d nfesaas

# Rodar todos os testes
dotnet test

# Rebuild de uma imagem específica
docker compose build api
docker compose up -d api

# Ver saúde dos containers
docker compose ps
```

---

## 11. Gotchas específicos do Mac / Apple Silicon

| # | Situação | Solução |
|---|---|---|
| 1 | **`brew` não encontrado** após instalar | O Homebrew no Apple Silicon fica em `/opt/homebrew/`. Adicione `eval "$(/opt/homebrew/bin/brew shellenv)"` ao `~/.zshrc`. |
| 2 | **`dotnet` não encontrado** (formula `dotnet@8`) | `dotnet@8` é keg-only (não symlinkado em `/opt/homebrew/bin`). Confirme `export PATH="/opt/homebrew/opt/dotnet@8/bin:$PATH"` no `~/.zshrc`. |
| 3 | **`dotnet --version` mostra 9.x/10.x em vez de 8.x** | Você tem outro SDK (ex. cask `dotnet-sdk`) na frente no PATH. Garanta que a linha do `dotnet@8` vem **antes** de qualquer outra entrada de dotnet no `~/.zshrc`. |
| 4 | **`dotnet ef` não encontrado** após `dotnet tool install` | Adicione `export PATH="$PATH:$HOME/.dotnet/tools"` ao `~/.zshrc`. Instale com `dotnet tool install --global dotnet-ef --version 8.0.0` para casar com o EF Core 8 do projeto (sem `--version`, o comando busca a última versão, hoje 10.x, incompatível). |
| 5 | **`restart.sh` não abre o Rider automaticamente** | Instale o Rider (`brew install --cask rider`) — o script tenta `rider`/`open -a Rider` e cai para VS Code se não encontrar. Ou use `--no-ide` e abra manualmente. |
| 6 | **Docker: `platform linux/amd64`** | Não é necessário — todas as imagens deste projeto (postgres:16-alpine, dotnet/sdk:8.0, nginx:alpine) têm suporte nativo a `linux/arm64`. Não adicione `platform:` forçando amd64. |
| 7 | **`SkiaSharp` / DANFE PDF falhando nos testes** | No Linux (Docker) já está configurado via `SkiaSharp.NativeAssets.Linux`. Localmente no macOS o runtime SkiaSharp para ARM já vem com o pacote principal. |
| 8 | **Testcontainers não encontra Docker socket** | Se você instalou Docker via Colima em vez do Docker Desktop, exporte: `export DOCKER_HOST=unix:///var/run/docker.sock`. |
| 9 | **`.env` e `.DS_Store`** | O `.gitignore` já ignora `.env`, `.env.*` e `.DS_Store`. Nada vaza acidentalmente. |
| 10 | **`dp_keys` volume apagado com `docker compose down -v`** | `-v` apaga **todos** os volumes, incluindo o `dp_keys`. Isso invalida `CertificadoSenha` e `CscToken` já cifrados no banco. Só use `-v` se souber o que está fazendo. Prefira `docker compose down` (sem `-v`) para parar sem perder dados. |
| 11 | **Arquivo `migration.sql` gerado na raiz** | Já está no `.gitignore` (`migration*.sql`). Não o commite. |
| 12 | **`~/.zshrc` não se aplica em shells não-interativos** | Ferramentas de automação (scripts, agentes) que abrem um shell não-interativo podem não carregar `~/.zshrc`. Se `dotnet`/`dotnet ef` "sumirem" nesse contexto, exporte o PATH explicitamente no próprio comando. |

---

## 12. Ferramentas opcionais

### GUI para PostgreSQL

```bash
brew install --cask tableplus      # recomendado — interface limpa, suporte a múltiplos DBs
# ou
brew install --cask dbeaver-community  # gratuito e open source
```

Configuração TablePlus para o banco local:

- Host: `127.0.0.1`
- Port: `5432`
- Database: `nfesaas`
- User: `nfesaas`
- Password: `<POSTGRES_PASSWORD do seu .env>`

### Insomnia / Bruno (teste de API REST)

```bash
brew install --cask insomnia    # alternativa ao Postman
# ou
brew install --cask bruno       # open source, configs versionáveis
```

A coleção de rotas pode ser exportada do Swagger: `http://localhost:5001/swagger/v1/swagger.json`

### Proxyman (debug HTTP)

Útil para interceptar chamadas entre o Blazor WASM e a API:

```bash
brew install --cask proxyman
```

### GitHub CLI

```bash
brew install gh
gh auth login   # autenticar via browser
```

### jq (processar JSON no terminal)

```bash
brew install jq
# Exemplo: curl -s http://localhost:5001/health | jq .
```

---

## 13. Resumo rápido — copy & paste

Execute em sequência num terminal novo do Mac:

```bash
# === 1. Homebrew ===
/bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"
echo 'eval "$(/opt/homebrew/bin/brew shellenv)"' >> ~/.zshrc && source ~/.zshrc

# === 2. Dependências do sistema ===
brew install --cask iterm2
brew install dotnet@8 fnm gh jq
echo 'export DOTNET_ROOT="/opt/homebrew/opt/dotnet@8/libexec"' >> ~/.zshrc
echo 'export PATH="/opt/homebrew/opt/dotnet@8/bin:$PATH"' >> ~/.zshrc
source ~/.zshrc

# === 3. Oh My Zsh ===
sh -c "$(curl -fsSL https://raw.githubusercontent.com/ohmyzsh/ohmyzsh/master/tools/install.sh)"
echo 'eval "$(fnm env --use-on-cd)"' >> ~/.zshrc && source ~/.zshrc

# === 4. Node.js + Claude Code ===
fnm install --lts && fnm use lts-latest
npm install -g @anthropic-ai/claude-code

# === 5. Docker Desktop ===
# Baixe manualmente em: https://www.docker.com/products/docker-desktop/
# (versão Apple Silicon) e instale normalmente.

# === 6. GitHub SSH ===
ssh-keygen -t ed25519 -C "ribeirobecker@gmail.com" -f ~/.ssh/id_ed25519_github
eval "$(ssh-agent -s)" && ssh-add --apple-use-keychain ~/.ssh/id_ed25519_github
cat >> ~/.ssh/config << 'EOF'
Host github.com
  AddKeysToAgent yes
  UseKeychain yes
  IdentityFile ~/.ssh/id_ed25519_github
EOF
pbcopy < ~/.ssh/id_ed25519_github.pub
echo "Cole a chave no GitHub: https://github.com/settings/keys"
# (abra o link, adicione a chave e depois continue)
ssh -T git@github.com

# === 7. Clonar repositório ===
mkdir -p ~/dev && cd ~/dev
git clone git@github.com:amrbecker/nfe-saas.git
cd nfe-saas

# === 8. Configurar .env ===
cp .env.example .env
echo ""
echo "Edite o .env com os secrets gerados abaixo:"
echo "JWT_SECRET:        $(openssl rand -base64 48)"
echo "POSTGRES_PASSWORD: $(openssl rand -base64 24)"
echo ""
echo "Abra o arquivo: nano .env"

# === 9. Ferramentas .NET globais ===
dotnet tool install --global dotnet-ef --version 8.0.0
echo 'export PATH="$PATH:$HOME/.dotnet/tools"' >> ~/.zshrc && source ~/.zshrc
dotnet ef --version

# === 10. Subir o projeto ===
# Certifique-se que o Docker Desktop está rodando, depois:
chmod +x restart.sh
./restart.sh

# Ou manualmente:
# docker compose up -d --build

# === 11. Verificar ===
echo "Aguardando API..."
until curl -sf http://localhost:5001/health > /dev/null; do sleep 3; done
echo "API OK: http://localhost:5001"
echo "Swagger: http://localhost:5001/swagger"
echo "WebUI:   http://localhost:5002"
echo "Login demo: admin@nfesaas.com.br / Admin@123"
```

---

*Gerado em 2026-06-30, atualizado em 2026-08-18 (fluxo de execução migrado de `restart.ps1`/PowerShell para `restart.sh`/bash — nativo para macOS). Stack: .NET 8 · Blazor WASM · PostgreSQL 16 · Docker Compose.*
