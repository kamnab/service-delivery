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
RUN dotnet publish "ServiceDelivery.Client.csproj" -c Release -o /app/publish

# Step 2: Serve the Blazor WebAssembly app as static files
FROM nginx:latest AS base

# Copy the published files from the build step
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html
# Copy your self-signed certs and config into the image
COPY ./nginx/certs /etc/nginx/certs/
COPY ./nginx/nginx.conf /etc/nginx/nginx.conf

# Expose ports for HTTP and HTTPS
EXPOSE 80
EXPOSE 443

# Start Nginx to serve static files
CMD ["nginx", "-g", "daemon off;"]
