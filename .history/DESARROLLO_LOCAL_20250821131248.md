# 🖥️ Desarrollo Local con Tesseract OCR

## 📋 Resumen

Guía completa para ejecutar tu proyecto con **Tesseract OCR** localmente en Windows, sin necesidad de Docker.

## 🚀 Pasos para Desarrollo Local

### **1. Instalar Tesseract OCR**

#### **Opción A: Instalador de Windows (Recomendado)**
1. Descargar desde: https://github.com/UB-Mannheim/tesseract/wiki
2. Ejecutar el `.exe` 
3. **¡Importante!** Anotar la ruta de instalación
4. Asegurarse de que se instalen los idiomas español e inglés

#### **Opción B: Chocolatey**
```powershell
choco install tesseract
```

### **2. Configurar el Proyecto**

#### **Ejecutar Script de Configuración**
```powershell
.\setup_tesseract_local.ps1
```

#### **Configuración Manual**
Si prefieres configurar manualmente, edita `appsettings.Development.json`:

```json
{
  "Tesseract": {
    "TessdataPath": "C:\\Program Files\\Tesseract-OCR\\tessdata",
    "PrimaryLanguage": "spa",
    "FallbackLanguage": "eng"
  }
}
```

**Nota:** Ajusta la ruta según donde instalaste Tesseract.

### **3. Ejecutar la Aplicación**

#### **Desde la Raíz del Proyecto**
```bash
cd src/CarnetAduaneroProcessor.API
dotnet run
```

#### **O desde la Raíz**
```bash
dotnet run --project src/CarnetAduaneroProcessor.API
```

### **4. Verificar Funcionamiento**

La aplicación debería:
- ✅ Inicializar Tesseract con idioma español
- ✅ Mostrar logs de inicialización exitosa
- ✅ Estar disponible en `https://localhost:7000` o `http://localhost:5000`

## 🔧 Configuración de Entorno

### **Variables de Entorno (Opcional)**
```bash
set ASPNETCORE_ENVIRONMENT=Development
set TESSERACT_PATH=C:\Program Files\Tesseract-OCR
```

### **Archivos de Configuración**
- `appsettings.json` - Configuración base
- `appsettings.Development.json` - Configuración local (sobrescribe la base)
- `appsettings.Docker.json` - Configuración para Docker

## 🧪 Pruebas Locales

### **1. Probar OCR con Tesseract**
```csharp
// En tu controlador o servicio
var resultado = await _carnetService.ProcesarImagenAsync(imagen);
```

### **2. Verificar Logs**
Busca estos mensajes en la consola:
- ✅ "Tesseract inicializado con idioma español"
- ✅ "Texto extraído exitosamente con Tesseract"

### **3. Probar Fallback a Azure**
Si Tesseract falla, deberías ver:
- ⚠️ "Tesseract falló, usando Azure Computer Vision como fallback"

## 🛠️ Solución de Problemas

### **Tesseract No Se Inicializa**
1. **Verificar instalación:**
   ```powershell
   tesseract --version
   ```

2. **Verificar archivos de idioma:**
   ```powershell
   dir "C:\Program Files\Tesseract-OCR\tessdata\*.traineddata"
   ```

3. **Verificar permisos** del directorio tessdata

### **Error de Ruta**
1. **Verificar ruta en configuración:**
   ```json
   "TessdataPath": "C:\\Program Files\\Tesseract-OCR\\tessdata"
   ```

2. **Usar doble backslash** en la configuración JSON

### **Error de Compilación**
1. **Restaurar paquetes NuGet:**
   ```bash
   dotnet restore
   ```

2. **Limpiar y reconstruir:**
   ```bash
   dotnet clean
   dotnet build
   ```

## 📁 Estructura de Archivos Local

```
ProcesadorOCR/
├── src/
│   └── CarnetAduaneroProcessor.API/
│       ├── appsettings.Development.json  ← Configuración local
│       ├── Uploads/                      ← Archivos subidos
│       └── Logs/                         ← Logs de la aplicación
├── setup_tesseract_local.ps1             ← Script de configuración
└── DESARROLLO_LOCAL.md                   ← Este archivo
```

## 🎯 Ventajas del Desarrollo Local

- ✅ **Sin contenedores** - Más rápido para desarrollo
- ✅ **Debugging directo** - Puntos de interrupción funcionan
- ✅ **Cambios inmediatos** - Hot reload automático
- ✅ **Recursos locales** - No depende de servicios externos
- ✅ **Tesseract nativo** - Mejor rendimiento en Windows

## 🔄 Flujo de Desarrollo

1. **Desarrollar** con Tesseract local
2. **Probar** funcionalidad básica
3. **Comitar** cambios al repositorio
4. **Desplegar** con Docker (incluye Tesseract)

## 🚀 Próximos Pasos

1. **Ejecutar** `setup_tesseract_local.ps1`
2. **Verificar** que Tesseract esté instalado
3. **Ejecutar** la aplicación con `dotnet run`
4. **Probar** OCR con documentos reales
5. **¡Disfrutar** del OCR gratuito local!

---

**¡Ahora puedes desarrollar y probar Tesseract localmente sin costos! 🎉**
