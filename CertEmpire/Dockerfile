FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base

# Add required native libraries
RUN apt-get update && apt-get install -y \
    libfontconfig1 \
    libfreetype6 \
    libglib2.0-0 \
    libx11-6 \
    libxext6 \
    libsm6 \
    libxrender1 \
    libpng-dev \
    && rm -rf /var/lib/apt/lists/*

USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["CertEmpire/CertEmpire.csproj", "CertEmpire/"]
RUN dotnet restore "./CertEmpire/CertEmpire.csproj"
COPY . .
WORKDIR "/src/CertEmpire"
RUN dotnet build "./CertEmpire.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./CertEmpire.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CertEmpire.dll"]
