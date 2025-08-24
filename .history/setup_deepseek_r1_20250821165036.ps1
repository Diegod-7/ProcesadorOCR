# Script para configurar y probar DeepSeek R1: 8B con Ollama
# Autor: Asistente IA
# Fecha: $(Get-Date -Format "yyyy-MM-dd")

Write-Host "🚀 Configurando DeepSeek R1: 8B con Ollama..." -ForegroundColor Green

# Verificar si Ollama está instalado
try {
    $ollamaVersion = ollama --version
    Write-Host "✅ Ollama está instalado: $ollamaVersion" -ForegroundColor Green
} catch {
    Write-Host "❌ Ollama no está instalado. Por favor instala Ollama desde https://ollama.ai" -ForegroundColor Red
    exit 1
}

# Verificar si Ollama está ejecutándose
try {
    $response = Invoke-RestMethod -Uri "http://localhost:11434/api/tags" -Method Get -TimeoutSec 5
    Write-Host "✅ Ollama está ejecutándose en http://localhost:11434" -ForegroundColor Green
} catch {
    Write-Host "❌ Ollama no está ejecutándose. Iniciando servicio..." -ForegroundColor Yellow
    Start-Process "ollama" -ArgumentList "serve" -WindowStyle Hidden
    Start-Sleep -Seconds 10
    
    # Verificar nuevamente
    try {
        $response = Invoke-RestMethod -Uri "http://localhost:11434/api/tags" -Method Get -TimeoutSec 5
        Write-Host "✅ Ollama iniciado correctamente" -ForegroundColor Green
    } catch {
        Write-Host "❌ No se pudo iniciar Ollama. Por favor verifica la instalación." -ForegroundColor Red
        exit 1
    }
}

# Descargar DeepSeek R1: 8B
Write-Host "📥 Descargando DeepSeek R1: 8B..." -ForegroundColor Yellow
try {
    ollama pull deepseek-r1:8b
    Write-Host "✅ DeepSeek R1: 8B descargado correctamente" -ForegroundColor Green
} catch {
    Write-Host "❌ Error descargando DeepSeek R1: 8b: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Verificar que el modelo esté disponible
Write-Host "🔍 Verificando modelos disponibles..." -ForegroundColor Yellow
try {
    $models = ollama list
    if ($models -match "deepseek-r1:8b") {
        Write-Host "✅ DeepSeek R1: 8B está disponible" -ForegroundColor Green
    } else {
        Write-Host "❌ DeepSeek R1: 8B no está disponible" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "❌ Error verificando modelos: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Probar el modelo con una consulta simple
Write-Host "🧪 Probando DeepSeek R1: 8B..." -ForegroundColor Yellow
try {
    $testPrompt = "Eres un asistente experto en procesamiento de documentos. Responde solo con 'OK' si entiendes."
    $testResponse = ollama run deepseek-r1:8b $testPrompt
    
    if ($testResponse -match "OK") {
        Write-Host "✅ DeepSeek R1: 8B responde correctamente" -ForegroundColor Green
    } else {
        Write-Host "⚠️ DeepSeek R1: 8B responde, pero la respuesta no es la esperada: $testResponse" -ForegroundColor Yellow
    }
} catch {
    Write-Host "❌ Error probando el modelo: $($_.Exception.Message)" -ForegroundColor Red
}

# Mostrar información del sistema
Write-Host "📊 Información del sistema:" -ForegroundColor Cyan
Write-Host "   - Modelo: DeepSeek R1: 8B" -ForegroundColor White
Write-Host "   - URL: http://localhost:11434" -ForegroundColor White
Write-Host "   - Endpoint: /api/generate" -ForegroundColor White

# Crear archivo de configuración de ejemplo
$configExample = @"
# Configuración de Ollama para DeepSeek R1: 8B
# Archivo: appsettings.json

{
  "Ollama": {
    "BaseUrl": "http://localhost:11434",
    "Model": "deepseek-r1:8b"
  }
}
"@

$configExample | Out-File -FilePath "ollama_config_example.json" -Encoding UTF8
Write-Host "📝 Archivo de configuración de ejemplo creado: ollama_config_example.json" -ForegroundColor Green

Write-Host "🎉 Configuración completada!" -ForegroundColor Green
Write-Host "💡 Ahora puedes usar DeepSeek R1: 8B en tu aplicación Cursor con Ollama" -ForegroundColor Cyan
Write-Host "🔗 Para más información: https://ollama.ai/library/deepseek-r1" -ForegroundColor Blue
