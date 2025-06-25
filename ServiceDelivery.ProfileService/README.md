# Running using Docker
Here's a complete working setup using mkcert-generated trusted HTTPS certificates for your ASP.NET Core app running in Docker — with .env, Dockerfile, entrypoint.sh, and docker-compose.yml

## ✅ 1. .env (optional)
```
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=https://+:443;http://+:80
ASPNETCORE_Kestrel__Certificates__Default__Path=/app/Infrastructure/Resources/https/profile-service.pfx
ASPNETCORE_Kestrel__Certificates__Default__Password=changeit 
```

## ✅ 2. mkcert Certificate Generation Script (run once on host)
```
#!/bin/bash
# generate-dev-cert.sh

mkdir -p Infrastructure/Resources/https

mkcert -pkcs12 -p12-file Infrastructure/Resources/https/profile-service.pfx localhost 127.0.0.1 ::1

echo "✅ Trusted HTTPS cert generated at Infrastructure/Resources/https/profile-service.pfx"
```

Run this script on your host, not in Docker:
```
chmod +x generate-dev-cert.sh
./generate-dev-cert.sh
```

## ✅ 3. entrypoint.sh (used by Docker container)
```
#!/bin/sh

echo "✅ Using mkcert-generated trusted certificate"
echo "📄 Cert path: ${ASPNETCORE_Kestrel__Certificates__Default__Path}"
echo "🚀 Starting ASP.NET Core app with HTTPS..."

dotnet ServiceDelivery.ProfileService.dll
```

## ✅ 4. Dockerfile
```
# -------- Build Stage --------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /source

COPY . .
RUN dotnet restore "./ServiceDelivery.ProfileService.csproj"
RUN dotnet publish "./ServiceDelivery.ProfileService.csproj" -c Release -o /app/publish

# -------- Runtime Stage --------
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

# Copy app
COPY --from=build /app/publish .

# Entry script
COPY entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh

# Ports
EXPOSE 443 80

# Run app
ENTRYPOINT ["/entrypoint.sh"]
```

## ✅ 5. docker-compose.yml
```
version: "3.8"

services:
  profile-service:
    build:
      context: .
    ports:
      - "5501:443"  # HTTPS
      - "5502:80"   # Optional HTTP
    environment:
      - ASPNETCORE_ENVIRONMENT=${ASPNETCORE_ENVIRONMENT}
      - ASPNETCORE_URLS=${ASPNETCORE_URLS}
      - ASPNETCORE_Kestrel__Certificates__Default__Path=${ASPNETCORE_Kestrel__Certificates__Default__Path}
      - ASPNETCORE_Kestrel__Certificates__Default__Password=${ASPNETCORE_Kestrel__Certificates__Default__Password}
    volumes:
      - ./Infrastructure/Resources:/app/Infrastructure/Resources
```

## ✅ Final Setup Checklist
1- Install mkcert (on host):
2- Generate the cert on host: ./generate-dev-cert.sh
3- Build and run: docker compose up --build
4- Visit app: 