# Usar la imagen oficial de .NET 7.0
FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Instalar Tesseract OCR y dependencias
RUN apt-get update && apt-get install -y \
    tesseract-ocr \
    tesseract-ocr-spa \
    tesseract-ocr-eng \
    libgdiplus \
    libc6-dev \
    && rm -rf /var/lib/apt/lists/*

# Usar la imagen de SDK para compilar
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src

# Copiar archivos de proyecto y restaurar dependencias
COPY ["src/CarnetAduaneroProcessor.API/CarnetAduaneroProcessor.API.csproj", "src/CarnetAduaneroProcessor.API/"]
COPY ["src/CarnetAduaneroProcessor.Core/CarnetAduaneroProcessor.Core.csproj", "src/CarnetAduaneroProcessor.Core/"]
COPY ["src/CarnetAduaneroProcessor.Infrastructure/CarnetAduaneroProcessor.Infrastructure.csproj", "src/CarnetAduaneroProcessor.Infrastructure/"]
COPY ["src/CarnetAduaneroProcessor.Web/CarnetAduaneroProcessor.Web.csproj", "src/CarnetAduaneroProcessor.Web/"]

RUN dotnet restore "src/CarnetAduaneroProcessor.API/CarnetAduaneroProcessor.API.csproj"

# Copiar todo el código fuente
COPY . .
WORKDIR "/src/src/CarnetAduaneroProcessor.API"

# Compilar la aplicación
RUN dotnet build "CarnetAduaneroProcessor.API.csproj" -c Release -o /app/build

# Publicar la aplicación
FROM build AS publish
RUN dotnet publish "CarnetAduaneroProcessor.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Imagen final
FROM base AS final
WORKDIR /app

# Copiar archivos de Tesseract
COPY --from=build /usr/share/tessdata /usr/share/tessdata

# Copiar la aplicación publicada
COPY --from=publish /app/publish .

# Crear directorio para uploads
RUN mkdir -p /app/Uploads
RUN mkdir -p /app/Logs

# Establecer permisos
RUN chmod -R 755 /app/Uploads
RUN chmod -R 755 /app/Logs

# Usuario no root para seguridad
RUN groupadd -r appuser && useradd -r -g appuser appuser
RUN chown -R appuser:appuser /app
USER appuser

ENTRYPOINT ["dotnet", "CarnetAduaneroProcessor.API.dll"] 