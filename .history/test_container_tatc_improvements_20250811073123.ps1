# Script para probar las mejoras en la extracción del campo container
# del documento TATC

Write-Host "=== PRUEBA DE MEJORAS EN EXTRACCIÓN DE CONTAINER TATC ===" -ForegroundColor Green
Write-Host ""

# Leer el archivo de prueba
$archivoPrueba = "test_tatc_container_mejorado.txt"
if (Test-Path $archivoPrueba) {
    $textoOcr = Get-Content $archivoPrueba -Raw
    Write-Host "Archivo de prueba cargado correctamente" -ForegroundColor Yellow
    Write-Host "Longitud del texto: $($textoOcr.Length) caracteres" -ForegroundColor Yellow
    Write-Host ""
} else {
    Write-Host "ERROR: No se encontró el archivo de prueba: $archivoPrueba" -ForegroundColor Red
    exit 1
}

# Simular las expresiones regulares mejoradas para container
Write-Host "=== PRUEBA DE EXTRACCIÓN DE CONTAINER ===" -ForegroundColor Cyan

# Patrón principal mejorado
$matchContenedor = [regex]::Match($textoOcr, "Contenedor\s*:\s*([A-Z0-9\s\-\(\)]+)", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
if ($matchContenedor.Success) {
    Write-Host "✓ Patrón principal encontrado: $($matchContenedor.Groups[1].Value.Trim())" -ForegroundColor Green
} else {
    Write-Host "✗ Patrón principal NO encontrado" -ForegroundColor Red
}

# Fallback 1: formato específico del texto OCR
$matchContenedorFallback = [regex]::Match($textoOcr, "Contenedor\s*:\s*([A-Z0-9\s\-\(\)]+?)(?=\r\n|\n|$)", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
if ($matchContenedorFallback.Success) {
    Write-Host "✓ Fallback 1 (específico) encontrado: $($matchContenedorFallback.Groups[1].Value.Trim())" -ForegroundColor Green
} else {
    Write-Host "✗ Fallback 1 (específico) NO encontrado" -ForegroundColor Red
}

# Fallback 2: formato estándar
$matchContenedorFallback2 = [regex]::Match($textoOcr, "Contenedor\s+([A-Z]{4}\d{6}-\d)", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
if ($matchContenedorFallback2.Success) {
    Write-Host "✓ Fallback 2 (estándar) encontrado: $($matchContenedorFallback2.Groups[1].Value.Trim())" -ForegroundColor Green
} else {
    Write-Host "✗ Fallback 2 (estándar) NO encontrado" -ForegroundColor Red
}

# Fallback 3: formato MSC
$matchContenedorFallback3 = [regex]::Match($textoOcr, "Contenedor\s+([A-Z]{4}\d{7})", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
if ($matchContenedorFallback3.Success) {
    Write-Host "✓ Fallback 3 (MSC) encontrado: $($matchContenedorFallback3.Groups[1].Value.Trim())" -ForegroundColor Green
} else {
    Write-Host "✗ Fallback 3 (MSC) NO encontrado" -ForegroundColor Red
}

# Fallback 4: formato MAERSK
$matchContenedorFallback4 = [regex]::Match($textoOcr, "([A-Z]{4}\d{6}-\d)\s+\d+\s+[A-Z]+", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
if ($matchContenedorFallback4.Success) {
    Write-Host "✓ Fallback 4 (MAERSK) encontrado: $($matchContenedorFallback4.Groups[1].Value.Trim())" -ForegroundColor Green
} else {
    Write-Host "✗ Fallback 4 (MAERSK) NO encontrado" -ForegroundColor Red
}

# Fallback 5: cualquier contenedor en el texto
$matchContenedorFallback5 = [regex]::Match($textoOcr, "([A-Z]{4}\d{7})", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
if ($matchContenedorFallback5.Success) {
    Write-Host "✓ Fallback 5 (genérico) encontrado: $($matchContenedorFallback5.Groups[1].Value.Trim())" -ForegroundColor Green
} else {
    Write-Host "✗ Fallback 5 (genérico) NO encontrado" -ForegroundColor Red
}

Write-Host ""

# Búsqueda específica para el patrón del texto OCR
Write-Host "=== BÚSQUEDA ESPECÍFICA DEL TEXTO OCR ===" -ForegroundColor Yellow

# Búsqueda específica
$matchContenedorEspecifico = [regex]::Match($textoOcr, "Contenedor\s*:\s*([A-Z0-9\s\-\(\)]+)", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
if ($matchContenedorEspecifico.Success) {
    Write-Host "✓ Búsqueda específica encontrada: $($matchContenedorEspecifico.Groups[1].Value.Trim())" -ForegroundColor Green
} else {
    Write-Host "✗ Búsqueda específica NO encontrada" -ForegroundColor Red
}

# Búsqueda agresiva
$matchContenedorAgresivo = [regex]::Match($textoOcr, ".*?Contenedor\s*:?\s*([A-Z0-9\s\-\(\)]+)", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
if ($matchContenedorAgresivo.Success) {
    Write-Host "✓ Búsqueda agresiva encontrada: $($matchContenedorAgresivo.Groups[1].Value.Trim())" -ForegroundColor Green
} else {
    Write-Host "✗ Búsqueda agresiva NO encontrada" -ForegroundColor Red
}

# Búsqueda muy agresiva
$matchContenedorMuyAgresivo = [regex]::Match($textoOcr, ".*?Contenedor.*?([A-Z]{4,5}\s+\d{6}-\d\s*\([A-Z]+\))", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
if ($matchContenedorMuyAgresivo.Success) {
    Write-Host "✓ Búsqueda muy agresiva encontrada: $($matchContenedorMuyAgresivo.Groups[1].Value.Trim())" -ForegroundColor Green
} else {
    Write-Host "✗ Búsqueda muy agresiva NO encontrada" -ForegroundColor Red
}

Write-Host ""

# Búsqueda final de campos críticos
Write-Host "=== BÚSQUEDA FINAL DE CAMPOS CRÍTICOS ===" -ForegroundColor Yellow

# Búsqueda final
$matchContenedorFinal = [regex]::Match($textoOcr, "Contenedor\s*:\s*([A-Z0-9\s\-\(\)]+)", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
if ($matchContenedorFinal.Success) {
    Write-Host "✓ Búsqueda final encontrada: $($matchContenedorFinal.Groups[1].Value.Trim())" -ForegroundColor Green
} else {
    Write-Host "✗ Búsqueda final NO encontrada" -ForegroundColor Red
}

# Último intento
$matchContenedorUltimo = [regex]::Match($textoOcr, ".*?Contenedor\s*:?\s*([A-Z0-9\s\-\(\)]+)", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
if ($matchContenedorUltimo.Success) {
    Write-Host "✓ Último intento encontrado: $($matchContenedorUltimo.Groups[1].Value.Trim())" -ForegroundColor Green
} else {
    Write-Host "✗ Último intento NO encontrado" -ForegroundColor Red
}

# Búsqueda muy agresiva (final)
$matchContenedorMuyAgresivoFinal = [regex]::Match($textoOcr, ".*?Contenedor.*?([A-Z]{4,5}\s+\d{6}-\d\s*\([A-Z]+\))", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
if ($matchContenedorMuyAgresivoFinal.Success) {
    Write-Host "✓ Búsqueda muy agresiva (final) encontrada: $($matchContenedorMuyAgresivoFinal.Groups[1].Value.Trim())" -ForegroundColor Green
} else {
    Write-Host "✗ Búsqueda muy agresiva (final) NO encontrada" -ForegroundColor Red
}

# Búsqueda desesperada
$matchContenedorDesesperado = [regex]::Match($textoOcr, "([A-Z]{4,5}\s+\d{6}-\d\s*\([A-Z]+\))", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
if ($matchContenedorDesesperado.Success) {
    Write-Host "✓ Búsqueda desesperada encontrada: $($matchContenedorDesesperado.Groups[1].Value.Trim())" -ForegroundColor Green
} else {
    Write-Host "✗ Búsqueda desesperada NO encontrada" -ForegroundColor Red
}

Write-Host ""
Write-Host "=== RESUMEN DE EXTRACCIÓN ===" -ForegroundColor Yellow

# Extraer el mejor valor encontrado para container
$containerFinal = ""
if ($matchContenedor.Success) {
    $containerFinal = $matchContenedor.Groups[1].Value.Trim()
} elseif ($matchContenedorFallback.Success) {
    $containerFinal = $matchContenedorFallback.Groups[1].Value.Trim()
} elseif ($matchContenedorFallback2.Success) {
    $containerFinal = $matchContenedorFallback2.Groups[1].Value.Trim()
} elseif ($matchContenedorFallback3.Success) {
    $containerFinal = $matchContenedorFallback3.Groups[1].Value.Trim()
} elseif ($matchContenedorFallback4.Success) {
    $containerFinal = $matchContenedorFallback4.Groups[1].Value.Trim()
} elseif ($matchContenedorFallback5.Success) {
    $containerFinal = $matchContenedorFallback5.Groups[1].Value.Trim()
} elseif ($matchContenedorEspecifico.Success) {
    $containerFinal = $matchContenedorEspecifico.Groups[1].Value.Trim()
} elseif ($matchContenedorAgresivo.Success) {
    $containerFinal = $matchContenedorAgresivo.Groups[1].Value.Trim()
} elseif ($matchContenedorMuyAgresivo.Success) {
    $containerFinal = $matchContenedorMuyAgresivo.Groups[1].Value.Trim()
} elseif ($matchContenedorFinal.Success) {
    $containerFinal = $matchContenedorFinal.Groups[1].Value.Trim()
} elseif ($matchContenedorUltimo.Success) {
    $containerFinal = $matchContenedorUltimo.Groups[1].Value.Trim()
} elseif ($matchContenedorMuyAgresivoFinal.Success) {
    $containerFinal = $matchContenedorMuyAgresivoFinal.Groups[1].Value.Trim()
} elseif ($matchContenedorDesesperado.Success) {
    $containerFinal = $matchContenedorDesesperado.Groups[1].Value.Trim()
}

Write-Host "Container extraído: $containerFinal" -ForegroundColor $(if ($containerFinal) { "Green" } else { "Red" })

Write-Host ""
Write-Host "=== VALOR ESPERADO ===" -ForegroundColor Yellow
Write-Host "Container esperado: CMALJ 650120-0 (FCL)" -ForegroundColor White

Write-Host ""
Write-Host "=== VERIFICACIÓN ===" -ForegroundColor Yellow
if ($containerFinal -eq "CMALJ 650120-0 (FCL)") {
    Write-Host "✓ Container: CORRECTO" -ForegroundColor Green
} else {
    Write-Host "✗ Container: INCORRECTO (esperado: CMALJ 650120-0 (FCL), obtenido: $containerFinal)" -ForegroundColor Red
}

Write-Host ""
Write-Host "Prueba completada." -ForegroundColor Green
