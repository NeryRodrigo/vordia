# Consulte https://aka.ms/customizecontainer para aprender a personalizar su contenedor de depuración y cómo Visual Studio usa este Dockerfile para compilar sus imágenes para una depuración más rápida.

# Esta fase se usa cuando se ejecuta desde VS en modo rápido (valor predeterminado para la configuración de depuración)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


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
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Gesco.API.dll"]