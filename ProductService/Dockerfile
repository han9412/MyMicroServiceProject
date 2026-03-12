# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Fix SSL certificate chain issues in restricted/corporate environments
RUN apt-get update && apt-get install -y ca-certificates && update-ca-certificates

# Copy shared Contracts library
COPY Contracts/NuGet.Config Contracts/
COPY Contracts/Contracts.csproj Contracts/

# Copy ProductService project file and restore
COPY ProductService/NuGet.Config ProductService/
COPY ProductService/ProductService.csproj ProductService/
RUN dotnet restore ProductService/ProductService.csproj

# Copy all source and publish
COPY Contracts/ Contracts/
COPY ProductService/ ProductService/
RUN dotnet publish ProductService/ProductService.csproj -c Release -o /app

# Stage 2: Run
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "ProductService.dll"]