#!/usr/bin/env pwsh

param(
    [switch]$Build,
    [switch]$Logs,
    [switch]$Stop,
    [switch]$Clean,
    [switch]$Status,
    [switch]$Restart
)

Write-Host "🚀 Procesador OCR con Ollama - Script de Docker" -ForegroundColor Green
Write-Host "=================================================" -ForegroundColor Green

if ($Build) {
    Write-Host "🔨 Construyendo imagen Docker con Ollama..." -ForegroundColor Yellow
    
    # Verificar si Docker está ejecutándose
    if (-not (docker info 2>$null)) {
        Write-Host "❌ Docker no está ejecutándose. Inicia Docker Desktop primero." -ForegroundColor Red
        exit 1
    }
    
    # Detener contenedores existentes
    Write-Host "🛑 Deteniendo contenedores existentes..." -ForegroundColor Yellow
    docker-compose down 2>$null
    
    # Construir y ejecutar
    Write-Host "🚀 Construyendo y ejecutando..." -ForegroundColor Yellow
    docker-compose up --build -d
    
    Write-Host "✅ Construcción completada!" -ForegroundColor Green
    Write-Host "⏳ Esperando a que Ollama se inicialice (esto puede tomar 5-10 minutos)..."
    
    # Esperar y mostrar logs
    Start-Sleep -Seconds 30
    Write-Host "📋 Mostrando logs de inicialización..." -ForegroundColor Yellow
    docker-compose logs -f --tail=50
}

elseif ($Logs) {
    Write-Host "📋 Mostrando logs..." -ForegroundColor Yellow
    docker-compose logs -f
}

elseif ($Stop) {
    Write-Host "🛑 Deteniendo servicios..." -ForegroundColor Yellow
    docker-compose down
    Write-Host "✅ Servicios detenidos" -ForegroundColor Green
}

elseif ($Clean) {
    Write-Host "🧹 Limpiando todo..." -ForegroundColor Yellow
    docker-compose down -v --remove-orphans
    docker system prune -f
    docker volume prune -f
    Write-Host "✅ Limpieza completada" -ForegroundColor Green
}

elseif ($Status) {
    Write-Host "📊 Estado de los servicios..." -ForegroundColor Yellow
    docker-compose ps
    Write-Host ""
    Write-Host "🔍 Verificando Ollama..." -ForegroundColor Yellow
    try {
        $response = Invoke-RestMethod -Uri "http://localhost:11434/api/tags" -Method GET -TimeoutSec 5
        Write-Host "✅ Ollama está funcionando" -ForegroundColor Green
        Write-Host "📋 Modelos disponibles:" -ForegroundColor Cyan
        $response.models | ForEach-Object { Write-Host "  - $($_.name)" -ForegroundColor White }
    }
    catch {
        Write-Host "❌ Ollama no está respondiendo" -ForegroundColor Red
    }
}

elseif ($Restart) {
    Write-Host "🔄 Reiniciando servicios..." -ForegroundColor Yellow
    docker-compose restart
    Write-Host "✅ Servicios reiniciados" -ForegroundColor Green
}

else {
    Write-Host "📖 Uso del script:" -ForegroundColor Cyan
    Write-Host "  .\docker-run-ollama.ps1 -Build    # Construir y ejecutar" -ForegroundColor White
    Write-Host "  .\docker-run-ollama.ps1 -Logs     # Ver logs" -ForegroundColor White
    Write-Host "  .\docker-run-ollama.ps1 -Stop     # Detener servicios" -ForegroundColor White
    Write-Host "  .\docker-run-ollama.ps1 -Clean    # Limpiar todo" -ForegroundColor White
    Write-Host "  .\docker-run-ollama.ps1 -Status   # Ver estado" -ForegroundColor White
    Write-Host "  .\docker-run-ollama.ps1 -Restart  # Reiniciar servicios" -ForegroundColor White
    Write-Host ""
    Write-Host "🌐 URLs de acceso:" -ForegroundColor Cyan
    Write-Host "  API Principal: http://localhost:8080" -ForegroundColor White
    Write-Host "  Ollama: http://localhost:11434" -ForegroundColor White
    Write-Host "  Swagger: http://localhost:8080/swagger" -ForegroundColor White
}
