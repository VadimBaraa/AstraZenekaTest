FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj(s) and nuget config first (paths relative to build context)
COPY ["AnalyticsBackend/AnalyticsBackend.csproj", "AnalyticsBackend/"]
# If you use nuget.config or private feeds:
# COPY ["nuget.config", "./"]

WORKDIR /src/AnalyticsBackend
RUN dotnet restore "AnalyticsBackend.csproj"

# Now copy the rest of the source
WORKDIR /src
COPY . .

WORKDIR /src/AnalyticsBackend
RUN dotnet publish -c Release -o /app/publish
