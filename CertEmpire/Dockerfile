# Base image for runtime (uses ASP.NET Core runtime only)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base

# Install native dependencies needed for SkiaSharp (used indirectly by DocX when rendering images)
RUN apt-get update && apt-get install -y \
    libfontconfig1 \
    libfreetype6 \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# SDK image for building the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project file and restore dependencies
COPY ["CertEmpire/CertEmpire.csproj", "CertEmpire/"]
RUN dotnet restore "CertEmpire/CertEmpire.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/CertEmpire"
RUN dotnet build "CertEmpire.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish the application
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "CertEmpire.csproj" \
    -c $BUILD_CONFIGURATION \
    -r linux-x64 \
    --self-contained true \
    /p:PublishTrimmed=false \
    /p:UseAppHost=false \
    -o /app/publish

# Final runtime image with app
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CertEmpire.dll"]
