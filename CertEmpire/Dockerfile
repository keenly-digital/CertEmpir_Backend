# Runtime image with necessary native libs
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base

RUN apt-get update && apt-get install -y \
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

WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Build image
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["CertEmpire/CertEmpire.csproj", "CertEmpire/"]
RUN dotnet restore "CertEmpire/CertEmpire.csproj"
COPY . .
WORKDIR "/src/CertEmpire"
RUN dotnet build "CertEmpire.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish the app
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "CertEmpire.csproj" \
    -c Release \
    -r linux-x64 \
    --self-contained true \
    /p:PublishTrimmed=false \
    /p:UseAppHost=false \
    -o /app/publish

# Final image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CertEmpire.dll"]
