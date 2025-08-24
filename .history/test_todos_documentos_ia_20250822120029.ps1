# Script de prueba para verificar que TODOS los tipos de documentos usen IA
# Usando Gemma 3: 4B multimodal

Write-Host "=== PRUEBA DE TODOS LOS TIPOS DE DOCUMENTOS CON IA ===" -ForegroundColor Green
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

Write-Host ""
Write-Host "=== VERIFICANDO TODOS LOS CONTROLADORES ===" -ForegroundColor Green

# Lista de controladores a verificar
$controladores = @(
    @{Nombre = "Carnet Aduanero"; Endpoint = "carnetaduanero/info"},
    @{Nombre = "Documento de Recepción"; Endpoint = "documentorecepcion/info"},
    @{Nombre = "Declaración de Ingreso"; Endpoint = "declaracioningreso/info"},
    @{Nombre = "Guía de Despacho"; Endpoint = "guiadespacho/info"},
    @{Nombre = "TACT/ADC"; Endpoint = "tactadc/info"},
    @{Nombre = "Selección de Aforo"; Endpoint = "seleccionaforo/info"},
    @{Nombre = "Comprobante de Transacción"; Endpoint = "comprobantetransaccion/info"}
)

$controladoresFuncionando = 0
$controladoresTotal = $controladores.Count

foreach ($controlador in $controladores) {
    Write-Host "Verificando $($controlador.Nombre)..." -ForegroundColor Yellow
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:5000/api/$($controlador.Endpoint)" -UseBasicParsing -ErrorAction Stop
        if ($response.StatusCode -eq 200) {
            $info = $response.Content | ConvertFrom-Json
            Write-Host "  ✓ $($controlador.Nombre) funcionando" -ForegroundColor Green
            Write-Host "    Versión: $($info.version)" -ForegroundColor Cyan
            Write-Host "    Método: $($info.metodoExtraccion)" -ForegroundColor Cyan
            
            # Verificar si usa IA
            if ($info.metodoExtraccion -like "*IA*" -or $info.metodoExtraccion -like "*Gemma*") {
                Write-Host "    ✓ IA implementada" -ForegroundColor Green
                $controladoresFuncionando++
            } else {
                Write-Host "    ⚠ Método tradicional" -ForegroundColor Yellow
            }
        } else {
            Write-Host "  ✗ $($controlador.Nombre) no responde" -ForegroundColor Red
        }
    } catch {
        Write-Host "  ✗ Error en $($controlador.Nombre): $($_.Exception.Message)" -ForegroundColor Red
    }
    Write-Host ""
}

Write-Host "=== RESUMEN FINAL ===" -ForegroundColor Green
Write-Host "Controladores con IA implementada: $controladoresFuncionando/$controladoresTotal" -ForegroundColor Cyan

if ($controladoresFuncionando -eq $controladoresTotal) {
    Write-Host "🎉 ¡TODOS los controladores están usando IA!" -ForegroundColor Green
} else {
    Write-Host "⚠ Algunos controladores aún no tienen IA implementada" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "=== ENDPOINTS PRINCIPALES PARA PROBAR ===" -ForegroundColor Green
Write-Host "1. Carnet Aduanero: POST /api/carnetaduanero/procesar" -ForegroundColor Cyan
Write-Host "2. Documento Recepción: POST /api/documentorecepcion/procesar" -ForegroundColor Cyan
Write-Host "3. Declaración Ingreso: POST /api/declaracioningreso/procesar" -ForegroundColor Cyan
Write-Host "4. Guía Despacho: POST /api/guiadespacho/procesar" -ForegroundColor Cyan
Write-Host "5. TACT/ADC: POST /api/tactadc/procesar" -ForegroundColor Cyan
Write-Host "6. Selección Aforo: POST /api/seleccionaforo/procesar" -ForegroundColor Cyan
Write-Host "7. Comprobante Transacción: POST /api/comprobantetransaccion/procesar" -ForegroundColor Cyan

Write-Host ""
Write-Host "Todos los endpoints soportan:" -ForegroundColor Yellow
Write-Host "  - Imágenes PNG, JPG, JPEG hasta 20MB" -ForegroundColor Cyan
Write-Host "  - Procesamiento automático con IA (Gemma 3: 4B)" -ForegroundColor Cyan
Write-Host "  - Fallback automático a OCR tradicional si la IA falla" -ForegroundColor Cyan
Write-Host "  - Campo TextoExtraido con JSON completo de la IA" -ForegroundColor Cyan
