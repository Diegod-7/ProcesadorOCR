# Script para probar las mejoras en la extracción de BL Armador y NaveViaje
# del Documento de Recepción

Write-Host "=== PRUEBA DE MEJORAS EN EXTRACCIÓN DE DOCUMENTO DE RECEPCIÓN ===" -ForegroundColor Green
Write-Host ""

# Leer el archivo de prueba
$archivoPrueba = "test_documento_recepcion_mejorado.txt"
if (Test-Path $archivoPrueba) {
    $textoOcr = Get-Content $archivoPrueba -Raw
    Write-Host "Archivo de prueba cargado correctamente" -ForegroundColor Yellow
    Write-Host "Longitud del texto: $($textoOcr.Length) caracteres" -ForegroundColor Yellow
    Write-Host ""
} else {
    Write-Host "ERROR: No se encontró el archivo de prueba: $archivoPrueba" -ForegroundColor Red
    exit 1
}

# Simular las expresiones regulares mejoradas para BL Armador
Write-Host "=== PRUEBA DE EXTRACCIÓN DE BL ARMADOR ===" -ForegroundColor Cyan

# Patrón principal
$blArmadorMatch = [regex]::Match($textoOcr, "BL\s+(?:Armador|Arm)\s*:?\s*([A-Z0-9\(\)\/\s]+?)(?=\r\n|\n|$)")
if ($blArmadorMatch.Success) {
    Write-Host "✓ Patrón principal encontrado: $($blArmadorMatch.Groups[1].Value.Trim())" -ForegroundColor Green
} else {
    Write-Host "✗ Patrón principal NO encontrado" -ForegroundColor Red
}

# Fallback 1
$blArmadorFallback1 = [regex]::Match($textoOcr, "BL\s+Armador\s*:?\s*([A-Z0-9\(\)\/\s]+?)(?=\r\n|\n|$)")
if ($blArmadorFallback1.Success) {
    Write-Host "✓ Fallback 1 encontrado: $($blArmadorFallback1.Groups[1].Value.Trim())" -ForegroundColor Green
} else {
    Write-Host "✗ Fallback 1 NO encontrado" -ForegroundColor Red
}

# Fallback 2 - más específico
$blArmadorFallback2 = [regex]::Match($textoOcr, "BL\s+Armador\s*:?\s*([A-Z0-9\(\)\/\s]+?)(?=\r\n|\n|Consignatario|$)")
if ($blArmadorFallback2.Success) {
    Write-Host "✓ Fallback 2 encontrado: $($blArmadorFallback2.Groups[1].Value.Trim())" -ForegroundColor Green
} else {
    Write-Host "✗ Fallback 2 NO encontrado" -ForegroundColor Red
}

# Fallback 3 - más flexible
$blArmadorFallback3 = [regex]::Match($textoOcr, "BL\s+Armador\s*:?\s*([^:\r\n]+?)(?=\r\n|\n|Consignatario|$)")
if ($blArmadorFallback3.Success) {
    Write-Host "✓ Fallback 3 encontrado: $($blArmadorFallback3.Groups[1].Value.Trim())" -ForegroundColor Green
} else {
    Write-Host "✗ Fallback 3 NO encontrado" -ForegroundColor Red
}

# Búsqueda específica del texto OCR
$blArmadorEspecifico = [regex]::Match($textoOcr, "BL\s+Armador\s*:\s*([^:\r\n]+?)(?=\r\n|\n|Consignatario|$)")
if ($blArmadorEspecifico.Success) {
    Write-Host "✓ Búsqueda específica encontrada: $($blArmadorEspecifico.Groups[1].Value.Trim())" -ForegroundColor Green
} else {
    Write-Host "✗ Búsqueda específica NO encontrada" -ForegroundColor Red
}

Write-Host ""

# Simular las expresiones regulares mejoradas para Nave/Viaje
Write-Host "=== PRUEBA DE EXTRACCIÓN DE NAVE/VIAJE ===" -ForegroundColor Cyan

# Patrón principal
$naveViajeMatch = [regex]::Match($textoOcr, "(?:Nave/Viaje|Nave|Viaje)\s*:?\s*([A-Z\s]+/\s*[A-Z0-9]+?)(?=\r\n|\n|$)")
if ($naveViajeMatch.Success) {
    Write-Host "✓ Patrón principal encontrado: $($naveViajeMatch.Groups[1].Value.Trim())" -ForegroundColor Green
} else {
    Write-Host "✗ Patrón principal NO encontrado" -ForegroundColor Red
}

# Fallback 1
$naveViajeFallback1 = [regex]::Match($textoOcr, "Nave/Viaje\s*:?\s*([A-Z\s]+/\s*[A-Z0-9]+?)(?=\r\n|\n|$)")
if ($naveViajeFallback1.Success) {
    Write-Host "✓ Fallback 1 encontrado: $($naveViajeFallback1.Groups[1].Value.Trim())" -ForegroundColor Green
} else {
    Write-Host "✗ Fallback 1 NO encontrado" -ForegroundColor Red
}

# Fallback 2 - más específico
$naveViajeFallback2 = [regex]::Match($textoOcr, "Nave/Viaje\s*:?\s*([A-Z\s]+/\s*[A-Z0-9]+?)(?=\r\n|\n|Linea|$)")
if ($naveViajeFallback2.Success) {
    Write-Host "✓ Fallback 2 encontrado: $($naveViajeFallback2.Groups[1].Value.Trim())" -ForegroundColor Green
} else {
    Write-Host "✗ Fallback 2 NO encontrado" -ForegroundColor Red
}

# Fallback 3 - más flexible
$naveViajeFallback3 = [regex]::Match($textoOcr, "Nave/Viaje\s*:?\s*([^:\r\n]+?)(?=\r\n|\n|Linea|$)")
if ($naveViajeFallback3.Success) {
    Write-Host "✓ Fallback 3 encontrado: $($naveViajeFallback3.Groups[1].Value.Trim())" -ForegroundColor Green
} else {
    Write-Host "✗ Fallback 3 NO encontrado" -ForegroundColor Red
}

# Fallback 4 - muy flexible
$naveViajeFallback4 = [regex]::Match($textoOcr, "Nave/Viaje\s*:?\s*([^:\r\n]+?)(?=\r\n|\n|$)")
if ($naveViajeFallback4.Success) {
    Write-Host "✓ Fallback 4 encontrado: $($naveViajeFallback4.Groups[1].Value.Trim())" -ForegroundColor Green
} else {
    Write-Host "✗ Fallback 4 NO encontrado" -ForegroundColor Red
}

# Búsqueda específica del texto OCR
$naveViajeEspecifico = [regex]::Match($textoOcr, "Nave/Viaje\s*:\s*([^:\r\n]+?)(?=\r\n|\n|Linea|$)")
if ($naveViajeEspecifico.Success) {
    Write-Host "✓ Búsqueda específica encontrada: $($naveViajeEspecifico.Groups[1].Value.Trim())" -ForegroundColor Green
} else {
    Write-Host "✗ Búsqueda específica NO encontrada" -ForegroundColor Red
}

Write-Host ""
Write-Host "=== RESUMEN DE EXTRACCIÓN ===" -ForegroundColor Yellow

# Extraer el mejor valor encontrado para BL Armador
$blArmadorFinal = ""
if ($blArmadorEspecifico.Success) {
    $blArmadorFinal = $blArmadorEspecifico.Groups[1].Value.Trim()
} elseif ($blArmadorFallback3.Success) {
    $blArmadorFinal = $blArmadorFallback3.Groups[1].Value.Trim()
} elseif ($blArmadorFallback2.Success) {
    $blArmadorFinal = $blArmadorFallback2.Groups[1].Value.Trim()
} elseif ($blArmadorFallback1.Success) {
    $blArmadorFinal = $blArmadorFallback1.Groups[1].Value.Trim()
} elseif ($blArmadorMatch.Success) {
    $blArmadorFinal = $blArmadorMatch.Groups[1].Value.Trim()
}

# Extraer el mejor valor encontrado para Nave/Viaje
$naveViajeFinal = ""
if ($naveViajeEspecifico.Success) {
    $naveViajeFinal = $naveViajeEspecifico.Groups[1].Value.Trim()
} elseif ($naveViajeFallback4.Success) {
    $naveViajeFinal = $naveViajeFallback4.Groups[1].Value.Trim()
} elseif ($naveViajeFallback3.Success) {
    $naveViajeFinal = $naveViajeFallback3.Groups[1].Value.Trim()
} elseif ($naveViajeFallback2.Success) {
    $naveViajeFinal = $naveViajeFallback2.Groups[1].Value.Trim()
} elseif ($naveViajeFallback1.Success) {
    $naveViajeFinal = $naveViajeFallback1.Groups[1].Value.Trim()
} elseif ($naveViajeMatch.Success) {
    $naveViajeFinal = $naveViajeMatch.Groups[1].Value.Trim()
}

Write-Host "BL Armador extraído: $blArmadorFinal" -ForegroundColor $(if ($blArmadorFinal) { "Green" } else { "Red" })
Write-Host "Nave/Viaje extraído: $naveViajeFinal" -ForegroundColor $(if ($naveViajeFinal) { "Green" } else { "Red" })

Write-Host ""
Write-Host "=== VALORES ESPERADOS ===" -ForegroundColor Yellow
Write-Host "BL Armador esperado: (M)BAC0549074/ (H)DACA78565" -ForegroundColor White
Write-Host "Nave/Viaje esperado: CMA CGM BEIRA / OLISSN1" -ForegroundColor White

Write-Host ""
Write-Host "=== VERIFICACIÓN ===" -ForegroundColor Yellow
if ($blArmadorFinal -eq "(M)BAC0549074/ (H)DACA78565") {
    Write-Host "✓ BL Armador: CORRECTO" -ForegroundColor Green
} else {
    Write-Host "✗ BL Armador: INCORRECTO (esperado: (M)BAC0549074/ (H)DACA78565, obtenido: $blArmadorFinal)" -ForegroundColor Red
}

if ($naveViajeFinal -eq "CMA CGM BEIRA / OLISSN1") {
    Write-Host "✓ Nave/Viaje: CORRECTO" -ForegroundColor Green
} else {
    Write-Host "✗ Nave/Viaje: INCORRECTO (esperado: CMA CGM BEIRA / OLISSN1, obtenido: $naveViajeFinal)" -ForegroundColor Red
}

Write-Host ""
Write-Host "Prueba completada." -ForegroundColor Green
