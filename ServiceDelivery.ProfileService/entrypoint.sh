#!/bin/sh

echo "✅ Using mkcert-generated trusted certificate"
echo "📄 Cert path: ${ASPNETCORE_Kestrel__Certificates__Default__Path}"
echo "🚀 Starting ASP.NET Core app with HTTPS..."

dotnet ServiceDelivery.ProfileService.dll
