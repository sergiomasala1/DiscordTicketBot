# ── Stage 1: Build ────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
 
COPY DiscordTicketBot.csproj ./
RUN dotnet restore
 
COPY . ./
RUN dotnet publish -c Release -o /app/publish --no-restore
 
# ── Stage 2: Runtime ───────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
 
RUN mkdir -p /app/data /app/logs
 
COPY --from=build /app/publish .
 
ENV DISCORD_TOKEN=""
ENV Database__Path="/app/data/ticketbot.db"
 
RUN useradd -m botuser && chown -R botuser /app
USER botuser
 
ENTRYPOINT ["dotnet", "DiscordTicketBot.dll"]
