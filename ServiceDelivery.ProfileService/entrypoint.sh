#!/bin/sh

CERT_DIR="/https"
CERT_PATH="$CERT_DIR/https.pfx"

# Generate cert only if it doesn't exist
if [ ! -f "$CERT_PATH" ]; then
  echo "🔧 Generating self-signed HTTPS certificate..."
  mkdir -p $CERT_DIR
  openssl req -x509 -newkey rsa:4096 -nodes \
    -subj "/CN=localhost" \
    -keyout "$CERT_DIR/key.pem" -out "$CERT_DIR/cert.pem" -days 365

  openssl pkcs12 -export \
    -out "$CERT_PATH" \
    -inkey "$CERT_DIR/key.pem" \
    -in "$CERT_DIR/cert.pem" \
    -passout pass:
fi

echo "✅ HTTPS certificate is ready at $CERT_PATH"
ls -lh "$CERT_PATH"

# Launch the app
exec dotnet ServiceDelivery.ProfileService.dll
