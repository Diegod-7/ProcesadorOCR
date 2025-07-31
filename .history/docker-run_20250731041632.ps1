# Script de PowerShell para ejecutar el Procesador OCR con Docker
# Ejecutar como administrador si hay problemas de permisos

param(
    [switch]$Build,
    [switch]$Stop,
    [switch]$Logs,
    [switch]$Clean,
    [switch]$Help
)

function Show-Help {
    Write-Host "Uso: .\docker-run.ps1 [opciones]" -ForegroundColor Green
    Write-Host ""
    Write-Host "Opciones:" -ForegroundColor Yellow
    Write-Host "  -Build    Construir y ejecutar la aplicación" -ForegroundColor White
    Write-Host "  -Stop     Detener la aplicación" -ForegroundColor White
    Write-Host "  -Logs     Mostrar logs en tiempo real" -ForegroundColor White
    Write-Host "  -Clean    Limpiar contenedores e imágenes" -ForegroundColor White
    Write-Host "  -Help     Mostrar esta ayuda" -ForegroundColor White
    Write-Host ""
    Write-Host "Ejemplos:" -ForegroundColor Yellow
    Write-Host "  .\docker-run.ps1 -Build    # Construir y ejecutar" -ForegroundColor White
    Write-Host "  .\docker-run.ps1 -Logs     # Ver logs" -ForegroundColor White
    Write-Host "  .\docker-run.ps1 -Stop     # Detener" -ForegroundColor White
}

function Test-Docker {
    try {
        docker --version | Out-Null
        return $true
    }
    catch {
        Write-Host "Error: Docker no está instalado o no está en el PATH" -ForegroundColor Red
        return $false
    }
}

function Test-DockerCompose {
    try {
        docker-compose --version | Out-Null
        return $true
    }
    catch {
        Write-Host "Error: Docker Compose no está instalado" -ForegroundColor Red
        return $false
    }
}

function Start-Application {
    Write-Host "Iniciando Procesador OCR..." -ForegroundColor Green
    
    # Crear directorios si no existen
    if (!(Test-Path "Uploads")) {
        New-Item -ItemType Directory -Path "Uploads" | Out-Null
        Write-Host "Directorio Uploads creado" -ForegroundColor Yellow
    }
    
    if (!(Test-Path "Logs")) {
        New-Item -ItemType Directory -Path "Logs" | Out-Null
        Write-Host "Directorio Logs creado" -ForegroundColor Yellow
    }
    
    # Construir y ejecutar
    Write-Host "Construyendo imagen Docker..." -ForegroundColor Yellow
    docker-compose up --build -d
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Aplicación iniciada exitosamente!" -ForegroundColor Green
        Write-Host ""
        Write-Host "URLs de acceso:" -ForegroundColor Cyan
        Write-Host "  API: http://localhost:8080" -ForegroundColor White
        Write-Host "  Swagger: http://localhost:8080" -ForegroundColor White
        Write-Host "  Health: http://localhost:8080/health" -ForegroundColor White
        Write-Host ""
        Write-Host "Para ver logs: .\docker-run.ps1 -Logs" -ForegroundColor Yellow
        Write-Host "Para detener: .\docker-run.ps1 -Stop" -ForegroundColor Yellow
    }
    else {
        Write-Host "Error al iniciar la aplicación" -ForegroundColor Red
    }
}

function Stop-Application {
    Write-Host "Deteniendo Procesador OCR..." -ForegroundColor Yellow
    docker-compose down
    Write-Host "Aplicación detenida" -ForegroundColor Green
}

function Show-Logs {
    Write-Host "Mostrando logs en tiempo real (Ctrl+C para salir)..." -ForegroundColor Yellow
    docker-compose logs -f
}

function Clean-Docker {
    Write-Host "Limpiando contenedores e imágenes..." -ForegroundColor Yellow
    
    # Detener y eliminar contenedores
    docker-compose down -v
    
    # Eliminar imágenes
    docker rmi procesador-ocr:latest -f 2>$null
    
    # Limpiar imágenes no utilizadas
    docker image prune -f
    
    Write-Host "Limpieza completada" -ForegroundColor Green
}

# Verificar Docker
if (!(Test-Docker)) {
    exit 1
}

if (!(Test-DockerCompose)) {
    exit 1
}

# Procesar parámetros
if ($Help) {
    Show-Help
    exit 0
}

if ($Stop) {
    Stop-Application
    exit 0
}

if ($Logs) {
    Show-Logs
    exit 0
}

if ($Clean) {
    Clean-Docker
    exit 0
}

if ($Build) {
    Start-Application
    exit 0
}

# Si no se especificó ningún parámetro, mostrar ayuda
Show-Help 