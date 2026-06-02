#!/usr/bin/env bash
# Provision + deploy ValueKu to Azure App Service (Free F1) + Azure SQL.
# Usage:  bash deploy-azure.sh <globally-unique-app-name>
# Example: bash deploy-azure.sh valueku-rahman
# Optional env vars to also configure Google sign-in / Blob avatars:
#   GOOGLE_CLIENT_ID=... GOOGLE_CLIENT_SECRET=... STORAGE_BLOB_CONN=... bash deploy-azure.sh valueku-rahman
set -euo pipefail

APP="${1:-}"
if [ -z "$APP" ]; then
  echo "Usage: bash deploy-azure.sh <globally-unique-app-name>   (e.g. valueku-rahman)"
  exit 1
fi

RG="${RG:-valueku-rg}"
LOC="${LOC:-eastasia}"   # must be in your subscription's allowed regions
PLAN="${APP}-plan"
SQLSERVER="${SQLSERVER:-${APP}-sql}"
SQLDB="ValueKu"
SQLADMIN="valekuadmin"
SQLPWD="$(openssl rand -base64 18 | tr -dc 'A-Za-z0-9')Aa9#x"
ADMINPWD="$(openssl rand -base64 12 | tr -dc 'A-Za-z0-9')Aa9#x"
ROOT="$(cd "$(dirname "$0")" && pwd)"

echo "==> Target: https://$APP.azurewebsites.net  (RG=$RG, region=$LOC, Free F1)"

echo "==> Building & publishing locally..."
rm -rf "$ROOT/publish" "$ROOT/app.zip"
dotnet publish "$ROOT/ValueKu/ValueKu.csproj" -c Release -o "$ROOT/publish" -v quiet
( cd "$ROOT/publish" && zip -qr "$ROOT/app.zip" . )

echo "==> Creating resource group..."
az group create -n "$RG" -l "$LOC" -o none

echo "==> Creating Azure SQL (a few minutes)..."
az sql server create -n "$SQLSERVER" -g "$RG" -l "$LOC" -u "$SQLADMIN" -p "$SQLPWD" -o none
az sql db create -g "$RG" -s "$SQLSERVER" -n "$SQLDB" --service-objective Basic --backup-storage-redundancy Local -o none
az sql server firewall-rule create -g "$RG" -s "$SQLSERVER" -n AllowAzure \
  --start-ip-address 0.0.0.0 --end-ip-address 0.0.0.0 -o none

echo "==> Creating App Service (Linux, .NET 9, Free F1)..."
az appservice plan create -g "$RG" -n "$PLAN" --is-linux --sku F1 -o none
az webapp create -g "$RG" -p "$PLAN" -n "$APP" --runtime "DOTNETCORE:9.0" -o none

echo "==> Applying configuration..."
CONN="Server=tcp:$SQLSERVER.database.windows.net,1433;Initial Catalog=$SQLDB;User ID=$SQLADMIN;Password=$SQLPWD;Encrypt=True;TrustServerCertificate=False;Connection Timeout=60;MultipleActiveResultSets=True"
az webapp config connection-string set -n "$APP" -g "$RG" \
  --connection-string-type SQLAzure --settings DefaultConnection="$CONN" -o none

SETTINGS=(SeedUser__Password="$ADMINPWD")
[ -n "${GOOGLE_CLIENT_ID:-}" ]     && SETTINGS+=(Authentication__Google__ClientId="$GOOGLE_CLIENT_ID")
[ -n "${GOOGLE_CLIENT_SECRET:-}" ] && SETTINGS+=(Authentication__Google__ClientSecret="$GOOGLE_CLIENT_SECRET")
[ -n "${STORAGE_BLOB_CONN:-}" ]    && SETTINGS+=(Storage__BlobConnectionString="$STORAGE_BLOB_CONN")
az webapp config appsettings set -n "$APP" -g "$RG" --settings "${SETTINGS[@]}" -o none

echo "==> Deploying compiled app..."
az webapp deploy -g "$RG" -n "$APP" --src-path "$ROOT/app.zip" --type zip -o none
az webapp restart -n "$APP" -g "$RG" -o none
rm -f "$ROOT/app.zip"

cat <<DONE

============================================================
 Deployed:   https://$APP.azurewebsites.net
 Admin login: admin  /  $ADMINPWD
 SQL admin:   $SQLADMIN  /  $SQLPWD     <-- save this somewhere safe
 Google redirect URI (add in Google console if you use Google sign-in):
              https://$APP.azurewebsites.net/signin-google
 Note: the first page load takes ~30-60s while it migrates + seeds.
 To remove everything later:  az group delete -n $RG
============================================================
DONE
