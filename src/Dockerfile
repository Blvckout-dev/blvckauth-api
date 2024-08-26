FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env

# Copy everything
COPY my-masternode-auth/ my-masternode-auth/
COPY my-masternode-data-models/ my-masternode-data-models/
WORKDIR /my-masternode-auth
# Restore as distinct layers
RUN dotnet restore
# Build and publish a release
RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /my-masternode-auth
COPY --from=build-env /my-masternode-auth/out .
ENV DOTNET_EnableDiagnostics=0
EXPOSE 8080
ENTRYPOINT ["dotnet", "my-masternode-auth.dll"]