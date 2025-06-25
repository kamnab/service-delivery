#!/bin/sh

CERT_PATH="${ASPNETCORE_Kestrel__Certificates__Default__Path:-/app/Infrastructure/Resources/https/profile-service.pfx}"
PASSWORD="${ASPNETCORE_Kestrel__Certificates__Default__Password:-profile-service}"
CERT_DIR="$(dirname "$CERT_PATH")"

mkdir -p "$CERT_DIR"

# Generate self-signed cert only if not already present
if [ ! -f "$CERT_PATH" ]; then
  echo "🔐 Generating self-signed HTTPS certificate..."

  openssl req -x509 -newkey rsa:2048 -nodes \
    -keyout "$CERT_DIR/key.pem" \
    -out "$CERT_DIR/cert.pem" \
    -subj "/CN=localhost" \
    -days 365

  openssl pkcs12 -export \
    -out "$CERT_PATH" \
    -inkey "$CERT_DIR/key.pem" \
    -in "$CERT_DIR/cert.pem" \
    -passout pass:"$PASSWORD"

  echo "✅ Certificate created at $CERT_PATH"
fi

echo "🚀 Starting ASP.NET Core app with HTTPS"
dotnet ServiceDelivery.ProfileService.dll
