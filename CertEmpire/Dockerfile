# Base image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base

# Switch to root to install native libraries needed by SkiaSharp and others
USER root

# Install SkiaSharp native dependencies (required for image/font rendering)
RUN apt-get update && apt-get install -y --no-install-recommends \
    libfontconfig1 \
    libfreetype6 \
    libharfbuzz0b \
    libjpeg62-turbo \
    libglib2.0-0 \
    libx11-6 \
    libxext6 \
    libsm6 \
    libxrender1 \
    libpng16-16 \
    && rm -rf /var/lib/apt/lists/*

# Optional: switch back to non-root if required by hosting provider
# USER $APP_UID   ← you can comment this if $APP_UID is undefined
WORKDIR /app
EXPOSE 8080 8081

# Build image
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["CertEmpire/CertEmpire.csproj", "CertEmpire/"]
RUN dotnet restore "CertEmpire/CertEmpire.csproj"
COPY . .
WORKDIR "/src/CertEmpire"
RUN dotnet build "CertEmpire.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish app
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "CertEmpire.csproj" -c $BUILD_CONFIGURATION -r linux-x64 --self-contained false /p:UseAppHost=false -o /app/publish

# Final runtime container
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CertEmpire.dll"]
