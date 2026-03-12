# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Fix SSL certificate chain issues in restricted environments
RUN apt-get update && apt-get install -y ca-certificates && update-ca-certificates

# Copy shared Contracts library
COPY Contracts/NuGet.Config Contracts/
COPY Contracts/Contracts.csproj Contracts/

# Copy OrderService project file and restore
COPY OrderService/NuGet.Config OrderService/
COPY OrderService/OrderService.csproj OrderService/
RUN dotnet restore OrderService/OrderService.csproj

# Copy all source and publish
COPY Contracts/ Contracts/
COPY OrderService/ OrderService/
RUN dotnet publish OrderService/OrderService.csproj -c Release -o /app

# Stage 2: Run
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "OrderService.dll"]
