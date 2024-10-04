FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build-env

WORKDIR /App

# Copy everything
COPY . ./
# Restore as distinct layers
RUN dotnet restore

# Build and publish a release
RUN dotnet publish playground/DavidTest/DavidTest.Web --no-restore -o out; exit 0
RUN dotnet publish playground/DavidTest/DavidTest.Web --no-restore -o out; exit 0
RUN dotnet publish playground/DavidTest/DavidTest.Web --no-restore -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /App
COPY --from=build-env /App/out .
ENTRYPOINT ["dotnet", "DavidTest.Web.dll"]
