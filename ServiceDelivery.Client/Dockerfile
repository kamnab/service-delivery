# Step 1: Build the Blazor WebAssembly PWA
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

# Set working directory
WORKDIR /src

# Copy the project file and restore dependencies
COPY ["ServiceDelivery.Client.csproj", "/src"]
RUN dotnet restore "ServiceDelivery.Client.csproj"

# Copy the rest of the files and publish the app
COPY . .
WORKDIR "/src"

# Install required dependencies (wasm-tools and python)
RUN apt-get update && apt-get install -y python3 && \
    ln -s /usr/bin/python3 /usr/bin/python

# Install the necessary workload for AOT/WebAssembly
RUN dotnet workload install wasm-tools-net8

# Publish with AOT, trimming, and Brotli compression enabled
RUN dotnet publish "ServiceDelivery.Client.csproj" \
    -c Release \
    -p:RunAOTCompilation=true \
    -p:PublishTrimmed=true \
    -p:BlazorEnableCompression=true \
    -o /app/publish

# Stage 2: Serve the Blazor WASM app using nginx
FROM nginx:alpine AS final

# Copy the published files from the build step
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html
# Copy your self-signed certs and config into the image
COPY ./nginx/certs /etc/nginx/certs/
COPY ./nginx/nginx.conf /etc/nginx/nginx.conf

# Expose port
EXPOSE 80

# Start Nginx to serve static files
CMD ["nginx", "-g", "daemon off;"]
