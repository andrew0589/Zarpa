# Build context is the REPO ROOT (see docker-compose.dokploy.yml).
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY global.json ./
COPY NavigationES.Web/NavigationES.Web.csproj NavigationES.Web/
COPY NavigationES.ApiClient/NavigationES.ApiClient.csproj NavigationES.ApiClient/
COPY NavigationES.Shared/NavigationES.Shared.csproj NavigationES.Shared/
RUN dotnet restore NavigationES.Web/NavigationES.Web.csproj

COPY NavigationES.Web/ NavigationES.Web/
COPY NavigationES.ApiClient/ NavigationES.ApiClient/
COPY NavigationES.Shared/ NavigationES.Shared/
RUN dotnet publish NavigationES.Web/NavigationES.Web.csproj -c Release -o /app/publish

# Blazor WebAssembly publishes to plain static files — nothing .NET runs on the
# server, so the runtime image is just nginx.
FROM nginx:1.27-alpine AS final

COPY --from=build /app/publish/wwwroot /usr/share/nginx/html
COPY NavigationES.Web/nginx.conf /etc/nginx/conf.d/default.conf

# nginx's own entrypoint runs everything in /docker-entrypoint.d before starting;
# this script bakes API_BASE_URL into wwwroot/appsettings.json at container start,
# so the same image can point at a different API without a rebuild.
COPY --chmod=755 NavigationES.Web/docker-entrypoint.d/10-api-base-url.sh \
     /docker-entrypoint.d/10-api-base-url.sh

EXPOSE 80

HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD wget -q -O /dev/null http://localhost/healthz || exit 1
