# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY .editorconfig Directory.Build.props Directory.Packages.props ./
COPY src/Travelogic.Suppliers.Domain/Travelogic.Suppliers.Domain.csproj src/Travelogic.Suppliers.Domain/
COPY src/Travelogic.Suppliers.Application/Travelogic.Suppliers.Application.csproj src/Travelogic.Suppliers.Application/
COPY src/Travelogic.Suppliers.Infrastructure/Travelogic.Suppliers.Infrastructure.csproj src/Travelogic.Suppliers.Infrastructure/
COPY src/Travelogic.Suppliers.Api/Travelogic.Suppliers.Api.csproj src/Travelogic.Suppliers.Api/
RUN dotnet restore src/Travelogic.Suppliers.Api/Travelogic.Suppliers.Api.csproj

COPY src/ src/
RUN dotnet publish src/Travelogic.Suppliers.Api/Travelogic.Suppliers.Api.csproj \
    --configuration Release --no-restore --output /app /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
COPY --from=build /app .
ENTRYPOINT ["dotnet", "Travelogic.Suppliers.Api.dll"]
