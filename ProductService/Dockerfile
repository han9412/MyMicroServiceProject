# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Fix SSL certificate chain issues in restricted/corporate environments
RUN apt-get update && apt-get install -y ca-certificates && update-ca-certificates

# Copy csproj first for better layer caching — only re-restores when dependencies change
COPY NuGet.Config .
COPY *.csproj .
RUN dotnet restore

# Copy the rest and publish
COPY . .
RUN dotnet publish -c Release -o /app

# Stage 2: Run
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "ProductService.dll"]