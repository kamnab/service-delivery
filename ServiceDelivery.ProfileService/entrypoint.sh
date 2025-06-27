#!/bin/sh

#!/bin/bash

# Generate self-signed cert at runtime (if not exist)
if [ ! -f /app/https/https.pfx ]; then
  mkdir -p /app/https
  openssl req -x509 -newkey rsa:4096 -nodes \
    -subj "/CN=localhost" \
    -keyout /app/https/key.pem -out /app/https/cert.pem -days 365

  openssl pkcs12 -export \
    -out /app/https/https.pfx \
    -inkey /app/https/key.pem \
    -in /app/https/cert.pem \
    -passout pass:
fi

# echo "✅ Using mkcert-generated trusted certificate"
# echo "📄 Cert path: ${ASPNETCORE_Kestrel__Certificates__Default__Path}"
# echo "🚀 Starting ASP.NET Core app with HTTPS..."

exec dotnet ServiceDelivery.ProfileService.dll
