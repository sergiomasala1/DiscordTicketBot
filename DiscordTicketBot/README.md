# 🎫 DiscordTicketBot — Controle de Tempo de Chamados

Bot Discord em **C# (.NET 8)** para que técnicos de suporte controlem manualmente o tempo gasto em chamados, sem integração com sistemas externos.

---

## ✨ Funcionalidades

| Comando / Ação | Descrição |
|---|---|
| `/start` | Abre modal para informar número e descrição do chamado, inicia timer automaticamente |
| `/status` | Exibe chamado ativo, tempo atual e chamados pausados |
| Botão ⏸️ Pausar | Pausa o timer e registra o horário da pausa |
| Botão ▶️ Retomar | Retoma o timer e acumula o tempo pausado |
| Botão ✅ Finalizar | Encerra o chamado e exibe resumo com tempo líquido |

### Recursos técnicos
- ✅ Timer persiste após reiniciar o bot (dados salvos no SQLite)
- ✅ Cálculo de **tempo líquido** (descontando pausas)
- ✅ Múltiplas pausas e retomadas por chamado
- ✅ Embeds modernos com atualização em tempo real
- ✅ Configuração via `appsettings.json` ou variáveis de ambiente

---

## 🗂️ Estrutura do Projeto

```
DiscordTicketBot/
├── Commands/
│   ├── SlashCommandHandler.cs   # /start e /status
│   ├── ModalHandler.cs          # Processamento do formulário /start
│   └── ButtonHandler.cs         # Ações: Pausar, Retomar, Finalizar
├── Services/
│   ├── BotService.cs            # Ciclo de vida do bot (IHostedService)
│   └── TicketService.cs         # Lógica de negócio dos chamados
├── Database/
│   ├── AppDbContext.cs          # DbContext do Entity Framework
│   └── DatabaseInitializer.cs  # Criação/migração automática do banco
├── Models/
│   ├── User.cs                  # Entidade usuário Discord
│   ├── Ticket.cs                # Entidade chamado (com cálculos de tempo)
│   └── Sessions.cs              # TimeSession e PauseSession
├── Components/
│   └── TicketComponents.cs      # Fábrica de botões Discord
├── Utils/
│   └── EmbedFactory.cs          # Fábrica de embeds Discord
├── Program.cs                   # Entry point + injeção de dependência
├── appsettings.json             # Configurações
└── Dockerfile                   # Container para hospedagem
```

---

## 🛠️ Pré-requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- Um [Bot Discord](https://discord.com/developers/applications) com as permissões corretas
- (Opcional) [Docker](https://www.docker.com/)

---

## ⚙️ Configuração do Bot Discord

### 1. Criar o Bot no Discord Developer Portal

1. Acesse [discord.com/developers/applications](https://discord.com/developers/applications)
2. Clique em **New Application** → dê um nome
3. Vá em **Bot** → clique em **Add Bot**
4. Em **Privileged Gateway Intents**, habilite:
   - ✅ **Server Members Intent**
   - ✅ **Message Content Intent** (se quiser commands por prefixo no futuro)
5. Copie o **Token** do bot (guarde com segurança!)
6. Em **OAuth2 → URL Generator**, selecione:
   - Scopes: `bot`, `applications.commands`
   - Bot Permissions: `Send Messages`, `Use Slash Commands`, `Embed Links`, `Read Message History`
7. Copie o URL gerado e convide o bot para seu servidor

---

## 🚀 Rodando Localmente

### 1. Clone o repositório

```bash
git clone https://github.com/seu-usuario/discord-ticket-bot.git
cd discord-ticket-bot
```

### 2. Configure o token do bot

**Opção A — appsettings.json** (apenas para desenvolvimento local):
```json
{
  "Discord": {
    "Token": "SEU_TOKEN_AQUI"
  }
}
```

**Opção B — variável de ambiente** (recomendado para produção):
```bash
# Linux/macOS
export DISCORD_TOKEN="SEU_TOKEN_AQUI"

# Windows PowerShell
$env:DISCORD_TOKEN = "SEU_TOKEN_AQUI"
```

### 3. Execute o bot

```bash
dotnet restore
dotnet run
```

O banco SQLite será criado automaticamente em `data/ticketbot.db`.

---

## 🐳 Rodando com Docker

```bash
# Build da imagem
docker build -t discord-ticket-bot .

# Executar com volume persistente para o banco
docker run -d \
  --name ticketbot \
  -e DISCORD_TOKEN="SEU_TOKEN_AQUI" \
  -v $(pwd)/data:/app/data \
  -v $(pwd)/logs:/app/logs \
  --restart unless-stopped \
  discord-ticket-bot
```

---

## ☁️ Hospedagem Gratuita — Opções Detalhadas

### 🚂 1. Railway (Recomendado — mais simples)

**Plano gratuito**: $5 de crédito mensal (suficiente para bot 24/7 com uso moderado)

**Passo a passo:**

1. Acesse [railway.app](https://railway.app) e faça login com GitHub
2. Clique em **New Project → Deploy from GitHub Repo**
3. Selecione seu repositório
4. Vá em **Variables** e adicione:
   - `DISCORD_TOKEN` = seu token
   - `Database__Path` = `/app/data/ticketbot.db`
5. Em **Settings**, configure o **Start Command**:
   ```
   dotnet DiscordTicketBot.dll
   ```
6. Adicione um **Volume** em `/app/data` para persistir o banco SQLite
7. O deploy acontece automaticamente a cada push no `main`

> 💡 **Dica**: Railway detecta automaticamente o Dockerfile se existir.

---

### 🎨 2. Render

**Plano gratuito**: Serviços ficam em sleep após 15 min de inatividade (ruim para bot 24/7).
Use o plano **Starter ($7/mês)** para uptime contínuo, ou use keep-alive.

**Passo a passo:**

1. Acesse [render.com](https://render.com) e faça login com GitHub
2. Clique em **New → Web Service** (ou **Background Worker** — melhor para bots)
3. Conecte seu repositório
4. Configure:
   - **Environment**: `Docker`
   - **Build Command**: (deixe vazio — usa o Dockerfile)
5. Em **Environment Variables** adicione `DISCORD_TOKEN`
6. Em **Disks**, adicione:
   - Mount Path: `/app/data`
   - Size: 1 GB
7. Clique em **Create Web Service**

> ⚠️ Para manter o serviço gratuito ativo 24/7, use um serviço como [UptimeRobot](https://uptimerobot.com) para pingar a URL a cada 14 minutos.

---

### 🌐 3. Koyeb

**Plano gratuito**: 1 instância sempre ligada, sem sleep automático.

**Passo a passo:**

1. Acesse [koyeb.com](https://www.koyeb.com) e crie uma conta
2. Clique em **Create App → GitHub**
3. Selecione seu repositório e branch `main`
4. Configure:
   - **Build type**: Dockerfile
   - **Run command**: `dotnet DiscordTicketBot.dll`
5. Em **Environment variables**:
   - `DISCORD_TOKEN` = seu token
   - `Database__Path` = `/app/data/ticketbot.db`
6. Em **Volumes**, crie um volume persistente em `/app/data`
7. Clique em **Deploy**

> ✅ O Koyeb oferece deploy automático via GitHub push.

---

### ☁️ 4. Oracle Cloud Free Tier (Mais robusto — 24/7 garantido)

**Plano gratuito**: 2 VMs gratuitas para sempre (AMD), ou 4 VMs ARM Ampere. Sem sleep.

**Passo a passo:**

1. Crie conta em [cloud.oracle.com](https://cloud.oracle.com) (requer cartão de crédito, mas não cobra)
2. Crie uma **Compute Instance**:
   - Shape: `VM.Standard.E2.1.Micro` (gratuito)
   - OS: Ubuntu 22.04
3. Abra as portas necessárias no Security Group (não precisa de porta, o bot só outbound)
4. Conecte via SSH:
   ```bash
   ssh ubuntu@IP_DA_VM
   ```
5. Instale o .NET 8:
   ```bash
   wget https://dot.net/v1/dotnet-install.sh
   chmod +x dotnet-install.sh
   ./dotnet-install.sh --version latest --runtime dotnet
   export PATH=$PATH:~/.dotnet
   echo 'export PATH=$PATH:~/.dotnet' >> ~/.bashrc
   ```
6. Clone e publique o projeto:
   ```bash
   git clone https://github.com/seu-usuario/discord-ticket-bot.git
   cd discord-ticket-bot
   dotnet publish -c Release -o /app/publish
   ```
7. Crie um serviço systemd para manter o bot sempre ativo:
   ```bash
   sudo nano /etc/systemd/system/ticketbot.service
   ```
   ```ini
   [Unit]
   Description=Discord Ticket Bot
   After=network.target

   [Service]
   Type=simple
   User=ubuntu
   WorkingDirectory=/app/publish
   ExecStart=/home/ubuntu/.dotnet/dotnet /app/publish/DiscordTicketBot.dll
   Restart=always
   RestartSec=10
   Environment=DISCORD_TOKEN=SEU_TOKEN_AQUI
   Environment=Database__Path=/app/data/ticketbot.db

   [Install]
   WantedBy=multi-user.target
   ```
   ```bash
   sudo systemctl daemon-reload
   sudo systemctl enable ticketbot
   sudo systemctl start ticketbot
   sudo systemctl status ticketbot
   ```

---

### 💻 5. VPS Gratuita — Alternativas

| Provedor | Plano Gratuito | Obs |
|---|---|---|
| **Fly.io** | 3 VMs tiny compartilhadas | Ótimo para Docker |
| **Hetzner** | Sem gratuito — €4,51/mês | Melhor custo-benefício pago |
| **DigitalOcean** | $200 de crédito por 60 dias (novo usuário) | Droplet $4/mês |
| **Google Cloud** | $300 de crédito por 90 dias | e2-micro gratuito para sempre |
| **AWS** | 12 meses free tier t2.micro | Após isso cobra |

---

## 🔄 Deploy Automático via GitHub

O arquivo `.github/workflows/deploy.yml` já inclui CI/CD que:
1. Faz build e verifica o código a cada push
2. Builda e publica a imagem Docker no Docker Hub
3. Serviços como Railway e Render detectam automaticamente a imagem atualizada

**Configure os secrets no GitHub** (Settings → Secrets → Actions):
- `DOCKERHUB_USERNAME` — seu usuário no Docker Hub
- `DOCKERHUB_TOKEN` — token de acesso do Docker Hub

---

## 🗃️ Banco de Dados

O banco SQLite é criado automaticamente pelo EF Core na primeira execução.

**Tabelas criadas:**

| Tabela | Descrição |
|---|---|
| `Users` | Usuários Discord registrados |
| `Tickets` | Chamados com status e tempos |
| `TimeSessions` | Sessões de trabalho ativo |
| `PauseSessions` | Sessões de pausa com início e fim |

**Backup do banco:**
```bash
# Copiar o banco para backup
cp data/ticketbot.db data/backup_$(date +%Y%m%d).db
```

---

## 📝 Variáveis de Ambiente

| Variável | Descrição | Padrão |
|---|---|---|
| `DISCORD_TOKEN` | Token do bot Discord | *(obrigatório)* |
| `Database__Path` | Caminho do arquivo SQLite | `data/ticketbot.db` |

---

## 🤝 Contribuindo

1. Fork o projeto
2. Crie uma branch: `git checkout -b feature/minha-feature`
3. Commit: `git commit -m 'feat: minha nova feature'`
4. Push: `git push origin feature/minha-feature`
5. Abra um Pull Request

---

## 📄 Licença

MIT License — veja o arquivo [LICENSE](LICENSE) para detalhes.
