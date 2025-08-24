# Script para configurar Tesseract OCR localmente en Windows
Write-Host "🖥️ Configurando Tesseract OCR para desarrollo local" -ForegroundColor Green

# Verificar si Tesseract está instalado
$tesseractPath = $null
$possiblePaths = @(
    "C:\Program Files\Tesseract-OCR\tesseract.exe",
    "C:\Program Files (x86)\Tesseract-OCR\tesseract.exe"
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
    Write-Host "📥 Descarga desde: https://github.com/UB-Mannheim/tesseract/wiki" -ForegroundColor Yellow
    exit 1
}

# Verificar archivos de idioma
$tessdataPath = Join-Path $tesseractPath "tessdata"
if (Test-Path $tessdataPath) {
    $files = Get-ChildItem $tessdataPath -Filter "*.traineddata"
    Write-Host "✅ Archivos de idioma encontrados: $($files.Count)" -ForegroundColor Green
}

# Actualizar configuración local
$configFile = "src\CarnetAduaneroProcessor.API\appsettings.Development.json"
if (Test-Path $configFile) {
    $content = Get-Content $configFile -Raw
    $updatedContent = $content -replace '"TessdataPath": ".*"', "`"TessdataPath`": `"$($tessdataPath.Replace('\', '\\'))`""
    Set-Content $configFile $updatedContent -Encoding UTF8
    Write-Host "✅ Configuración actualizada" -ForegroundColor Green
}

Write-Host "🚀 Para ejecutar: cd src\CarnetAduaneroProcessor.API && dotnet run" -ForegroundColor Yellow
