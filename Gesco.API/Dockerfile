# Consulte https://aka.ms/customizecontainer para aprender a personalizar su contenedor de depuración y cómo Visual Studio usa este Dockerfile para compilar sus imágenes para una depuración más rápida.

# Esta fase se usa cuando se ejecuta desde VS en modo rápido (valor predeterminado para la configuración de depuración)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Chrome/Puppeteer: el PDF oficial de ANDE necesita grecaptcha en el portal.
# aspnet:8.0 es Debian slim y no trae libglib ni el resto de .so que Chrome 130 exige.
USER root
RUN apt-get update && apt-get install -y --no-install-recommends \
        ca-certificates \
        fonts-liberation \
        libasound2 \
        libatk-bridge2.0-0 \
        libatk1.0-0 \
        libatspi2.0-0 \
        libcairo2 \
        libcups2 \
        libdbus-1-3 \
        libdrm2 \
        libexpat1 \
        libgbm1 \
        libglib2.0-0 \
        libgtk-3-0 \
        libnspr4 \
        libnss3 \
        libpango-1.0-0 \
        libx11-6 \
        libx11-xcb1 \
        libxcb1 \
        libxcomposite1 \
        libxdamage1 \
        libxext6 \
        libxfixes3 \
        libxkbcommon0 \
        libxrandr2 \
        libxshmfence1 \
        wget \
        xdg-utils \
    && rm -rf /var/lib/apt/lists/*
USER $APP_UID


# Esta fase se usa para compilar el proyecto de servicio
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Gesco.API/Gesco.API.csproj", "Gesco.API/"]
COPY ["Gesco.Application/Gesco.Application.csproj", "Gesco.Application/"]
COPY ["Gesco.Domain/Gesco.Domain.csproj", "Gesco.Domain/"]
COPY ["Gesco.Infrastructure/Gesco.Infrastructure.csproj", "Gesco.Infrastructure/"]

# Restauramos las dependencias
RUN dotnet restore "Gesco.API/Gesco.API.csproj"

# Copiamos el resto del código
COPY . .
WORKDIR "/src/Gesco.API"
RUN dotnet build "Gesco.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Gesco.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
USER root
COPY --from=publish /app/publish .
RUN mkdir -p /home/app/.local/share/Gesco /tmp/Gesco /app/chrome /app/almacen-crm \
    && chown -R $APP_UID:$APP_UID /home/app /tmp/Gesco /app/chrome /app/almacen-crm
USER $APP_UID
ENTRYPOINT ["dotnet", "Gesco.API.dll"]