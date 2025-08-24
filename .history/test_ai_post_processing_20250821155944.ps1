# Script para probar el post-procesamiento con IA
Write-Host "=== PRUEBA DE POST-PROCESAMIENTO CON IA ===" -ForegroundColor Green

# Configurar tu API key de OpenAI
$openaiApiKey = "tu-api-key-aqui"
if ($openaiApiKey -eq "tu-api-key-aqui") {
    Write-Host "⚠️  IMPORTANTE: Configura tu API key de OpenAI en la variable \$openaiApiKey" -ForegroundColor Yellow
    Write-Host "   Puedes obtener una en: https://platform.openai.com/api-keys" -ForegroundColor Cyan
    Write-Host ""
}

# JSON de ejemplo del documento
$documentoJson = @"
{
  "id": 0,
  "numeroFolio": "",
  "totalPagado": 0,
  "rut": "77591058-5",
  "formulario": "",
  "fechaVencimiento": null,
  "monedaPago": "",
  "fechaPago": null,
  "institucionRecaudadora": "Identificador de Transacci",
  "identificadorTransaccion": "77591058-5",
  "codigoBarras": "06240508201625063001504715",
  "numeroReferencia": "06240508201625063001504715",
  "nombreArchivo": "4.comprobante_transaccion.png",
  "hashArchivo": "b1656485c8a0a76fc907db3a62dc85e698be60930df7390dc23298f5503a836d",
  "metodoExtraccion": "Tesseract OCR",
  "textoExtraido": "*TGR COMPROBANTE DE TRANSACCION Tesorería Genera de la República Rut - Rol Formulario Folio Vencimiento Moneda de Pago Total Pagado Fecha Pago Institución Recaudadora Identificador de Transacción 77591058-5 15 4560010758 09-07-2025 CLP 8.153.962 24-06-2025 17:44:12 BANCO ITAU 02847341-57208059 No válido para pago en Instituciones Recaudadoras 06240508201625063001504715 77",
  "confianzaExtraccion": 0.8,
  "fechaProcesamiento": "2025-08-21T19:42:23.7352219Z",
  "comentarios": "No se pudieron extraer todos los campos requeridos del documento de Comprobante de Transacción",
  "esValido": false
}
"@

# Texto extraído por OCR
$textoOcr = "*TGR COMPROBANTE DE TRANSACCION Tesorería Genera de la República Rut - Rol Formulario Folio Vencimiento Moneda de Pago Total Pagado Fecha Pago Institución Recaudadora Identificador de Transacción 77591058-5 15 4560010758 09-07-2025 CLP 8.153.962 24-06-2025 17:44:12 BANCO ITAU 02847341-57208059 No válido para pago en Instituciones Recaudadoras 06240508201625063001504715 77"

Write-Host "📄 Documento original:" -ForegroundColor Cyan
Write-Host $documentoJson
Write-Host ""

Write-Host "🔍 Texto OCR extraído:" -ForegroundColor Cyan
Write-Host $textoOcr
Write-Host ""

# Función para probar con OpenAI
function Test-OpenAI {
    param($apiKey, $documento, $texto)
    
    if ($apiKey -eq "tu-api-key-aqui") {
        Write-Host "❌ API key no configurada" -ForegroundColor Red
        return
    }
    
    try {
        Write-Host "🤖 Probando con OpenAI..." -ForegroundColor Yellow
        
        $headers = @{
            "Authorization" = "Bearer $apiKey"
            "Content-Type" = "application/json"
        }
        
        $body = @{
            model = "gpt-3.5-turbo"
            messages = @(
                @{
                    role = "system"
                    content = "Eres un asistente experto en procesamiento de documentos que responde solo con JSON válido."
                },
                @{
                    role = "user"
                    content = "Eres un experto en procesamiento de documentos chilenos. Analiza el siguiente JSON de un Comprobante de Transacción y el texto extraído por OCR, y completa los campos faltantes.

JSON del documento:
$documento

Texto extraído por OCR:
$texto

Instrucciones:
1. Analiza el texto OCR para identificar los valores de los campos vacíos
2. Completa solo los campos que estén vacíos o sean null
3. Mantén el formato JSON exacto
4. Para fechas, usa el formato ISO 8601 (YYYY-MM-DD o YYYY-MM-DDTHH:mm:ss)
5. Para números, mantén el formato original
6. No inventes información, solo extrae lo que está en el texto

Responde SOLO con el JSON completado, sin explicaciones adicionales."
                }
            )
            temperature = 0.1
            max_tokens = 1000
        } | ConvertTo-Json -Depth 10
        
        $response = Invoke-RestMethod -Uri "https://api.openai.com/v1/chat/completions" -Method Post -Headers $headers -Body $body
        
        if ($response.choices -and $response.choices[0].message.content) {
            Write-Host "✅ Respuesta de OpenAI:" -ForegroundColor Green
            Write-Host $response.choices[0].message.content -ForegroundColor White
        } else {
            Write-Host "❌ No se pudo obtener respuesta de OpenAI" -ForegroundColor Red
        }
    }
    catch {
        Write-Host "❌ Error llamando a OpenAI: $($_.Exception.Message)" -ForegroundColor Red
    }
}

# Función para probar con Ollama (gratuito)
function Test-Ollama {
    param($documento, $texto)
    
    try {
        Write-Host "🤖 Probando con Ollama (gratuito)..." -ForegroundColor Yellow
        
        $body = @{
            model = "llama3.1:8b"
            prompt = "Eres un experto en procesamiento de documentos chilenos. Analiza el siguiente JSON de un Comprobante de Transacción y el texto extraído por OCR, y completa los campos faltantes.

JSON del documento:
$documento

Texto extraído por OCR:
$texto

Instrucciones:
1. Analiza el texto OCR para identificar los valores de los campos vacíos
2. Completa solo los campos que estén vacíos o sean null
3. Mantén el formato JSON exacto
4. Para fechas, usa el formato ISO 8601 (YYYY-MM-DD o YYYY-MM-DDTHH:mm:ss)
5. Para números, mantén el formato original
6. No inventes información, solo extrae lo que está en el texto

Responde SOLO con el JSON completado, sin explicaciones adicionales."
            stream = $false
            options = @{
                temperature = 0.1
                top_p = 0.9
                max_tokens = 1000
            }
        } | ConvertTo-Json -Depth 10
        
        try {
            $response = Invoke-RestMethod -Uri "http://localhost:11434/api/generate" -Method Post -Body $body -ContentType "application/json"
            
            if ($response.response) {
                Write-Host "✅ Respuesta de Ollama:" -ForegroundColor Green
                Write-Host $response.response -ForegroundColor White
            } else {
                Write-Host "❌ No se pudo obtener respuesta de Ollama" -ForegroundColor Red
            }
        }
        catch {
            Write-Host "❌ Ollama no está ejecutándose en localhost:11434" -ForegroundColor Red
            Write-Host "   Para usar Ollama:" -ForegroundColor Cyan
            Write-Host "   1. Instala Ollama desde: https://ollama.ai" -ForegroundColor Cyan
            Write-Host "   2. Ejecuta: ollama run llama3.1:8b" -ForegroundColor Cyan
            Write-Host "   3. Ollama se ejecutará en http://localhost:11434" -ForegroundColor Cyan
        }
    }
    catch {
        Write-Host "❌ Error llamando a Ollama: $($_.Exception.Message)" -ForegroundColor Red
    }
}

# Ejecutar pruebas
Write-Host "🚀 Iniciando pruebas..." -ForegroundColor Green
Write-Host ""

Test-OpenAI -apiKey $openaiApiKey -documento $documentoJson -texto $textoOcr
Write-Host ""

Test-Ollama -documento $documentoJson -texto $textoOcr
Write-Host ""

Write-Host "=== FIN DE PRUEBAS ===" -ForegroundColor Green
Write-Host ""
Write-Host "💡 Consejos:" -ForegroundColor Cyan
Write-Host "   - OpenAI: Muy barato (~$0.01 por documento), muy preciso" -ForegroundColor White
Write-Host "   - Ollama: Gratuito, requiere instalación local, menos preciso" -ForegroundColor White
Write-Host "   - Para producción, usa OpenAI. Para desarrollo/pruebas, usa Ollama" -ForegroundColor White
