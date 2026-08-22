#!/bin/sh
# Writes wwwroot/appsettings.json from the API_BASE_URL environment variable.
#
# Program.cs reads builder.Configuration["ApiBaseUrl"] and falls back to
# http://<page-host>:7136/ when it is blank — which is right for dev and wrong in
# production, so this file must exist with the real value before anyone loads the app.
set -eu

: "${API_BASE_URL:?API_BASE_URL is not set — the app would fall back to the dev API URL}"

ROOT=/usr/share/nginx/html

# Refit builds request URIs against this as a base; without the trailing slash the
# last path segment gets replaced instead of appended.
case "$API_BASE_URL" in
    */) ;;
    *) API_BASE_URL="$API_BASE_URL/" ;;
esac

printf '{\n  "ApiBaseUrl": "%s"\n}\n' "$API_BASE_URL" > "$ROOT/appsettings.json"

# Publish may have emitted precompressed siblings of the file we just rewrote; with
# gzip_static on, nginx would serve the stale compressed copy in preference.
rm -f "$ROOT/appsettings.json.gz" "$ROOT/appsettings.json.br"

echo "10-api-base-url.sh: ApiBaseUrl set to $API_BASE_URL"
