# ── Stage 1: Build ────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /

# Copy project files first — NuGet restore is cached until .csproj changes
COPY ["WeatherService.Api/WeatherService.Api.csproj", "WeatherService.Api/"]
COPY ["WeatherService.Model/WeatherService.Model.csproj", "WeatherService.Model/"]
COPY ["WeatherService.Infrastructure/WeatherService.Infrastructure.csproj", "WeatherService.Infrastructure/"]
RUN dotnet restore "WeatherService.Api/WeatherService.Api.csproj"

# Copy source and build
COPY ..
WORKDIR "/WeatherService.Api"
RUN dotnet build "WeatherService.Api.csproj" -c Release -o /app/build

# ── Stage 2: Publish ──────────────────────────────────────────────────────────
FROM build as publish
RUN dotnet publish "WeatherService.Api.csproj" \
	-c Release -o /app/publish \
	/p:UseAppHost=false \
	/p:GenerateDocumentationFile=true
	
# ── Stage 3: Runtime ──────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

# Run as non-root user for security
RUN addgroup --system --gid 1001 weatherapp \
	&& adduser --system --uid 1001 --ingroup weatherapp --shell /bin/false weatherapp

WORKDIR /app/build
RUN mkdir -p /app/data /app/logs && chown -R weatherapp:weatherapp /app/build

COPY --from=publish --chown=weatherapp:weatherapp /app/publish .
USER weatherapp

HEALTHCHECK --interval=30s --timeout=10s --start-period=20s --retries=3 \
	CMD curl http://localhost:8080/health || exit 1
	
EXPOSE 8080
ENV ASPNETCORE_URLS="http//+:8080"
ENV ASPNETCORE_ENVIRONMENT="Production"
ENV Database__Path="/app/data/weather.db"

ENTRYPOINT["dotnet", "WeatherService.Api.dll"]