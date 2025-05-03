# --------- Stage 1: Build Blazor WebAssembly PWA ---------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src

# Copy project file and restore dependencies
COPY ["ServiceDelivery.Client.csproj", "./"]
RUN dotnet restore "ServiceDelivery.Client.csproj"

# Copy the rest of the source
COPY . .

# Install Python (needed for wasm-tools)
RUN apt-get update && apt-get install -y python3 && \
    ln -s /usr/bin/python3 /usr/bin/python

# Install WebAssembly workload with AOT
RUN dotnet workload install wasm-tools-net8

# Publish with AOT, trimming, and Brotli compression
RUN dotnet publish "ServiceDelivery.Client.csproj" \
    -c Release \
    -o /app/publish


# --------- # Stage 2: Serve with NGINX
FROM nginx:alpine AS final

# Set working directory
WORKDIR /app

# Copy published app from build stage
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html

# Copy custom Nginx config
COPY ./nginx/nginx.conf /etc/nginx/nginx.conf

# Copy SSL certs
# COPY ./scripts/certs/oer.local.pem /etc/nginx/certs/oer.local.pem
# COPY ./scripts/certs/oer.local-key.pem /etc/nginx/certs/oer.local-key.pem

# Restrict permissions on certs
# RUN chmod 600 /etc/nginx/certs/*

# Expose web ports
EXPOSE 80 443

# Start dbus, avahi-daemon, and nginx
CMD ["nginx", "-g", "daemon off;"]
