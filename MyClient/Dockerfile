# Stage 1: Build Blazor WASM static files
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Fix SSL certificate chain issues in restricted environments
RUN apt-get update && apt-get install -y ca-certificates && update-ca-certificates

COPY NuGet.Config .
COPY *.csproj .
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release -o /app

# Stage 2: Serve static files with nginx
FROM nginx:alpine
COPY nginx.conf /etc/nginx/nginx.conf
# Blazor publish output static files are in wwwroot/
COPY --from=build /app/wwwroot /usr/share/nginx/html
EXPOSE 80
