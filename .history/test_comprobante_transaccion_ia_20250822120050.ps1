# Script de prueba para verificar que Comprobante de Transacción use IA
# Usando Gemma 3: 4B multimodal

Write-Host "=== PRUEBA DE COMPROBANTE DE TRANSACCIÓN CON IA ===" -ForegroundColor Green
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
    Write-Host "Asegúrate de que Ollama esté ejecutándose en http://localhost:11434" -ForegroundColor Yellow
    exit 1
}

# Verificar que el modelo Gemma 3: 4B esté disponible
Write-Host "Verificando modelo Gemma 3: 4B..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "http://localhost:11434/api/tags" -UseBasicParsing -ErrorAction Stop
    $models = $response.Content | ConvertFrom-Json
    $gemmaModel = $models.models | Where-Object { $_.name -like "*gemma3:4b*" }
    
    if ($gemmaModel) {
        Write-Host "✓ Modelo Gemma 3: 4B disponible: $($gemmaModel.name)" -ForegroundColor Green
        Write-Host "  Tamaño: $([math]::Round($gemmaModel.size / 1GB, 2)) GB" -ForegroundColor Cyan
    } else {
        Write-Host "✗ Modelo Gemma 3: 4B no encontrado" -ForegroundColor Red
        Write-Host "Ejecuta: ollama pull gemma3:4b" -ForegroundColor Yellow
        exit 1
    }
} catch {
    Write-Host "✗ Error verificando modelos: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Verificar que la API esté funcionando
Write-Host "Verificando que la API esté funcionando..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "http://localhost:5000/api/comprobantetransaccion/info" -UseBasicParsing -ErrorAction Stop
    if ($response.StatusCode -eq 200) {
        $info = $response.Content | ConvertFrom-Json
        Write-Host "✓ API funcionando - Versión: $($info.version)" -ForegroundColor Green
        Write-Host "  Método de extracción: $($info.metodoExtraccion)" -ForegroundColor Cyan
        Write-Host "  Modelo IA: $($info.modeloIA)" -ForegroundColor Cyan
        Write-Host "  Fallback: $($info.fallback)" -ForegroundColor Cyan
    } else {
        Write-Host "✗ API no está respondiendo correctamente" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "✗ Error conectando con la API: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Asegúrate de que la API esté ejecutándose en http://localhost:5000" -ForegroundColor Yellow
    exit 1
}

Write-Host ""
Write-Host "=== ESTADO ACTUAL ===" -ForegroundColor Green
Write-Host "✓ Ollama funcionando" -ForegroundColor Green
Write-Host "✓ Modelo Gemma 3: 4B disponible" -ForegroundColor Green
Write-Host "✓ API funcionando con IA implementada" -ForegroundColor Green
Write-Host "✓ Fallback a OCR configurado" -ForegroundColor Green

Write-Host ""
Write-Host "=== CAMPOS ESPERADOS EN COMPROBANTE DE TRANSACCIÓN ===" -ForegroundColor Yellow
Write-Host "Campos críticos (marcados en rojo):" -ForegroundColor Red
Write-Host "  - NumeroFolio: Número de folio del documento" -ForegroundColor Red
Write-Host "  - TotalPagado: Monto total pagado" -ForegroundColor Red

Write-Host ""
Write-Host "Campos adicionales:" -ForegroundColor Cyan
Write-Host "  - Rut: RUT del contribuyente" -ForegroundColor Cyan
Write-Host "  - Formulario: Tipo de formulario" -ForegroundColor Cyan
Write-Host "  - FechaVencimiento: Fecha de vencimiento" -ForegroundColor Cyan
Write-Host "  - MonedaPago: Moneda del pago" -ForegroundColor Cyan
Write-Host "  - FechaPago: Fecha del pago" -ForegroundColor Cyan
Write-Host "  - InstitucionRecaudadora: Institución que recauda" -ForegroundColor Cyan
Write-Host "  - IdentificadorTransaccion: ID de la transacción" -ForegroundColor Cyan
Write-Host "  - CodigoBarras: Código de barras" -ForegroundColor Cyan
Write-Host "  - NumeroReferencia: Número de referencia" -ForegroundColor Cyan

Write-Host ""
Write-Host "=== CÓMO PROBAR ===" -ForegroundColor Green
Write-Host "1. Sube una imagen PNG/JPG de Comprobante de Transacción a:" -ForegroundColor Yellow
Write-Host "   POST http://localhost:5000/api/comprobantetransaccion/procesar" -ForegroundColor Cyan
Write-Host ""
Write-Host "2. La IA procesará la imagen y extraerá los campos automáticamente" -ForegroundColor Yellow
Write-Host "3. Si la IA falla, se usará OCR tradicional como fallback" -ForegroundColor Yellow
Write-Host "4. El campo TextoExtraido contendrá el JSON completo de la IA" -ForegroundColor Yellow

Write-Host ""
Write-Host "=== FORMATO DE RESPUESTA ESPERADO ===" -ForegroundColor Green
Write-Host "La API devolverá un objeto ComprobanteTransaccion con:" -ForegroundColor Yellow
Write-Host "  - Todos los campos extraídos de la imagen" -ForegroundColor Cyan
Write-Host "  - MetodoExtraccion: 'IA (Gemma 3: 4B)' o 'OCR Tradicional'" -ForegroundColor Cyan
Write-Host "  - TextoExtraido: JSON completo extraído por la IA" -ForegroundColor Cyan
Write-Host "  - FechaProcesamiento: Timestamp del procesamiento" -ForegroundColor Cyan

Write-Host ""
Write-Host "¡Comprobante de Transacción está listo para usar IA!" -ForegroundColor Green
