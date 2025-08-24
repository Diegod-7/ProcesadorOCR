# Script de prueba específico para Carné Aduanero con IA
# Usando Gemma 3: 4B multimodal

Write-Host "=== PRUEBA DE CARNÉ ADUANERO CON IA ===" -ForegroundColor Green
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
Write-Host "Ahora puedes procesar tu carné aduanero con IA:" -ForegroundColor Cyan
Write-Host ""
Write-Host "1. ENDPOINT PRINCIPAL (con fallback automático):" -ForegroundColor White
Write-Host "   POST http://localhost:5000/api/carnetaduanero/procesar" -ForegroundColor Yellow
Write-Host ""
Write-Host "2. ENDPOINT SOLO IA:" -ForegroundColor White
Write-Host "   POST http://localhost:5000/api/carnetaduanero/procesar-imagen-ia" -ForegroundColor Yellow
Write-Host ""
Write-Host "=== CAMPOS QUE DEBERÍA EXTRAER ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "✅ Titulo: ""CARNÉ ADUANERO""" -ForegroundColor Green
Write-Host "✅ Nombre: NOMBRE COMPLETO (nombre + apellidos)" -ForegroundColor Green
Write-Host "✅ RUT: Formato completo XX.XXX.XXX-X" -ForegroundColor Green
Write-Host "✅ NumeroCarne: Número completo (ej: N8687)" -ForegroundColor Green
Write-Host "✅ FechaEmision: DD.MM.YYYY" -ForegroundColor Green
Write-Host "✅ Resolucion: Número completo (ej: 01.42)" -ForegroundColor Green
Write-Host "✅ AgadCod: Códigos AGAD si están disponibles" -ForegroundColor Green
Write-Host "✅ ConfianzaExtraccion: Número decimal 0.0-1.0" -ForegroundColor Green
Write-Host ""
Write-Host "=== EJEMPLO DE USO ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "# Con cURL:" -ForegroundColor Yellow
Write-Host 'curl -X POST -F "file=@carne_aduanero.png" http://localhost:5000/api/carnetaduanero/procesar' -ForegroundColor White
Write-Host ""
Write-Host "# Con PowerShell:" -ForegroundColor Yellow
Write-Host '$form = @{ file = Get-Item "carne_aduanero.png" }' -ForegroundColor White
Write-Host 'Invoke-RestMethod -Uri "http://localhost:5000/api/carnetaduanero/procesar" -Method Post -Form $form' -ForegroundColor White
Write-Host ""
Write-Host "=== MEJORAS IMPLEMENTADAS ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "🔧 Prompt optimizado para carné aduanero" -ForegroundColor Green
Write-Host "🔧 Extracción de NOMBRE COMPLETO (no solo apellidos)" -ForegroundColor Green
Write-Host "🔧 RUT completo (no enmascarado)" -ForegroundColor Green
Write-Host "🔧 Número de carné completo" -ForegroundColor Green
Write-Host "🔧 Resolución completa" -ForegroundColor Green
Write-Host "🔧 Códigos AGAD" -ForegroundColor Green
Write-Host "🔧 Fallback automático si IA falla" -ForegroundColor Green
Write-Host ""
Write-Host "¡Ahora la IA debería extraer MUCHO mejor la información del carné!" -ForegroundColor Green
Write-Host ""
Write-Host "Para probar, sube una imagen de carné aduanero a cualquiera de los endpoints." -ForegroundColor Yellow
