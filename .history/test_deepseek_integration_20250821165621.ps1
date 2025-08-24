# Script de prueba para la integración de DeepSeek R1: 8B
# Autor: Asistente IA
# Fecha: $(Get-Date -Format "yyyy-MM-dd")

Write-Host "🧪 Probando integración de DeepSeek R1: 8B..." -ForegroundColor Green

# Verificar que Ollama esté ejecutándose
try {
    $response = Invoke-RestMethod -Uri "http://localhost:11434/api/tags" -Method Get -TimeoutSec 5
    Write-Host "✅ Ollama está ejecutándose" -ForegroundColor Green
} catch {
    Write-Host "❌ Ollama no está ejecutándose. Ejecuta: ollama serve" -ForegroundColor Red
    exit 1
}

# Verificar que DeepSeek R1: 8B esté disponible
try {
    $models = ollama list
    if ($models -match "deepseek-r1:8b") {
        Write-Host "✅ DeepSeek R1: 8B está disponible" -ForegroundColor Green
    } else {
        Write-Host "❌ DeepSeek R1: 8B no está disponible. Ejecuta: ollama pull deepseek-r1:8b" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "❌ Error verificando modelos: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Crear un JSON de prueba
$jsonPrueba = @"
{
  "numeroFolio": "",
  "totalPagado": 0,
  "formulario": null,
  "fechaVencimiento": "",
  "monedaPago": "",
  "fechaPago": "",
  "institucionRecaudadora": "",
  "identificadorTransaccion": "",
  "textoExtraido": "COMPROBANTE DE TRANSACCIÓN N° 4560010758 FORMULARIO 15 FECHA DE VENCIMIENTO: 09/07/2025 MONEDA: CLP TOTAL PAGADO: $8.153.962 FECHA DE PAGO: 24/06/2025 17:44:12 INSTITUCIÓN: BANCO ITAU IDENTIFICADOR: 02847341-57208059"
}
"@

# Crear un prompt de prueba similar al que usa la aplicación
$promptPrueba = @"
Eres un asistente experto en procesamiento de documentos chilenos usando DeepSeek R1: 8B. Tu tarea es analizar un JSON y completar todos los campos faltantes basándote en el texto extraído por OCR.

DOCUMENTO JSON ACTUAL:
$jsonPrueba

TEXTO EXTRAÍDO POR OCR:
COMPROBANTE DE TRANSACCIÓN N° 4560010758 FORMULARIO 15 FECHA DE VENCIMIENTO: 09/07/2025 MONEDA: CLP TOTAL PAGADO: $8.153.962 FECHA DE PAGO: 24/06/2025 17:44:12 INSTITUCIÓN: BANCO ITAU IDENTIFICADOR: 02847341-57208059

INSTRUCCIONES DETALLADAS:
1. Analiza cuidadosamente el texto OCR para identificar TODOS los campos disponibles
2. Completa SOLO los campos que estén vacíos, sean null, o contengan valores por defecto
3. Mantén el formato JSON exacto y la estructura original
4. Para fechas, usa el formato ISO 8601 (YYYY-MM-DDTHH:mm:ss) o (YYYY-MM-DD) según el contexto
5. Para números, usa el formato decimal sin comas ni puntos de miles
6. Para monedas, usa el formato numérico sin símbolos de moneda
7. NO modifiques campos que ya tengan valores válidos
8. Si un campo no se puede extraer del texto, déjalo como null
9. Usa tu capacidad de razonamiento para inferir campos relacionados

RESPONDE SOLO CON EL JSON COMPLETADO, sin explicaciones adicionales, sin markdown, sin texto extra.
El JSON debe ser válido y parseable inmediatamente.
"@

Write-Host "📝 Enviando prompt de prueba a DeepSeek R1: 8B..." -ForegroundColor Yellow

# Crear el payload para Ollama
$payload = @{
    model = "deepseek-r1:8b"
    prompt = $promptPrueba
    stream = $false
    options = @{
        temperature = 0.05
        top_p = 0.95
        top_k = 40
        max_tokens = 4000
        repeat_penalty = 1.1
        num_predict = 4000
    }
}

try {
    # Convertir a JSON
    $jsonPayload = $payload | ConvertTo-Json -Depth 10
    $content = [System.Text.Encoding]::UTF8.GetBytes($jsonPayload)
    
    # Llamar a Ollama
    $response = Invoke-RestMethod -Uri "http://localhost:11434/api/generate" -Method Post -Body $content -ContentType "application/json" -TimeoutSec 60
    
    Write-Host "✅ Respuesta recibida de DeepSeek R1: 8B" -ForegroundColor Green
    Write-Host "📊 Respuesta:" -ForegroundColor Cyan
    Write-Host $response.response -ForegroundColor White
    
    # Verificar si la respuesta es JSON válido
    try {
        $jsonRespuesta = $response.response | ConvertFrom-Json -ErrorAction Stop
        Write-Host "✅ Respuesta es JSON válido" -ForegroundColor Green
        
        # Mostrar campos extraídos
        Write-Host "🔍 Campos extraídos:" -ForegroundColor Cyan
        $jsonRespuesta.PSObject.Properties | ForEach-Object {
            if ($_.Value -and $_.Value -ne "" -and $_.Value -ne "0" -and $_.Value -ne "null") {
                Write-Host "   ✅ $($_.Name): $($_.Value)" -ForegroundColor Green
            } else {
                Write-Host "   ❌ $($_.Name): $($_.Value)" -ForegroundColor Red
            }
        }
        
    } catch {
        Write-Host "⚠️ La respuesta no es JSON válido: $($_.Exception.Message)" -ForegroundColor Yellow
        Write-Host "💡 Esto puede indicar que el modelo necesita ajustes en el prompt" -ForegroundColor Cyan
    }
    
} catch {
    Write-Host "❌ Error llamando a Ollama: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        Write-Host "   Código de estado: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
    }
}

Write-Host "`n🎯 Prueba completada!" -ForegroundColor Green
Write-Host "💡 Si la respuesta es correcta, la integración está funcionando perfectamente" -ForegroundColor Cyan
Write-Host "🔧 Si hay problemas, revisa los logs y ajusta el prompt según sea necesario" -ForegroundColor Yellow
