# Script para probar las mejoras en la extracción de Cantidad y Peso
# del Documento de Recepción

Write-Host "=== PRUEBA DE MEJORAS EN EXTRACCIÓN DE CANTIDAD Y PESO ===" -ForegroundColor Green
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

# Simular las expresiones regulares mejoradas para Cantidad
Write-Host "=== PRUEBA DE EXTRACCIÓN DE CANTIDAD ===" -ForegroundColor Cyan

# Patrón principal
$cantidadMatch = [regex]::Match($textoOcr, "Cantidad\s*:?\s*(\d+)")
if ($cantidadMatch.Success) {
    Write-Host "✓ Patrón principal encontrado: $($cantidadMatch.Groups[1].Value.Trim())" -ForegroundColor Green
} else {
    Write-Host "✗ Patrón principal NO encontrado" -ForegroundColor Red
}

# Fallback 1: buscar en la tabla de bultos
$cantidadFallback1 = [regex]::Match($textoOcr, "(?:40'|20')\s+[A-Z\s]+?\s+(\d+)\s+[\d\.,]+")
if ($cantidadFallback1.Success) {
    Write-Host "✓ Fallback 1 (tabla) encontrado: $($cantidadFallback1.Groups[1].Value.Trim())" -ForegroundColor Green
} else {
    Write-Host "✗ Fallback 1 (tabla) NO encontrado" -ForegroundColor Red
}

# Fallback 2: buscar después de "CONTENEDOR"
$cantidadFallback2 = [regex]::Match($textoOcr, "CONTENEDOR\s+[A-Z\s]+?\s+(\d+)\s+[\d\.,]+")
if ($cantidadFallback2.Success) {
    Write-Host "✓ Fallback 2 (contenedor) encontrado: $($cantidadFallback2.Groups[1].Value.Trim())" -ForegroundColor Green
} else {
    Write-Host "✗ Fallback 2 (contenedor) NO encontrado" -ForegroundColor Red
}

# Fallback 3: buscar cualquier número después de "STD" o "NORMAL"
$cantidadFallback3 = [regex]::Match($textoOcr, "(?:STD|NORMAL)\s+(\d+)\s+[\d\.,]+")
if ($cantidadFallback3.Success) {
    Write-Host "✓ Fallback 3 (STD) encontrado: $($cantidadFallback3.Groups[1].Value.Trim())" -ForegroundColor Green
} else {
    Write-Host "✗ Fallback 3 (STD) NO encontrado" -ForegroundColor Red
}

Write-Host ""

# Simular las expresiones regulares mejoradas para Peso
Write-Host "=== PRUEBA DE EXTRACCIÓN DE PESO ===" -ForegroundColor Cyan

# Patrón principal
$pesoMatch = [regex]::Match($textoOcr, "Peso\s*:?\s*([\d\.,]+)")
if ($pesoMatch.Success) {
    Write-Host "✓ Patrón principal encontrado: $($pesoMatch.Groups[1].Value.Trim())" -ForegroundColor Green
} else {
    Write-Host "✗ Patrón principal NO encontrado" -ForegroundColor Red
}

# Fallback 1: buscar en la tabla de bultos después de la cantidad
$pesoFallback1 = [regex]::Match($textoOcr, "(?:40'|20')\s+[A-Z\s]+?\s+\d+\s+([\d\.,]+)")
if ($pesoFallback1.Success) {
    Write-Host "✓ Fallback 1 (tabla) encontrado: $($pesoFallback1.Groups[1].Value.Trim())" -ForegroundColor Green
} else {
    Write-Host "✗ Fallback 1 (tabla) NO encontrado" -ForegroundColor Red
}

# Fallback 2: buscar después de "CONTENEDOR" y cantidad
$pesoFallback2 = [regex]::Match($textoOcr, "CONTENEDOR\s+[A-Z\s]+?\s+\d+\s+([\d\.,]+)")
if ($pesoFallback2.Success) {
    Write-Host "✓ Fallback 2 (contenedor) encontrado: $($pesoFallback2.Groups[1].Value.Trim())" -ForegroundColor Green
} else {
    Write-Host "✗ Fallback 2 (contenedor) NO encontrado" -ForegroundColor Red
}

# Fallback 3: buscar cualquier número decimal después de "STD" o "NORMAL" y cantidad
$pesoFallback3 = [regex]::Match($textoOcr, "(?:STD|NORMAL)\s+\d+\s+([\d\.,]+)")
if ($pesoFallback3.Success) {
    Write-Host "✓ Fallback 3 (STD) encontrado: $($pesoFallback3.Groups[1].Value.Trim())" -ForegroundColor Green
} else {
    Write-Host "✗ Fallback 3 (STD) NO encontrado" -ForegroundColor Red
}

# Fallback 4: búsqueda más agresiva en la tabla
$pesoFallback4 = [regex]::Match($textoOcr, "(\d+)\s+([\d\.,]+)\s+[\d\.,]+")
if ($pesoFallback4.Success) {
    $cantidadEncontrada = $pesoFallback4.Groups[1].Value
    $pesoEncontrado = $pesoFallback4.Groups[2].Value
    if ($cantidadEncontrada -eq "1") {
        Write-Host "✓ Fallback 4 (agresivo) encontrado: $pesoEncontrado (cantidad: $cantidadEncontrada)" -ForegroundColor Green
    } else {
        Write-Host "✗ Fallback 4 (agresivo) encontrado pero cantidad incorrecta: $cantidadEncontrada" -ForegroundColor Yellow
    }
} else {
    Write-Host "✗ Fallback 4 (agresivo) NO encontrado" -ForegroundColor Red
}

# Búsqueda específica para el patrón del texto OCR
Write-Host ""
Write-Host "=== BÚSQUEDA ESPECÍFICA EN TABLA DE BULTOS ===" -ForegroundColor Yellow

# Buscar la línea específica del contenedor
$lineaContenedor = [regex]::Match($textoOcr, "\(H40\)\s+40'\s+CONTENEDOR\s+HIGH\s+CUBE\s+STD\s+(\d+)\s+([\d\.,]+)")
if ($lineaContenedor.Success) {
    $cantidadEspecifica = $lineaContenedor.Groups[1].Value
    $pesoEspecifico = $lineaContenedor.Groups[2].Value
    Write-Host "✓ Línea de contenedor encontrada:" -ForegroundColor Green
    Write-Host "  Cantidad: $cantidadEspecifica" -ForegroundColor Green
    Write-Host "  Peso: $pesoEspecifico" -ForegroundColor Green
} else {
    Write-Host "✗ Línea de contenedor NO encontrada" -ForegroundColor Red
    
    # Búsqueda alternativa más flexible
    $lineaContenedorAlt = [regex]::Match($textoOcr, "40'\s+CONTENEDOR\s+[A-Z\s]+?\s+(\d+)\s+([\d\.,]+)")
    if ($lineaContenedorAlt.Success) {
        $cantidadEspecifica = $lineaContenedorAlt.Groups[1].Value
        $pesoEspecifico = $lineaContenedorAlt.Groups[2].Value
        Write-Host "✓ Línea de contenedor (alternativa) encontrada:" -ForegroundColor Green
        Write-Host "  Cantidad: $cantidadEspecifica" -ForegroundColor Green
        Write-Host "  Peso: $pesoEspecifico" -ForegroundColor Green
    } else {
        Write-Host "✗ Línea de contenedor (alternativa) NO encontrada" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "=== RESUMEN DE EXTRACCIÓN ===" -ForegroundColor Yellow

# Extraer el mejor valor encontrado para Cantidad
$cantidadFinal = ""
if ($lineaContenedor.Success) {
    $cantidadFinal = $lineaContenedor.Groups[1].Value
} elseif ($lineaContenedorAlt.Success) {
    $cantidadFinal = $lineaContenedorAlt.Groups[1].Value
} elseif ($cantidadFallback3.Success) {
    $cantidadFinal = $cantidadFallback3.Groups[1].Value
} elseif ($cantidadFallback2.Success) {
    $cantidadFinal = $cantidadFallback2.Groups[1].Value
} elseif ($cantidadFallback1.Success) {
    $cantidadFinal = $cantidadFallback1.Groups[1].Value
} elseif ($cantidadMatch.Success) {
    $cantidadFinal = $cantidadMatch.Groups[1].Value
}

# Extraer el mejor valor encontrado para Peso
$pesoFinal = ""
if ($lineaContenedor.Success) {
    $pesoFinal = $lineaContenedor.Groups[2].Value
} elseif ($lineaContenedorAlt.Success) {
    $pesoFinal = $lineaContenedorAlt.Groups[2].Value
} elseif ($pesoFallback3.Success) {
    $pesoFinal = $pesoFallback3.Groups[1].Value
} elseif ($pesoFallback2.Success) {
    $pesoFinal = $pesoFallback2.Groups[1].Value
} elseif ($pesoFallback1.Success) {
    $pesoFinal = $pesoFallback1.Groups[1].Value
} elseif ($pesoMatch.Success) {
    $pesoFinal = $pesoMatch.Groups[1].Value
} elseif ($pesoFallback4.Success -and $pesoFallback4.Groups[1].Value -eq "1") {
    $pesoFinal = $pesoFallback4.Groups[2].Value
}

Write-Host "Cantidad extraída: $cantidadFinal" -ForegroundColor $(if ($cantidadFinal) { "Green" } else { "Red" })
Write-Host "Peso extraído: $pesoFinal" -ForegroundColor $(if ($pesoFinal) { "Green" } else { "Red" })

Write-Host ""
Write-Host "=== VALORES ESPERADOS ===" -ForegroundColor Yellow
Write-Host "Cantidad esperada: 1" -ForegroundColor White
Write-Host "Peso esperado: 5.384,03" -ForegroundColor White

Write-Host ""
Write-Host "=== VERIFICACIÓN ===" -ForegroundColor Yellow
if ($cantidadFinal -eq "1") {
    Write-Host "✓ Cantidad: CORRECTA" -ForegroundColor Green
} else {
    Write-Host "✗ Cantidad: INCORRECTA (esperado: 1, obtenido: $cantidadFinal)" -ForegroundColor Red
}

if ($pesoFinal -eq "5.384,03") {
    Write-Host "✓ Peso: CORRECTO" -ForegroundColor Green
} else {
    Write-Host "✗ Peso: INCORRECTO (esperado: 5.384,03, obtenido: $pesoFinal)" -ForegroundColor Red
}

Write-Host ""
Write-Host "Prueba completada." -ForegroundColor Green
