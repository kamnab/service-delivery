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
EXPOSE 5502

# Run app
ENTRYPOINT ["/entrypoint.sh"]


# docker run --rm --env-file .env odi-profile-service:dev env | grep ASPNETCORE
# docker run --rm \
#     --name odi-profile-service \
#     --env-file .env \
#     -p 5501:5501 \
#     -p 5502:5502 \
#     -v $(pwd)/profile-service.pfx:/app/Infrastructure/Resources/https/profile-service.pfx \
#     odi-profile-service:prod
