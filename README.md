# 📋 Procesador de Carnés Aduaneros - .NET

## 🎯 Descripción del Proyecto

Sistema completo en .NET para procesar PDFs de Carnés Aduaneros y extraer automáticamente la información relevante, creando modelos de datos estructurados.

## 🏗️ Arquitectura del Proyecto

```
CarnetAduaneroProcessor/
├── src/
│   ├── CarnetAduaneroProcessor.API/          # Web API
│   ├── CarnetAduaneroProcessor.Core/         # Lógica de negocio
│   ├── CarnetAduaneroProcessor.Infrastructure/ # Acceso a datos
│   └── CarnetAduaneroProcessor.Web/          # Interfaz web
├── tests/
│   ├── CarnetAduaneroProcessor.UnitTests/    # Tests unitarios
│   └── CarnetAduaneroProcessor.IntegrationTests/ # Tests de integración
└── docs/                                     # Documentación
```

## 🚀 Tecnologías Utilizadas

- **.NET 8** - Framework principal
- **Entity Framework Core** - ORM para base de datos
- **Azure Form Recognizer** - IA para extracción de datos
- **iTextSharp** - Procesamiento de PDFs
- **AutoMapper** - Mapeo de objetos
- **FluentValidation** - Validación de datos
- **Serilog** - Logging
- **Swagger** - Documentación de API

## 📋 Funcionalidades

### ✅ Extracción de Datos
- Procesamiento automático de PDFs
- Extracción de texto con IA (Azure Form Recognizer)
- Reconocimiento de patrones específicos (RUT, fechas, etc.)
- Validación de datos extraídos

### ✅ Modelos de Datos
- **CarnetAduanero** - Modelo principal
- **Titular** - Información del titular
- **Documento** - Metadatos del documento
- **Procesamiento** - Historial de procesamiento

### ✅ API REST
- Endpoints para subir PDFs
- Consulta de documentos procesados
- Exportación de datos
- Estadísticas de procesamiento

### ✅ Interfaz Web
- Subida de archivos por drag & drop
- Visualización de resultados
- Historial de procesamiento
- Exportación a Excel/CSV

## 🎯 Campos Extraídos

- **Número de Carné** (ej: N8687)
- **Titular** (Nombre completo)
- **RUT** (ej: 15.970.128-K)
- **Fecha de Emisión** (ej: 17.01.2024)
- **Resolución** (ej: 0142)
- **AGAD Cod** (ej: E-12)
- **Entidad Emisora** (ej: anagena)
- **Estado del Documento**

## 🔧 Configuración

### Prerrequisitos
- .NET 8 SDK
- SQL Server o SQLite
- Azure Form Recognizer (opcional)
- Visual Studio 2022 o VS Code

### Instalación
```bash
# Clonar el repositorio
git clone [url-del-repositorio]

# Restaurar dependencias
dotnet restore

# Configurar base de datos
dotnet ef database update

# Ejecutar el proyecto
dotnet run --project src/CarnetAduaneroProcessor.API
```

## 📊 Base de Datos

### Tablas Principales
- **CarnetsAduaneros** - Documentos procesados
- **Titulares** - Información de titulares
- **Procesamientos** - Historial de procesamiento
- **Errores** - Log de errores

## 🔐 Seguridad

- Autenticación JWT
- Autorización por roles
- Validación de archivos
- Sanitización de datos
- Rate limiting

## 📈 Monitoreo

- Logging estructurado
- Métricas de procesamiento
- Alertas automáticas
- Dashboard de estadísticas

## 🧪 Testing

- Tests unitarios (xUnit)
- Tests de integración
- Tests de API
- Cobertura de código > 80%

## 📝 Licencia

MIT License - Ver archivo LICENSE para más detalles.

## 🤝 Contribución

1. Fork el proyecto
2. Crear rama feature (`git checkout -b feature/AmazingFeature`)
3. Commit cambios (`git commit -m 'Add AmazingFeature'`)
4. Push a la rama (`git push origin feature/AmazingFeature`)
5. Abrir Pull Request

## 📞 Soporte

- Email: soporte@empresa.com
- Documentación: [link-a-docs]
- Issues: [link-a-github-issues] 