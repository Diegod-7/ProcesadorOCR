# 🚀 Instrucciones para Ejecutar el Proyecto

## 📋 Prerrequisitos

Antes de ejecutar el proyecto, asegúrate de tener instalado:

- **.NET 8 SDK** - [Descargar aquí](https://dotnet.microsoft.com/download/dotnet/8.0)
- **SQL Server** (LocalDB, Express o Developer Edition)
- **Visual Studio 2022** o **VS Code** con extensiones de C#

## 🔧 Configuración Inicial

### 1. Clonar y Restaurar Dependencias

```bash
# Navegar al directorio del proyecto
cd CarnetAduaneroProcessor

# Restaurar todas las dependencias
dotnet restore
```

### 2. Configurar Base de Datos

#### Opción A: SQL Server LocalDB (Recomendado para desarrollo)

```bash
# Crear la base de datos
dotnet ef database update --project src/CarnetAduaneroProcessor.API
```

#### Opción B: SQL Server Express/Developer

1. Abrir SQL Server Management Studio
2. Conectar a tu instancia de SQL Server
3. Ejecutar el script de migración que se genera con:
   ```bash
   dotnet ef migrations script --project src/CarnetAduaneroProcessor.API
   ```

### 3. Configurar Azure Form Recognizer (Opcional)

Si quieres usar Azure Form Recognizer para mejor extracción:

1. Crear un recurso de Azure Form Recognizer en [Azure Portal](https://portal.azure.com)
2. Obtener el endpoint y la clave
3. Editar `src/CarnetAduaneroProcessor.API/appsettings.json`:

```json
{
  "AzureFormRecognizer": {
    "Endpoint": "https://tu-form-recognizer.cognitiveservices.azure.com/",
    "Key": "tu-clave-aqui"
  }
}
```

**Nota:** Si no configuras Azure Form Recognizer, el sistema usará extracción manual con patrones regex.

## 🏃‍♂️ Ejecutar el Proyecto

### Opción 1: Visual Studio

1. Abrir `CarnetAduaneroProcessor.sln` en Visual Studio
2. Establecer `CarnetAduaneroProcessor.API` como proyecto de inicio
3. Presionar F5 o hacer clic en "Start Debugging"

### Opción 2: Línea de Comandos

```bash
# Navegar al proyecto API
cd src/CarnetAduaneroProcessor.API

# Ejecutar en modo desarrollo
dotnet run

# O ejecutar en modo release
dotnet run --configuration Release
```

### Opción 3: VS Code

1. Abrir la carpeta del proyecto en VS Code
2. Instalar extensiones recomendadas:
   - C# Dev Kit
   - C# Extensions
3. Abrir terminal y ejecutar:
   ```bash
   dotnet run --project src/CarnetAduaneroProcessor.API
   ```

## 🌐 Acceder a la Aplicación

Una vez ejecutado, podrás acceder a:

- **Interfaz Web**: http://localhost:5000 o https://localhost:5001
- **API Swagger**: http://localhost:5000/swagger
- **Endpoint de Salud**: http://localhost:5000/health
- **Información del Sistema**: http://localhost:5000/info

## 📁 Estructura del Proyecto

```
CarnetAduaneroProcessor/
├── src/
│   ├── CarnetAduaneroProcessor.API/          # Web API principal
│   │   ├── Controllers/                      # Controladores
│   │   ├── Program.cs                        # Configuración principal
│   │   └── appsettings.json                  # Configuración
│   ├── CarnetAduaneroProcessor.Core/         # Lógica de negocio
│   │   ├── Models/                           # Modelos de datos
│   │   └── Services/                         # Interfaces de servicios
│   └── CarnetAduaneroProcessor.Infrastructure/ # Acceso a datos
│       ├── Data/                             # Contexto de base de datos
│       └── Services/                         # Implementaciones de servicios
├── tests/                                    # Tests (futuro)
└── docs/                                     # Documentación
```

## 🔍 Endpoints de la API

### Procesamiento de PDFs

- **POST** `/api/carnetaduanero/procesar` - Procesar un PDF
- **POST** `/api/carnetaduanero/procesar-lote` - Procesar múltiples PDFs

### Consulta de Datos

- **GET** `/api/carnetaduanero` - Listar todos los carnés (con paginación)
- **GET** `/api/carnetaduanero/{id}` - Obtener carné por ID
- **GET** `/api/carnetaduanero/estadisticas` - Estadísticas del sistema
- **GET** `/api/carnetaduanero/exportar` - Exportar datos a JSON

### Gestión

- **DELETE** `/api/carnetaduanero/{id}` - Eliminar carné

## 🧪 Probar el Sistema

### 1. Usando la Interfaz Web

1. Abrir http://localhost:5000 en tu navegador
2. Arrastrar y soltar un PDF de Carné Aduanero
3. Hacer clic en "Procesar Archivos"
4. Ver los resultados extraídos

### 2. Usando Swagger

1. Abrir http://localhost:5000/swagger
2. Probar los endpoints directamente desde la interfaz
3. Subir un archivo PDF usando el endpoint `/procesar`

### 3. Usando Postman o curl

```bash
# Procesar un PDF
curl -X POST "https://localhost:5001/api/carnetaduanero/procesar" \
  -H "Content-Type: multipart/form-data" \
  -F "file=@ruta/al/archivo.pdf"

# Obtener estadísticas
curl -X GET "https://localhost:5001/api/carnetaduanero/estadisticas"
```

## 🔧 Configuración Avanzada

### Cambiar Base de Datos

Editar `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=tu-servidor;Database=CarnetAduaneroProcessor;User Id=usuario;Password=password;"
  }
}
```

### Configurar Logging

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning"
      }
    }
  }
}
```

### Configurar CORS

```json
{
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:3000",
      "https://tu-dominio.com"
    ]
  }
}
```

## 🐛 Solución de Problemas

### Error de Base de Datos

```bash
# Verificar conexión
dotnet ef database update --project src/CarnetAduaneroProcessor.API --verbose

# Recrear base de datos
dotnet ef database drop --project src/CarnetAduaneroProcessor.API
dotnet ef database update --project src/CarnetAduaneroProcessor.API
```

### Error de Dependencias

```bash
# Limpiar y restaurar
dotnet clean
dotnet restore
dotnet build
```

### Error de Certificado HTTPS

```bash
# Generar certificado de desarrollo
dotnet dev-certs https --trust
```

### Error de Puerto

Si el puerto 5000/5001 está ocupado, puedes cambiar en `Properties/launchSettings.json`:

```json
{
  "profiles": {
    "CarnetAduaneroProcessor.API": {
      "applicationUrl": "https://localhost:5001;http://localhost:5000"
    }
  }
}
```

## 📊 Monitoreo

### Logs

Los logs se guardan en:
- **Consola**: Durante la ejecución
- **Archivo**: `Logs/log-YYYY-MM-DD.txt`

### Métricas

- **Endpoint de salud**: `/health`
- **Información del sistema**: `/info`
- **Estadísticas**: `/api/carnetaduanero/estadisticas`

## 🚀 Despliegue

### Desarrollo

```bash
dotnet run --project src/CarnetAduaneroProcessor.API
```

### Producción

```bash
dotnet publish --project src/CarnetAduaneroProcessor.API --configuration Release
```

### Docker (Futuro)

```dockerfile
# Dockerfile será agregado en futuras versiones
FROM mcr.microsoft.com/dotnet/aspnet:8.0
COPY bin/Release/net8.0/publish/ App/
WORKDIR /App
ENTRYPOINT ["dotnet", "CarnetAduaneroProcessor.API.dll"]
```

## 📞 Soporte

Si encuentras problemas:

1. Revisar los logs en la consola o archivos
2. Verificar la configuración en `appsettings.json`
3. Asegurar que .NET 8 SDK esté instalado correctamente
4. Verificar que SQL Server esté funcionando

## 🎯 Próximas Características

- [ ] Tests unitarios y de integración
- [ ] Interfaz web más avanzada
- [ ] Exportación a Excel/CSV
- [ ] Autenticación JWT
- [ ] Docker support
- [ ] CI/CD pipeline
- [ ] Dashboard de estadísticas
- [ ] Notificaciones por email 