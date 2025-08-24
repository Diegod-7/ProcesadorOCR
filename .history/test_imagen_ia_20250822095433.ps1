# Script de prueba para el nuevo endpoint de procesamiento de imágenes con IA
# Usando Gemma 3: 4B multimodal

Write-Host "=== PRUEBA DE PROCESAMIENTO DE IMÁGENES CON IA ===" -ForegroundColor Green
Write-Host "Modelo: Gemma 3: 4B (Multimodal)" -ForegroundColor Cyan
Write-Host ""

# Verificar que Ollama esté funcionando
Write-Host "Verificando que Ollama esté funcionando..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "http://localhost:11434/api/tags" -UseBasicParsing -ErrorAction Stop
    if ($response.StatusCode -eq 200) {
        Write-Host "✓ Ollama está funcionando" -ForegroundColor Green
    } else {
        Write-Host "✗ Ollama no está respondiendo correctamente" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "✗ Error conectando con Ollama: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Asegúrate de que Ollama esté ejecutándose con: ollama serve" -ForegroundColor Yellow
    exit 1
}

# Verificar que el modelo Gemma 3: 4B esté disponible
Write-Host ""
Write-Host "Verificando modelo Gemma 3: 4B..." -ForegroundColor Yellow
try {
    $models = ollama list
    if ($models -match "gemma3:4b") {
        Write-Host "✓ Modelo Gemma 3: 4B disponible" -ForegroundColor Green
    } else {
        Write-Host "✗ Modelo Gemma 3: 4B no encontrado" -ForegroundColor Red
        Write-Host "Descargando modelo..." -ForegroundColor Yellow
        ollama pull gemma3:4b
        Write-Host "✓ Modelo descargado" -ForegroundColor Green
    }
} catch {
    Write-Host "✗ Error verificando modelos: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Verificar que la API esté funcionando
Write-Host ""
Write-Host "Verificando que la API esté funcionando..." -ForegroundColor Yellow
try {
    $apiResponse = Invoke-WebRequest -Uri "http://localhost:5000/api/home" -UseBasicParsing -ErrorAction Stop
    if ($apiResponse.StatusCode -eq 200) {
        Write-Host "✓ API está funcionando" -ForegroundColor Green
    } else {
        Write-Host "✗ API no está respondiendo correctamente" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "✗ Error conectando con la API: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Asegúrate de que la aplicación esté ejecutándose" -ForegroundColor Yellow
    exit 1
}

Write-Host ""
Write-Host "=== CONFIGURACIÓN COMPLETADA ===" -ForegroundColor Green
Write-Host ""
Write-Host "Ahora puedes usar el nuevo endpoint:" -ForegroundColor Cyan
Write-Host ""
Write-Host "POST http://localhost:5000/api/carnetaduanero/procesar-imagen-ia" -ForegroundColor White
Write-Host ""
Write-Host "Características del nuevo sistema:" -ForegroundColor Cyan
Write-Host "✅ Procesa imágenes directamente (PNG, JPG, JPEG, GIF, BMP, TIFF, WEBP)" -ForegroundColor Green
Write-Host "✅ Usa Gemma 3: 4B multimodal para análisis visual" -ForegroundColor Green
Write-Host "✅ Extrae JSON completo sin necesidad de OCR previo" -ForegroundColor Green
Write-Host "✅ Soporta archivos hasta 20MB" -ForegroundColor Green
Write-Host "✅ Análisis inteligente de documentos chilenos" -ForegroundColor Green
Write-Host ""
Write-Host "Para probar, sube una imagen de documento a:" -ForegroundColor Yellow
Write-Host "http://localhost:5000/api/carnetaduanero/procesar-imagen-ia" -ForegroundColor White
Write-Host ""
Write-Host "¡El sistema ahora puede 'ver' tus documentos y extraer información directamente!" -ForegroundColor Green
