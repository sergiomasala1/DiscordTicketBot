# ── Stage 1: Build ────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia arquivos de projeto e restaura dependências (cache layer)
COPY DiscordTicketBot.csproj ./
RUN dotnet restore

# Copia o restante e publica
COPY . ./
RUN dotnet publish -c Release -o /app/publish --no-restore

# ── Stage 2: Runtime ───────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Cria diretório para dados persistentes
RUN mkdir -p /app/data /app/logs

# Copia os binários publicados
COPY --from=build /app/publish .

# Variáveis de ambiente (sobrescreva ao fazer deploy)
ENV DISCORD_TOKEN=""
ENV Database__Path="/app/data/ticketbot.db"

# Volume para persistência do banco SQLite
VOLUME ["/app/data", "/app/logs"]

# Usuário não-root para segurança
RUN useradd -m botuser && chown -R botuser /app
USER botuser

ENTRYPOINT ["dotnet", "DiscordTicketBot.dll"]
