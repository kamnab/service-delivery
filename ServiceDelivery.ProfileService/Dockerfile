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
