# Integración con Ollama para Post-Procesamiento de IA

## ¿Qué es Ollama?

Ollama es una herramienta **gratuita** que te permite ejecutar modelos de IA localmente en tu computadora. Es perfecta para nuestro caso de uso porque:

- ✅ **Completamente gratuito** - No hay costos por uso
- ✅ **Ejecución local** - No envías datos a servidores externos
- ✅ **Sin límites** - Puedes hacer tantas consultas como quieras
- ✅ **Fácil de usar** - API REST simple
- ✅ **Modelos ligeros** - Funciona bien en computadoras normales

## Cómo Funciona

1. **Tesseract extrae el texto** del documento
2. **Nuestro servicio analiza** el texto y extrae campos
3. **Si faltan campos**, se envía a Ollama para completarlos
4. **Ollama analiza** el texto OCR y completa el JSON
5. **Se combina** la información original con la completada por IA

## Instalación

### 1. Ejecutar el Script de Instalación

```powershell
# Ejecutar como administrador
.\setup_ollama.ps1
```

El script:
- Descarga e instala Ollama
- Inicia el servicio
- Descarga el modelo `llama3.2:3b` (ligero y rápido)
- Verifica que todo funcione

### 2. Instalación Manual (Alternativa)

Si prefieres instalar manualmente:

1. Descargar desde: https://ollama.ai/download
2. Instalar el ejecutable
3. Abrir PowerShell y ejecutar:
   ```powershell
   ollama serve
   ollama pull llama3.2:3b
   ```

## Configuración

### appsettings.json

```json
{
  "Ollama": {
    "BaseUrl": "http://localhost:11434",
    "Model": "llama3.2:3b",
    "Enabled": true
  }
}
```

### Variables de Entorno (Opcional)

```bash
OLLAMA__BASEURL=http://localhost:11434
OLLAMA__MODEL=llama3.2:3b
OLLAMA__ENABLED=true
```

## Uso

### 1. Iniciar Ollama

```powershell
# Iniciar el servicio
ollama serve

# Verificar que esté funcionando
curl http://localhost:11434/api/tags
```

### 2. Probar el Modelo

```powershell
# Prueba simple
ollama run llama3.2:3b "Escribe solo 'Hola Mundo'"

# Prueba con JSON
ollama run llama3.2:3b "Completa este JSON: {\"campo\": \"\"}"
```

### 3. En tu Aplicación

El servicio se integra automáticamente. Cuando proceses un documento:

1. Se extrae texto con Tesseract
2. Se procesa con regex
3. Si faltan campos, se envía a Ollama
4. Ollama completa los campos faltantes
5. Se devuelve el documento completo

## Ejemplo de Prompt

Ollama recibe un prompt como este:

```
Eres un asistente experto en procesamiento de documentos chilenos. 
Tu tarea es completar un JSON de Comprobante de Transacción basándote en el texto extraído por OCR.

DOCUMENTO JSON ACTUAL:
{"numeroFolio": "", "totalPagado": 0, ...}

TEXTO EXTRAÍDO POR OCR:
*TGR COMPROBANTE DE TRANSACCION Tesorería Genera de la República...

INSTRUCCIONES:
1. Analiza el texto OCR y extrae los campos faltantes del JSON
2. Completa solo los campos que estén vacíos o sean null
3. Mantén el formato JSON exacto
4. Para fechas, usa el formato ISO 8601
5. Para números, usa el formato decimal sin comas ni puntos de miles

RESPONDE SOLO CON EL JSON COMPLETADO, sin explicaciones adicionales.
```

## Modelos Disponibles

### Recomendados para Nuestro Caso

- **`llama3.2:3b`** - Ligero, rápido, bueno para tareas simples ✅
- **`llama3.2:8b`** - Mejor calidad, más lento
- **`llama3.2:70b`** - Excelente calidad, requiere más recursos

### Cambiar Modelo

```powershell
# Descargar nuevo modelo
ollama pull llama3.2:8b

# Cambiar en appsettings.json
"Model": "llama3.2:8b"
```

## Monitoreo

### Ver Logs

```powershell
# Ver logs de Ollama
ollama logs

# Ver modelos disponibles
ollama list

# Ver uso de recursos
ollama ps
```

### API Endpoints

- **`GET /api/tags`** - Listar modelos
- **`POST /api/generate`** - Generar texto
- **`GET /api/ps`** - Estado del servicio

## Troubleshooting

### Error: "Connection refused"

```powershell
# Verificar que Ollama esté corriendo
ollama serve

# Verificar puerto
netstat -an | findstr 11434
```

### Error: "Model not found"

```powershell
# Descargar modelo
ollama pull llama3.2:3b

# Verificar modelos
ollama list
```

### Error: "Out of memory"

```powershell
# Usar modelo más ligero
ollama pull llama3.2:3b

# Cambiar en configuración
"Model": "llama3.2:3b"
```

### Rendimiento Lento

```powershell
# Usar modelo más ligero
ollama pull llama3.2:3b

# Ajustar parámetros en el código
"temperature": 0.1,
"max_tokens": 1000
```

## Ventajas de Ollama

### vs Azure OpenAI
- ✅ **Gratis** vs $0.002 por 1K tokens
- ✅ **Local** vs Dependencia de internet
- ✅ **Sin límites** vs Rate limits
- ✅ **Privacidad** vs Datos en la nube

### vs Otros Modelos Locales
- ✅ **Fácil instalación** vs Configuración compleja
- ✅ **API REST** vs Integración difícil
- ✅ **Modelos optimizados** vs Modelos genéricos
- ✅ **Comunidad activa** vs Soporte limitado

## Costos

- **Ollama**: Completamente gratuito
- **Modelos**: Descarga gratuita
- **Uso**: Sin límites ni costos
- **Recursos**: Solo CPU/RAM de tu máquina

## Próximos Pasos

1. **Ejecutar** `setup_ollama.ps1`
2. **Verificar** que Ollama funcione
3. **Probar** tu aplicación
4. **Ajustar** prompts si es necesario
5. **Optimizar** parámetros del modelo

¡Con Ollama tienes IA gratuita e ilimitada para mejorar la extracción de documentos!
