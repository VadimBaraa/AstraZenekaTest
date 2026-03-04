# ── Stage 1: Build ──────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Restore — отдельный слой для кэширования
COPY AnalyticsBackend/*.csproj ./AnalyticsBackend/
RUN dotnet restore "AnalyticsBackend/AnalyticsBackend.csproj"

# Копируем весь проект (Models/ уже внутри AnalyticsBackend/)
COPY AnalyticsBackend/ ./AnalyticsBackend/

WORKDIR "/app/AnalyticsBackend"
RUN dotnet publish "AnalyticsBackend.csproj" -c Release -o /app/publish

# ── Stage 2: Runtime ─────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000

ENTRYPOINT ["dotnet", "AnalyticsBackend.dll"]