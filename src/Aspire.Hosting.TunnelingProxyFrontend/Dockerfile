FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build-env

WORKDIR /App

# Copy everything
COPY . ./
# Restore as distinct layers
RUN dotnet restore

# Build and publish a release
RUN dotnet publish src/Aspire.Hosting.TunnelingProxyFrontend/Aspire.Hosting.TunnelingProxyFrontend.csproj -f net8.0 --no-restore -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /App
COPY --from=build-env /App/out .
ENTRYPOINT ["dotnet", "Aspire.Hosting.TunnelingProxyFrontend.dll"]
