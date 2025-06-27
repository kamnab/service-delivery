#!/bin/sh
CERT_PATH="/https/https.pfx"

if [ ! -f "$CERT_PATH" ]; then
  echo "🔧 Generating cert..."
  mkdir -p /https
  openssl req -x509 -newkey rsa:4096 -nodes -subj "/CN=localhost" \
    -keyout /https/key.pem -out /https/cert.pem -days 365
  openssl pkcs12 -export -out "$CERT_PATH" -inkey /https/key.pem -in /https/cert.pem -passout pass:
  chmod 644 "$CERT_PATH"
fi

echo "✅ HTTPS certificate ready at $CERT_PATH"
exec dotnet ServiceDelivery.ProfileService.dll
