# Script para probar las mejoras en la extracción del campo Nro DIN
# del documento de Seleccion Aforo

Write-Host "=== PRUEBA DE MEJORAS EN EXTRACCIÓN DE NRO DIN ===" -ForegroundColor Green
Write-Host ""

# Leer el archivo de prueba
$archivoPrueba = "test_seleccion_aforo_mejorado.txt"
if (Test-Path $archivoPrueba) {
    $textoOcr = Get-Content $archivoPrueba -Raw
    Write-Host "Archivo de prueba cargado correctamente" -ForegroundColor Yellow
    Write-Host "Longitud del texto: $($textoOcr.Length) caracteres" -ForegroundColor Yellow
    Write-Host ""
} else {
    Write-Host "ERROR: No se encontró el archivo de prueba: $archivoPrueba" -ForegroundColor Red
    exit 1
}

# Simular las expresiones regulares mejoradas para Nro DIN
Write-Host "=== PRUEBA DE EXTRACCIÓN DE NRO DIN ===" -ForegroundColor Cyan

# Patrón principal
$matchDin = [regex]::Match($textoOcr, "Nro\.\s*DIN:\s*(\d+)", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
if ($matchDin.Success) {
    Write-Host "✓ Patrón principal encontrado: $($matchDin.Groups[1].Value.Trim())" -ForegroundColor Green
} else {
    Write-Host "✗ Patrón principal NO encontrado" -ForegroundColor Red
}

# Fallback 1: buscar después de "Declaración de Ingreso"
$matchDinFallback = [regex]::Match($textoOcr, "Declaración de Ingreso.*?Nro\.\s*DIN:\s*(\d+)", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
if ($matchDinFallback.Success) {
    Write-Host "✓ Fallback 1 (Declaración) encontrado: $($matchDinFallback.Groups[1].Value.Trim())" -ForegroundColor Green
} else {
    Write-Host "✗ Fallback 1 (Declaración) NO encontrado" -ForegroundColor Red
}

# Fallback 2: buscar solo después de "DIN:"
$matchDinFallback2 = [regex]::Match($textoOcr, "DIN:\s*(\d+)", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
if ($matchDinFallback2.Success) {
    Write-Host "✓ Fallback 2 (DIN:) encontrado: $($matchDinFallback2.Groups[1].Value.Trim())" -ForegroundColor Green
} else {
    Write-Host "✗ Fallback 2 (DIN:) NO encontrado" -ForegroundColor Red
}

# Fallback 3: patrón más específico del texto OCR
$matchDinFallback3 = [regex]::Match($textoOcr, "Nro\.\s*DIN\s*:?\s*(\d+)", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
if ($matchDinFallback3.Success) {
    Write-Host "✓ Fallback 3 (específico) encontrado: $($matchDinFallback3.Groups[1].Value.Trim())" -ForegroundColor Green
} else {
    Write-Host "✗ Fallback 3 (específico) NO encontrado" -ForegroundColor Red
}

# Fallback 4: patrón más flexible
$matchDinFallback4 = [regex]::Match($textoOcr, "Nro\.?\s*DIN\s*:?\s*(\d+)", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
if ($matchDinFallback4.Success) {
    Write-Host "✓ Fallback 4 (flexible) encontrado: $($matchDinFallback4.Groups[1].Value.Trim())" -ForegroundColor Green
} else {
    Write-Host "✗ Fallback 4 (flexible) NO encontrado" -ForegroundColor Red
}

# Fallback 5: buscar cualquier cosa que contenga "DIN" y números
$matchDinFallback5 = [regex]::Match($textoOcr, ".*?DIN\s*:?\s*(\d+)", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
if ($matchDinFallback5.Success) {
    Write-Host "✓ Fallback 5 (genérico) encontrado: $($matchDinFallback5.Groups[1].Value.Trim())" -ForegroundColor Green
} else {
    Write-Host "✗ Fallback 5 (genérico) NO encontrado" -ForegroundColor Red
}

Write-Host ""

# Búsqueda específica para el patrón del texto OCR
Write-Host "=== BÚSQUEDA ESPECÍFICA DEL TEXTO OCR ===" -ForegroundColor Yellow

# Búsqueda específica
$matchDinEspecifico = [regex]::Match($textoOcr, "Nro\.\s*DIN\s*:\s*(\d+)", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
if ($matchDinEspecifico.Success) {
    Write-Host "✓ Búsqueda específica encontrada: $($matchDinEspecifico.Groups[1].Value.Trim())" -ForegroundColor Green
} else {
    Write-Host "✗ Búsqueda específica NO encontrada" -ForegroundColor Red
}

# Búsqueda agresiva
$matchDinAgresivo = [regex]::Match($textoOcr, "Nro\.\s*DIN\s*:\s*(\d+)", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
if ($matchDinAgresivo.Success) {
    Write-Host "✓ Búsqueda agresiva encontrada: $($matchDinAgresivo.Groups[1].Value.Trim())" -ForegroundColor Green
} else {
    Write-Host "✗ Búsqueda agresiva NO encontrada" -ForegroundColor Red
}

# Búsqueda muy agresiva
$matchDinMuyAgresivo = [regex]::Match($textoOcr, ".*?DIN.*?(\d{10})", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
if ($matchDinMuyAgresivo.Success) {
    Write-Host "✓ Búsqueda muy agresiva encontrada: $($matchDinMuyAgresivo.Groups[1].Value.Trim())" -ForegroundColor Green
} else {
    Write-Host "✗ Búsqueda muy agresiva NO encontrada" -ForegroundColor Red
}

Write-Host ""

# Búsqueda final de campos críticos
Write-Host "=== BÚSQUEDA FINAL DE CAMPOS CRÍTICOS ===" -ForegroundColor Yellow

# Búsqueda final
$matchDinFinal = [regex]::Match($textoOcr, "Nro\.\s*DIN\s*:\s*(\d+)", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
if ($matchDinFinal.Success) {
    Write-Host "✓ Búsqueda final encontrada: $($matchDinFinal.Groups[1].Value.Trim())" -ForegroundColor Green
} else {
    Write-Host "✗ Búsqueda final NO encontrada" -ForegroundColor Red
}

# Último intento
$matchDinUltimo = [regex]::Match($textoOcr, ".*?DIN\s*:?\s*(\d+)", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
if ($matchDinUltimo.Success) {
    Write-Host "✓ Último intento encontrado: $($matchDinUltimo.Groups[1].Value.Trim())" -ForegroundColor Green
} else {
    Write-Host "✗ Último intento NO encontrado" -ForegroundColor Red
}

# Búsqueda muy agresiva (final)
$matchDinMuyAgresivoFinal = [regex]::Match($textoOcr, ".*?DIN.*?(\d{10})", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
if ($matchDinMuyAgresivoFinal.Success) {
    Write-Host "✓ Búsqueda muy agresiva (final) encontrada: $($matchDinMuyAgresivoFinal.Groups[1].Value.Trim())" -ForegroundColor Green
} else {
    Write-Host "✗ Búsqueda muy agresiva (final) NO encontrada" -ForegroundColor Red
}

# Búsqueda desesperada
$matchDinDesesperado = [regex]::Match($textoOcr, "DIN.*?(\d{10})", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
if ($matchDinDesesperado.Success) {
    Write-Host "✓ Búsqueda desesperada encontrada: $($matchDinDesesperado.Groups[1].Value.Trim())" -ForegroundColor Green
} else {
    Write-Host "✗ Búsqueda desesperada NO encontrada" -ForegroundColor Red
}

Write-Host ""
Write-Host "=== RESUMEN DE EXTRACCIÓN ===" -ForegroundColor Yellow

# Extraer el mejor valor encontrado para Nro DIN
$nroDinFinal = ""
if ($matchDin.Success) {
    $nroDinFinal = $matchDin.Groups[1].Value
} elseif ($matchDinFallback.Success) {
    $nroDinFinal = $matchDinFallback.Groups[1].Value
} elseif ($matchDinFallback2.Success) {
    $nroDinFinal = $matchDinFallback2.Groups[1].Value
} elseif ($matchDinFallback3.Success) {
    $nroDinFinal = $matchDinFallback3.Groups[1].Value
} elseif ($matchDinFallback4.Success) {
    $nroDinFinal = $matchDinFallback4.Groups[1].Value
} elseif ($matchDinFallback5.Success) {
    $nroDinFinal = $matchDinFallback5.Groups[1].Value
} elseif ($matchDinEspecifico.Success) {
    $nroDinFinal = $matchDinEspecifico.Groups[1].Value
} elseif ($matchDinAgresivo.Success) {
    $nroDinFinal = $matchDinAgresivo.Groups[1].Value
} elseif ($matchDinMuyAgresivo.Success) {
    $nroDinFinal = $matchDinMuyAgresivo.Groups[1].Value
} elseif ($matchDinFinal.Success) {
    $nroDinFinal = $matchDinFinal.Groups[1].Value
} elseif ($matchDinUltimo.Success) {
    $nroDinFinal = $matchDinUltimo.Groups[1].Value
} elseif ($matchDinMuyAgresivoFinal.Success) {
    $nroDinFinal = $matchDinMuyAgresivoFinal.Groups[1].Value
} elseif ($matchDinDesesperado.Success) {
    $nroDinFinal = $matchDinDesesperado.Groups[1].Value
}

Write-Host "Nro DIN extraído: $nroDinFinal" -ForegroundColor $(if ($nroDinFinal) { "Green" } else { "Red" })

Write-Host ""
Write-Host "=== VALOR ESPERADO ===" -ForegroundColor Yellow
Write-Host "Nro DIN esperado: 2120204867" -ForegroundColor White

Write-Host ""
Write-Host "=== VERIFICACIÓN ===" -ForegroundColor Yellow
if ($nroDinFinal -eq "2120204867") {
    Write-Host "✓ Nro DIN: CORRECTO" -ForegroundColor Green
} else {
    Write-Host "✗ Nro DIN: INCORRECTO (esperado: 2120204867, obtenido: $nroDinFinal)" -ForegroundColor Red
}

Write-Host ""
Write-Host "Prueba completada." -ForegroundColor Green
