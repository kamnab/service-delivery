#!/bin/sh

#!/bin/bash

# Generate self-signed cert at runtime (if not exist)
if [ ! -f /https/https.pfx ]; then
  mkdir -p /https
  openssl req -x509 -newkey rsa:4096 -nodes \
    -subj "/CN=localhost" \
    -keyout /https/key.pem -out /https/cert.pem -days 365

  openssl pkcs12 -export \
    -out /https/https.pfx \
    -inkey /https/key.pem \
    -in /https/cert.pem \
    -passout pass:
fi

# echo "✅ Using mkcert-generated trusted certificate"
# echo "📄 Cert path: ${ASPNETCORE_Kestrel__Certificates__Default__Path}"
# echo "🚀 Starting ASP.NET Core app with HTTPS..."

exec dotnet ServiceDelivery.ProfileService.dll
