# Script para instalar y configurar Ollama en Windows
# Ollama es una herramienta gratuita para ejecutar modelos de IA localmente

Write-Host "=== INSTALACIÓN Y CONFIGURACIÓN DE OLLAMA ===" -ForegroundColor Green
Write-Host ""

# Verificar si Ollama ya está instalado
if (Get-Command ollama -ErrorAction SilentlyContinue) {
    Write-Host "✓ Ollama ya está instalado" -ForegroundColor Green
    $ollamaVersion = ollama --version
    Write-Host "Versión: $ollamaVersion" -ForegroundColor Cyan
} else {
    Write-Host "Instalando Ollama..." -ForegroundColor Yellow
    
    # Descargar e instalar Ollama
    $ollamaUrl = "https://ollama.ai/download/ollama-windows-amd64.exe"
    $installerPath = "$env:TEMP\ollama-installer.exe"
    
    try {
        Write-Host "Descargando Ollama..." -ForegroundColor Yellow
        Invoke-WebRequest -Uri $ollamaUrl -OutFile $installerPath
        
        Write-Host "Ejecutando instalador..." -ForegroundColor Yellow
        Start-Process -FilePath $installerPath -Wait -ArgumentList "/S"
        
        # Agregar Ollama al PATH si no está
        $ollamaPath = "C:\Program Files\Ollama"
        if ($env:PATH -notlike "*$ollamaPath*") {
            $env:PATH += ";$ollamaPath"
            [Environment]::SetEnvironmentVariable("PATH", $env:PATH, [EnvironmentVariableTarget]::User)
        }
        
        Write-Host "✓ Ollama instalado exitosamente" -ForegroundColor Green
    } catch {
        Write-Host "✗ Error instalando Ollama: $($_.Exception.Message)" -ForegroundColor Red
        exit 1
    } finally {
        if (Test-Path $installerPath) {
            Remove-Item $installerPath -Force
        }
    }
}

Write-Host ""
Write-Host "=== CONFIGURACIÓN DE OLLAMA ===" -ForegroundColor Green

# Iniciar el servicio de Ollama
Write-Host "Iniciando servicio de Ollama..." -ForegroundColor Yellow
try {
    Start-Process -FilePath "ollama" -ArgumentList "serve" -WindowStyle Hidden
    Start-Sleep -Seconds 5
    
    # Verificar que el servicio esté funcionando
    $response = Invoke-WebRequest -Uri "http://localhost:11434/api/tags" -UseBasicParsing -ErrorAction SilentlyContinue
    if ($response.StatusCode -eq 200) {
        Write-Host "✓ Servicio de Ollama iniciado correctamente" -ForegroundColor Green
    } else {
        Write-Host "✗ Error iniciando servicio de Ollama" -ForegroundColor Red
    }
} catch {
    Write-Host "✗ Error iniciando servicio de Ollama: $($_.Exception.Message)" -ForegroundColor Red
}

# Descargar modelo ligero
Write-Host ""
Write-Host "Descargando modelo de IA (llama3.2:3b)..." -ForegroundColor Yellow
Write-Host "Este proceso puede tomar varios minutos..." -ForegroundColor Cyan

try {
    ollama pull llama3.2:3b
    Write-Host "✓ Modelo descargado exitosamente" -ForegroundColor Green
} catch {
    Write-Host "✗ Error descargando modelo: $($_.Exception.Message)" -ForegroundColor Red
}

# Verificar modelos disponibles
Write-Host ""
Write-Host "=== MODELOS DISPONIBLES ===" -ForegroundColor Green
try {
    $models = ollama list
    Write-Host $models -ForegroundColor Cyan
} catch {
    Write-Host "✗ Error listando modelos: $($_.Exception.Message)" -ForegroundColor Red
}

# Probar el modelo
Write-Host ""
Write-Host "=== PRUEBA DEL MODELO ===" -ForegroundColor Green
Write-Host "Probando modelo con prompt simple..." -ForegroundColor Yellow

try {
    $testPrompt = "Escribe solo 'Hola Mundo'"
    $testResponse = ollama run llama3.2:3b $testPrompt
    Write-Host "Respuesta del modelo: $testResponse" -ForegroundColor Cyan
    Write-Host "✓ Modelo funcionando correctamente" -ForegroundColor Green
} catch {
    Write-Host "✗ Error probando modelo: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "=== CONFIGURACIÓN COMPLETADA ===" -ForegroundColor Green
Write-Host "Ollama está configurado y funcionando en http://localhost:11434" -ForegroundColor Cyan
Write-Host ""
Write-Host "Para usar Ollama manualmente:" -ForegroundColor Yellow
Write-Host "  ollama run llama3.2:3b 'Tu prompt aquí'" -ForegroundColor White
Write-Host ""
Write-Host "Para detener el servicio:" -ForegroundColor Yellow
Write-Host "  taskkill /F /IM ollama.exe" -ForegroundColor White
Write-Host ""
Write-Host "¡Ahora puedes probar tu aplicación con post-procesamiento de IA!" -ForegroundColor Green

