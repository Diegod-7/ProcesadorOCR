# Script para configurar Tesseract OCR localmente en Windows
Write-Host "🖥️ Configurando Tesseract OCR para desarrollo local" -ForegroundColor Green
Write-Host "=====================================================" -ForegroundColor Green

# Verificar si Tesseract está instalado
Write-Host "🔍 Verificando instalación de Tesseract..." -ForegroundColor Yellow

$tesseractPath = $null
$possiblePaths = @(
    "C:\Program Files\Tesseract-OCR\tesseract.exe",
    "C:\Program Files (x86)\Tesseract-OCR\tesseract.exe",
    "$env:ProgramFiles\Tesseract-OCR\tesseract.exe",
    "$env:ProgramFiles(x86)\Tesseract-OCR\tesseract.exe"
)

foreach ($path in $possiblePaths) {
    if (Test-Path $path) {
        $tesseractPath = Split-Path $path
        Write-Host "✅ Tesseract encontrado en: $tesseractPath" -ForegroundColor Green
        break
    }
}

if (-not $tesseractPath) {
    Write-Host "❌ Tesseract no encontrado. Instalando..." -ForegroundColor Red
    
    # Intentar instalar con Chocolatey
    try {
        Write-Host "📦 Instalando Tesseract con Chocolatey..." -ForegroundColor Yellow
        choco install tesseract -y
        if ($LASTEXITCODE -eq 0) {
            $tesseractPath = "C:\ProgramData\chocolatey\lib\tesseract\tools"
            Write-Host "✅ Tesseract instalado con Chocolatey" -ForegroundColor Green
        } else {
            throw "Error en instalación con Chocolatey"
        }
    } catch {
        Write-Host "❌ Chocolatey no disponible o error en instalación" -ForegroundColor Red
        Write-Host "📥 Por favor instala Tesseract manualmente desde:" -ForegroundColor Yellow
        Write-Host "   https://github.com/UB-Mannheim/tesseract/wiki" -ForegroundColor Cyan
        Write-Host "   Luego ejecuta este script nuevamente" -ForegroundColor Yellow
        exit 1
    }
}

# Verificar archivos de idioma
Write-Host "🌍 Verificando archivos de idioma..." -ForegroundColor Yellow
$tessdataPath = Join-Path $tesseractPath "tessdata"

if (Test-Path $tessdataPath) {
    $files = Get-ChildItem $tessdataPath -Filter "*.traineddata"
    if ($files.Count -gt 0) {
        Write-Host "✅ Archivos de idioma encontrados:" -ForegroundColor Green
        foreach ($file in $files) {
            Write-Host "   - $($file.Name)" -ForegroundColor Cyan
        }
    } else {
        Write-Host "⚠️  No se encontraron archivos de idioma" -ForegroundColor Yellow
    }
} else {
    Write-Host "❌ Directorio tessdata no encontrado en: $tessdataPath" -ForegroundColor Red
}

# Actualizar configuración local
Write-Host "🔧 Actualizando configuración local..." -ForegroundColor Yellow
$configFile = "src\CarnetAduaneroProcessor.API\appsettings.Development.json"

if (Test-Path $configFile) {
    $content = Get-Content $configFile -Raw
    $updatedContent = $content -replace '"TessdataPath": ".*"', "`"TessdataPath`": `"$($tessdataPath.Replace('\', '\\'))`""
    
    Set-Content $configFile $updatedContent -Encoding UTF8
    Write-Host "✅ Configuración actualizada con ruta: $tessdataPath" -ForegroundColor Green
} else {
    Write-Host "⚠️  Archivo de configuración no encontrado: $configFile" -ForegroundColor Yellow
}

# Verificar que el proyecto compile
Write-Host "📦 Compilando proyecto..." -ForegroundColor Yellow
try {
    dotnet build "src\CarnetAduaneroProcessor.API\CarnetAduaneroProcessor.API.csproj" --configuration Debug
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Proyecto compilado exitosamente" -ForegroundColor Green
    } else {
        Write-Host "❌ Error en la compilación" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "❌ Error durante la compilación: $_" -ForegroundColor Red
    exit 1
}

# Crear directorio de uploads si no existe
Write-Host "📁 Creando directorios necesarios..." -ForegroundColor Yellow
$uploadPath = "src\CarnetAduaneroProcessor.API\Uploads"
$logsPath = "src\CarnetAduaneroProcessor.API\Logs"

if (-not (Test-Path $uploadPath)) {
    New-Item -ItemType Directory -Path $uploadPath -Force
    Write-Host "✅ Directorio de uploads creado: $uploadPath" -ForegroundColor Green
}

if (-not (Test-Path $logsPath)) {
    New-Item -ItemType Directory -Path $logsPath -Force
    Write-Host "✅ Directorio de logs creado: $logsPath" -ForegroundColor Green
}

Write-Host ""
Write-Host "🎯 Configuración local completada:" -ForegroundColor Green
Write-Host "=================================" -ForegroundColor Green
Write-Host "✅ Tesseract instalado y configurado" -ForegroundColor Green
Write-Host "✅ Ruta configurada: $tessdataPath" -ForegroundColor Green
Write-Host "✅ Proyecto compilado correctamente" -ForegroundColor Green
Write-Host "✅ Directorios de trabajo creados" -ForegroundColor Green
Write-Host ""
Write-Host "🚀 Para ejecutar localmente:" -ForegroundColor Yellow
Write-Host "   1. cd src\CarnetAduaneroProcessor.API" -ForegroundColor Cyan
Write-Host "   2. dotnet run" -ForegroundColor Cyan
Write-Host "   3. ¡Disfrutar del OCR gratuito!" -ForegroundColor Cyan
Write-Host ""
Write-Host "💡 La aplicación usará Tesseract localmente y Azure como fallback" -ForegroundColor Green
