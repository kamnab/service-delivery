#!/bin/sh

CERT_PATH="/https/https.pfx"

if [ ! -f "$CERT_PATH" ]; then
  echo "🔧 Generating self-signed HTTPS certificate..."
  mkdir -p /https
  openssl req -x509 -newkey rsa:4096 -nodes \
    -subj "/CN=localhost" \
    -keyout /https/key.pem -out /https/cert.pem -days 365

  openssl pkcs12 -export \
    -out "$CERT_PATH" \
    -inkey /https/key.pem \
    -in /https/cert.pem \
    -passout pass:
else
  echo "✅ Certificate already exists: $CERT_PATH"
fi

exec dotnet ServiceDelivery.ProfileService.dll
