# Script para probar las mejoras en la extracción de campos
# del documento de Guía de Despacho

Write-Host "=== PRUEBA DE MEJORAS EN EXTRACCIÓN DE GUÍA DE DESPACHO ===" -ForegroundColor Green
Write-Host ""

# Leer el archivo de prueba
$archivoPrueba = "test_guia_despacho_mejorado.txt"
if (Test-Path $archivoPrueba) {
    $textoOcr = Get-Content $archivoPrueba -Raw
    Write-Host "Archivo de prueba cargado correctamente" -ForegroundColor Yellow
    Write-Host "Longitud del texto: $($textoOcr.Length) caracteres" -ForegroundColor Yellow
    Write-Host ""
} else {
    Write-Host "ERROR: No se encontró el archivo de prueba: $archivoPrueba" -ForegroundColor Red
    exit 1
}

# Simular las expresiones regulares mejoradas para los campos
Write-Host "=== PRUEBA DE EXTRACCIÓN DE CAMPOS ===" -ForegroundColor Cyan

# Detectar formato
$esFormatoAlexisMontenegro = $textoOcr -match "ALEXIS MONTENEGRO" -or $textoOcr -match "AGENCIA DE ADUANA Y ASESORIAS ELECTRONICA"
Write-Host "Formato Alexis Montenegro detectado: $esFormatoAlexisMontenegro" -ForegroundColor $(if ($esFormatoAlexisMontenegro) { "Green" } else { "Red" })

Write-Host ""

# 1. Número de guía
Write-Host "=== NÚMERO DE GUÍA ===" -ForegroundColor Yellow
$matchGuia = [regex]::Match($textoOcr, "N[°º](\d+)", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
if ($matchGuia.Success) {
    Write-Host "✓ Número de guía encontrado: $($matchGuia.Groups[1].Value)" -ForegroundColor Green
} else {
    Write-Host "✗ Número de guía NO encontrado" -ForegroundColor Red
}

# 2. RUT del emisor
Write-Host "=== RUT DEL EMISOR ===" -ForegroundColor Yellow
$matchRutEmisor = [regex]::Match($textoOcr, "R\.U\.T\s*\.\s*:\s*(\d{1,2}\.\d{3}\.\d{3}-\d)", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
if ($matchRutEmisor.Success) {
    Write-Host "✓ RUT del emisor encontrado: $($matchRutEmisor.Groups[1].Value)" -ForegroundColor Green
} else {
    Write-Host "✗ RUT del emisor NO encontrado" -ForegroundColor Red
}

# 3. Nombre del emisor
Write-Host "=== NOMBRE DEL EMISOR ===" -ForegroundColor Yellow
$matchNombreEmisor = [regex]::Match($textoOcr, "R\.U\.T\s*\.\s*:\s*\d{1,2}\.\d{3}\.\d{3}-\d\s+([A-ZÁÉÍÓÚÑ\s]+?)\s+GUIA", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
if ($matchNombreEmisor.Success) {
    Write-Host "✓ Nombre del emisor encontrado: $($matchNombreEmisor.Groups[1].Value.Trim())" -ForegroundColor Green
} else {
    Write-Host "✗ Nombre del emisor NO encontrado" -ForegroundColor Red
}

# 4. Giro del emisor
Write-Host "=== GIRO DEL EMISOR ===" -ForegroundColor Yellow
$matchGiroEmisor = [regex]::Match($textoOcr, "GIRO:\s*([A-ZÁÉÍÓÚÑ\s]+?)\s+Casa\s+Matriz:", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
if ($matchGiroEmisor.Success) {
    Write-Host "✓ Giro del emisor encontrado: $($matchGiroEmisor.Groups[1].Value.Trim())" -ForegroundColor Green
} else {
    Write-Host "✗ Giro del emisor NO encontrado" -ForegroundColor Red
}

# 5. Dirección del emisor
Write-Host "=== DIRECCIÓN DEL EMISOR ===" -ForegroundColor Yellow
$matchDireccionEmisor = [regex]::Match($textoOcr, "Casa\s+Matriz:\s*([A-ZÁÉÍÓÚÑ\s\d\-\.\-]+?)\s+N[°º]", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
if ($matchDireccionEmisor.Success) {
    Write-Host "✓ Dirección del emisor encontrada: $($matchDireccionEmisor.Groups[1].Value.Trim())" -ForegroundColor Green
} else {
    Write-Host "✗ Dirección del emisor NO encontrada" -ForegroundColor Red
}

# 6. Ciudad del emisor
Write-Host "=== CIUDAD DEL EMISOR ===" -ForegroundColor Yellow
$matchCiudadEmisor = [regex]::Match($textoOcr, "Casa\s+Matriz:\s*[A-ZÁÉÍÓÚÑ\s\d\-\.\-]+?([A-ZÁÉÍÓÚÑ]+)(?:\s+N[°º])", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
if ($matchCiudadEmisor.Success) {
    Write-Host "✓ Ciudad del emisor encontrada: $($matchCiudadEmisor.Groups[1].Value.Trim())" -ForegroundColor Green
} else {
    Write-Host "✗ Ciudad del emisor NO encontrada" -ForegroundColor Red
}

# 7. Nombre del receptor
Write-Host "=== NOMBRE DEL RECEPTOR ===" -ForegroundColor Yellow
$matchNombreReceptor = [regex]::Match($textoOcr, "Fecha\s+([A-ZÁÉÍÓÚÑ\s]+?)\s+COMERCIAL", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
if ($matchNombreReceptor.Success) {
    Write-Host "✓ Nombre del receptor encontrado: $($matchNombreReceptor.Groups[1].Value.Trim())" -ForegroundColor Green
} else {
    Write-Host "✗ Nombre del receptor NO encontrado" -ForegroundColor Red
}

# 8. RUT del receptor
Write-Host "=== RUT DEL RECEPTOR ===" -ForegroundColor Yellow
$matches = [regex]::Matches($textoOcr, "(\d{1,2}\.\d{3}\.\d{3}-\d)", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
if ($matches.Count -ge 2) {
    Write-Host "✓ RUT del receptor encontrado: $($matches[1].Groups[1].Value)" -ForegroundColor Green
} else {
    Write-Host "✗ RUT del receptor NO encontrado" -ForegroundColor Red
}

# 9. Dirección del receptor
Write-Host "=== DIRECCIÓN DEL RECEPTOR ===" -ForegroundColor Yellow
$matchDireccionReceptor = [regex]::Match($textoOcr, "LAS\s+ORQUIDEAS\s+IN\s*-\s*([A-ZÁÉÍÓÚÑ\s]+?)\s+Chudut:", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
if ($matchDireccionReceptor.Success) {
    $direccion = "LAS ORQUIDEAS IN - " + $matchDireccionReceptor.Groups[1].Value.Trim()
    Write-Host "✓ Dirección del receptor encontrada: $direccion" -ForegroundColor Green
} else {
    Write-Host "✗ Dirección del receptor NO encontrada" -ForegroundColor Red
}

# 10. Comuna del receptor
Write-Host "=== COMUNA DEL RECEPTOR ===" -ForegroundColor Yellow
$matchComunaReceptor = [regex]::Match($textoOcr, "Chudut:\s*([A-ZÁÉÍÓÚÑ\s]+?)\s+Comune:", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
if ($matchComunaReceptor.Success) {
    Write-Host "✓ Comuna del receptor encontrada: $($matchComunaReceptor.Groups[1].Value.Trim())" -ForegroundColor Green
} else {
    Write-Host "✗ Comuna del receptor NO encontrada" -ForegroundColor Red
}

# 11. Ciudad del receptor
Write-Host "=== CIUDAD DEL RECEPTOR ===" -ForegroundColor Yellow
$matchCiudadReceptor = [regex]::Match($textoOcr, "Comune:\s*([A-ZÁÉÍÓÚÑ\s]+?)\s+Origen", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
if ($matchCiudadReceptor.Success) {
    Write-Host "✓ Ciudad del receptor encontrada: $($matchCiudadReceptor.Groups[1].Value.Trim())" -ForegroundColor Green
} else {
    Write-Host "✗ Ciudad del receptor NO encontrada" -ForegroundColor Red
}

# 12. Transportista
Write-Host "=== TRANSPORTISTA ===" -ForegroundColor Yellow
$matchTransportista = [regex]::Match($textoOcr, "Fecha\s+([A-ZÁÉÍÓÚÑ\s]+?)\s+COMERCIAL", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
if ($matchTransportista.Success) {
    Write-Host "✓ Transportista encontrado: $($matchTransportista.Groups[1].Value.Trim())" -ForegroundColor Green
} else {
    Write-Host "✗ Transportista NO encontrado" -ForegroundColor Red
}

# 13. Origen
Write-Host "=== ORIGEN ===" -ForegroundColor Yellow
$matchOrigen = [regex]::Match($textoOcr, "Origen\s+([A-ZÁÉÍÓÚÑ\s]+?)\s+MEDLOG", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
if ($matchOrigen.Success) {
    Write-Host "✓ Origen encontrado: $($matchOrigen.Groups[1].Value.Trim())" -ForegroundColor Green
} else {
    Write-Host "✗ Origen NO encontrado" -ForegroundColor Red
}

# 14. Peso
Write-Host "=== PESO ===" -ForegroundColor Yellow
$matchPeso = [regex]::Match($textoOcr, "(\d+\.\d{3},\d{2})", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
if ($matchPeso.Success) {
    Write-Host "✓ Peso encontrado: $($matchPeso.Groups[1].Value)" -ForegroundColor Green
} else {
    Write-Host "✗ Peso NO encontrado" -ForegroundColor Red
}

# 15. Cantidad de bultos
Write-Host "=== CANTIDAD DE BULTOS ===" -ForegroundColor Yellow
$matchCantidadBultos = [regex]::Match($textoOcr, "(\d+)\s+CAJAS", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
if ($matchCantidadBultos.Success) {
    Write-Host "✓ Cantidad de bultos encontrada: $($matchCantidadBultos.Groups[1].Value)" -ForegroundColor Green
} else {
    Write-Host "✗ Cantidad de bultos NO encontrada" -ForegroundColor Red
}

# 16. Tipo de bulto
Write-Host "=== TIPO DE BULTO ===" -ForegroundColor Yellow
$matchTipoBulto = [regex]::Match($textoOcr, "(\d+)\s+CAJAS\s+([A-ZÁÉÍÓÚÑ\s]+?)(?:\s+-|\s+PELOTAS|\s*$)", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
if ($matchTipoBulto.Success) {
    $tipoBulto = "$($matchTipoBulto.Groups[1].Value) CAJAS $($matchTipoBulto.Groups[2].Value)".Trim()
    Write-Host "✓ Tipo de bulto encontrado: $tipoBulto" -ForegroundColor Green
} else {
    Write-Host "✗ Tipo de bulto NO encontrado" -ForegroundColor Red
}

# 17. Observaciones
Write-Host "=== OBSERVACIONES ===" -ForegroundColor Yellow
$matchObservaciones = [regex]::Match($textoOcr, "Observaciones:\s*([A-ZÁÉÍÓÚÑ\s\d\-\.\-]+?)(?:\s+CAAU|\s*$)", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
if ($matchObservaciones.Success) {
    Write-Host "✓ Observaciones encontradas: $($matchObservaciones.Groups[1].Value.Trim())" -ForegroundColor Green
} else {
    Write-Host "✗ Observaciones NO encontradas" -ForegroundColor Red
}

Write-Host ""
Write-Host "=== RESUMEN DE EXTRACCIÓN ===" -ForegroundColor Yellow

# Contar campos extraídos exitosamente
$camposExtraidos = 0
$totalCampos = 17

if ($matchGuia.Success) { $camposExtraidos++ }
if ($matchRutEmisor.Success) { $camposExtraidos++ }
if ($matchNombreEmisor.Success) { $camposExtraidos++ }
if ($matchGiroEmisor.Success) { $camposExtraidos++ }
if ($matchDireccionEmisor.Success) { $camposExtraidos++ }
if ($matchCiudadEmisor.Success) { $camposExtraidos++ }
if ($matchNombreReceptor.Success) { $camposExtraidos++ }
if ($matches.Count -ge 2) { $camposExtraidos++ }
if ($matchDireccionReceptor.Success) { $camposExtraidos++ }
if ($matchComunaReceptor.Success) { $camposExtraidos++ }
if ($matchCiudadReceptor.Success) { $camposExtraidos++ }
if ($matchTransportista.Success) { $camposExtraidos++ }
if ($matchOrigen.Success) { $camposExtraidos++ }
if ($matchPeso.Success) { $camposExtraidos++ }
if ($matchCantidadBultos.Success) { $camposExtraidos++ }
if ($matchTipoBulto.Success) { $camposExtraidos++ }
if ($matchObservaciones.Success) { $camposExtraidos++ }

$porcentajeExito = [math]::Round(($camposExtraidos / $totalCampos) * 100, 2)
Write-Host "Campos extraídos exitosamente: $camposExtraidos de $totalCampos ($porcentajeExito%)" -ForegroundColor $(if ($porcentajeExito -ge 80) { "Green" } elseif ($porcentajeExito -ge 60) { "Yellow" } else { "Red" })

Write-Host ""
Write-Host "=== VALORES ESPERADOS ===" -ForegroundColor Yellow
Write-Host "Número de guía esperado: 44172" -ForegroundColor White
Write-Host "RUT del emisor esperado: 13.021.175-5" -ForegroundColor White
Write-Host "Nombre del emisor esperado: ALEXIS MONTENEGRO PONCE" -ForegroundColor White
Write-Host "Giro del emisor esperado: AGENCIA DE ADUANA Y ASESORIAS ELECTRONICA" -ForegroundColor White
Write-Host "Dirección del emisor esperada: ESMERALDA 940 OF.111-B - VALPARAISO" -ForegroundColor White
Write-Host "Ciudad del emisor esperada: VALPARAISO" -ForegroundColor White
Write-Host "Nombre del receptor esperado: COMERCIAL CASA NOVEDAD LIMITADA" -ForegroundColor White
Write-Host "Dirección del receptor esperada: LAS ORQUIDEAS IN - COQUIMBO" -ForegroundColor White
Write-Host "Comuna del receptor esperada: COQUIMBO" -ForegroundColor White
Write-Host "Ciudad del receptor esperada: COQUIMBO" -ForegroundColor White
Write-Host "Peso esperado: 5.384,03" -ForegroundColor White
Write-Host "Cantidad de bultos esperada: 784" -ForegroundColor White

Write-Host ""
Write-Host "Prueba completada." -ForegroundColor Green
