# Docker - Procesador OCR

Este documento explica cómo ejecutar el Procesador OCR usando Docker.

## Requisitos Previos

- Docker Desktop instalado
- Docker Compose instalado
- Al menos 4GB de RAM disponible

## Configuración Rápida

### 1. Construir y Ejecutar con Docker Compose

```bash
# Construir y ejecutar la aplicación
docker-compose up --build

# Ejecutar en segundo plano
docker-compose up -d --build
```

### 2. Acceder a la Aplicación

- **API**: http://localhost:8080
- **Swagger UI**: http://localhost:8080
- **Health Check**: http://localhost:8080/health
- **Info**: http://localhost:8080/info

### 3. Ver Logs

```bash
# Ver logs en tiempo real
docker-compose logs -f

# Ver logs del contenedor específico
docker-compose logs -f procesador-ocr
```

## Construcción Manual

### 1. Construir la Imagen

```bash
docker build -t procesador-ocr:latest .
```

### 2. Ejecutar el Contenedor

```bash
docker run -d \
  --name procesador-ocr \
  -p 8080:80 \
  -p 8443:443 \
  -v $(pwd)/Uploads:/app/Uploads \
  -v $(pwd)/Logs:/app/Logs \
  -v $(pwd)/tessdata:/usr/share/tessdata \
  procesador-ocr:latest
```

## Estructura de Volúmenes

```
./Uploads/          # Archivos subidos por usuarios
./Logs/             # Logs de la aplicación
./tessdata/         # Datos de entrenamiento de Tesseract OCR
```

## Variables de Entorno

Puedes configurar las siguientes variables de entorno:

```bash
# En docker-compose.yml o al ejecutar docker run
environment:
  - ASPNETCORE_ENVIRONMENT=Production
  - ASPNETCORE_URLS=http://+:80;https://+:443
  - AzureVision__Key=tu-clave-de-azure
  - AzureVision__Endpoint=tu-endpoint-de-azure
```

## Comandos Útiles

```bash
# Detener la aplicación
docker-compose down

# Detener y eliminar volúmenes
docker-compose down -v

# Reconstruir sin cache
docker-compose build --no-cache

# Ver contenedores en ejecución
docker ps

# Entrar al contenedor
docker exec -it procesador-ocr bash

# Ver logs del contenedor
docker logs procesador-ocr

# Eliminar contenedor
docker rm -f procesador-ocr

# Eliminar imagen
docker rmi procesador-ocr:latest
```

## Solución de Problemas

### 1. Puerto ya en uso

Si el puerto 8080 está ocupado, cambia el mapeo en `docker-compose.yml`:

```yaml
ports:
  - "8081:80"  # Cambia 8080 por 8081
```

### 2. Permisos de archivos

Si hay problemas con permisos en Windows:

```bash
# En PowerShell como administrador
icacls "Uploads" /grant "Everyone:(OI)(CI)F"
icacls "Logs" /grant "Everyone:(OI)(CI)F"
```

### 3. Memoria insuficiente

Si Docker no tiene suficiente memoria:
- Abre Docker Desktop
- Ve a Settings → Resources → Memory
- Aumenta a al menos 4GB

### 4. Tesseract no funciona

Verifica que los archivos de tessdata estén presentes:

```bash
# Verificar archivos de Tesseract en el contenedor
docker exec -it procesador-ocr ls -la /usr/share/tessdata/
```

## Desarrollo

### Modo Desarrollo

Para desarrollo local, puedes usar:

```bash
# Ejecutar en modo desarrollo
docker-compose -f docker-compose.dev.yml up --build
```

### Hot Reload

Para desarrollo con hot reload:

```bash
# Montar el código fuente para cambios en tiempo real
docker run -d \
  --name procesador-ocr-dev \
  -p 8080:80 \
  -v $(pwd)/src:/app/src \
  procesador-ocr:latest
```

## Producción

### Configuración de Producción

Para producción, considera:

1. **Variables de entorno seguras**:
   ```bash
   export AzureVision__Key="tu-clave-real"
   export AzureVision__Endpoint="tu-endpoint-real"
   ```

2. **Volúmenes persistentes**:
   ```yaml
   volumes:
     - procesador_uploads:/app/Uploads
     - procesador_logs:/app/Logs
   ```

3. **Health checks**:
   ```yaml
   healthcheck:
     test: ["CMD", "curl", "-f", "http://localhost/health"]
     interval: 30s
     timeout: 10s
     retries: 3
   ```

### Escalado

```bash
# Escalar a múltiples instancias
docker-compose up --scale procesador-ocr=3
```

## Seguridad

- La aplicación se ejecuta como usuario no-root
- Los puertos están configurados para acceso local
- Las claves de Azure están incluidas (solo para pruebas)
- Para producción, usa variables de entorno o secretos de Docker

## Monitoreo

### Health Check

```bash
# Verificar estado de salud
curl http://localhost:8080/health
```

### Métricas

```bash
# Ver información del sistema
curl http://localhost:8080/info
```

### Logs

```bash
# Ver logs en tiempo real
docker-compose logs -f --tail=100
``` 