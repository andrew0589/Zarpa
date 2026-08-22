# Build context is the REPO ROOT (see docker-compose.dokploy.yml), so every path
# below is relative to C:\NavigationES.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first, with only the project files copied: this layer is cached and
# survives every source edit that doesn't touch a .csproj.
COPY global.json ./
COPY NavigationES.Api/NavigationES.Api.csproj NavigationES.Api/
COPY NavigationES.Shared/NavigationES.Shared.csproj NavigationES.Shared/
RUN dotnet restore NavigationES.Api/NavigationES.Api.csproj

COPY NavigationES.Api/ NavigationES.Api/
COPY NavigationES.Shared/ NavigationES.Shared/
RUN dotnet publish NavigationES.Api/NavigationES.Api.csproj \
    -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# curl is only here for the container HEALTHCHECK below.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

# `app` is the non-root user the .NET base images already ship.
USER $APP_UID

# TLS is terminated by Traefik; the container speaks plain HTTP on the internal network.
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    TZ=UTC
EXPOSE 8080

# /ping is the API's anonymous liveness endpoint (Program.cs). start-period covers
# the EF Core migration run at startup.
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD curl --fail http://localhost:8080/ping || exit 1

ENTRYPOINT ["dotnet", "NavigationES.Api.dll"]
