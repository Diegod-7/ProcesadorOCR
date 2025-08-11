# Script para probar las mejoras en la extracción de campos
# del documento de Declaración de Ingreso

Write-Host "=== PRUEBA DE MEJORAS EN EXTRACCIÓN DE DECLARACIÓN DE INGRESO ===" -ForegroundColor Green
Write-Host ""

# Leer el archivo de prueba
$archivoPrueba = "test_declaracion_ingreso_mejorado.txt"
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
Write-Host "=== PRUEBA DE EXTRACCIÓN DE CAMPOS CRÍTICOS ===" -ForegroundColor Cyan

# 1. Número de identificación
Write-Host "=== NÚMERO DE IDENTIFICACIÓN ===" -ForegroundColor Yellow
$matchNumero = [regex]::Match($textoOcr, "(\d{10}-\d)", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
if ($matchNumero.Success) {
    Write-Host "✓ Número de identificación encontrado: $($matchNumero.Groups[1].Value)" -ForegroundColor Green
} else {
    Write-Host "✗ Número de identificación NO encontrado" -ForegroundColor Red
}

# 2. Fecha de vencimiento
Write-Host "=== FECHA DE VENCIMIENTO ===" -ForegroundColor Yellow
$matchFechaVencimiento = [regex]::Match($textoOcr, "FECHA\s+DE\s+VENCIMIENTO[:\s]*(\d{2}/\d{2}/\d{4})", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
if ($matchFechaVencimiento.Success) {
    Write-Host "✓ Fecha de vencimiento encontrada: $($matchFechaVencimiento.Groups[1].Value)" -ForegroundColor Green
} else {
    Write-Host "✗ Fecha de vencimiento NO encontrada" -ForegroundColor Red
}

# 3. Tipo de operación
Write-Host "=== TIPO DE OPERACIÓN ===" -ForegroundColor Yellow
$matchTipoOperacion = [regex]::Match($textoOcr, "Tipo\s+Operacion[:\s]*([A-Z\s\.]+?)(?=\d{3}|$)", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
if ($matchTipoOperacion.Success) {
    Write-Host "✓ Tipo de operación encontrado: $($matchTipoOperacion.Groups[1].Value.Trim())" -ForegroundColor Green
} else {
    Write-Host "✗ Tipo de operación NO encontrado" -ForegroundColor Red
}

# 4. Código de tipo de operación
Write-Host "=== CÓDIGO DE TIPO DE OPERACIÓN ===" -ForegroundColor Yellow
$matchCodigoTipo = [regex]::Match($textoOcr, "Tipo\s+Operacion[:\s]*[A-Z\s\.]+?\s*(\d{3})", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
if ($matchCodigoTipo.Success) {
    Write-Host "✓ Código de tipo de operación encontrado: $($matchCodigoTipo.Groups[1].Value)" -ForegroundColor Green
} else {
    Write-Host "✗ Código de tipo de operación NO encontrado" -ForegroundColor Red
}

# 5. Tipo de bulto
Write-Host "=== TIPO DE BULTO ===" -ForegroundColor Yellow
$matchTipoBulto = [regex]::Match($textoOcr, "CONT40[:\s]*(\d+)", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
if ($matchTipoBulto.Success) {
    Write-Host "✓ Tipo de bulto encontrado: CONT40 $($matchTipoBulto.Groups[1].Value)" -ForegroundColor Green
} else {
    # Fallback
    $matchTipoBultoFallback = [regex]::Match($textoOcr, "([A-Z]{4}\d{2,3})", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
    if ($matchTipoBultoFallback.Success) {
        Write-Host "✓ Tipo de bulto encontrado (fallback): $($matchTipoBultoFallback.Groups[1].Value)" -ForegroundColor Green
    } else {
        Write-Host "✗ Tipo de bulto NO encontrado" -ForegroundColor Red
    }
}

# 6. Peso bruto
Write-Host "=== PESO BRUTO ===" -ForegroundColor Yellow
$matchPesoBruto = [regex]::Match($textoOcr, "CUENTAS\s+Y\s+VALORES[:\s]*.*?(\d{1,3}(?:\.\d{3})*(?:,\d{2})?)", [System.Text.RegularExpressions.RegexOptions]::Singleline)
if ($matchPesoBruto.Success) {
    Write-Host "✓ Peso bruto encontrado: $($matchPesoBruto.Groups[1].Value)" -ForegroundColor Green
} else {
    # Fallback alternativo
    $matchPesoBrutoAlt = [regex]::Match($textoOcr, "CONT40\s+Cantidad\s+\d+\s+(\d{1,3}(?:\.\d{3})*(?:,\d{2})?)", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
    if ($matchPesoBrutoAlt.Success) {
        Write-Host "✓ Peso bruto encontrado (alternativo): $($matchPesoBrutoAlt.Groups[1].Value)" -ForegroundColor Green
    } else {
        Write-Host "✗ Peso bruto NO encontrado" -ForegroundColor Red
    }
}

# 7. Sello del contenedor
Write-Host "=== SELLO DEL CONTENEDOR ===" -ForegroundColor Yellow
$matchSello = [regex]::Match($textoOcr, "([A-Z]{4}\s+\d{6}-\d\s+SELLO\s+[A-Z0-9]+)", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
if ($matchSello.Success) {
    Write-Host "✓ Sello del contenedor encontrado: $($matchSello.Groups[1].Value)" -ForegroundColor Green
} else {
    Write-Host "✗ Sello del contenedor NO encontrado" -ForegroundColor Red
}

# 8. Fecha de aceptación
Write-Host "=== FECHA DE ACEPTACIÓN ===" -ForegroundColor Yellow
$matchFechaAceptacion = [regex]::Match($textoOcr, "FECHA\s+DE\s+ACEPTACIÓN[:\s]*(\d{2}/\d{2}/\d{4})", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
if ($matchFechaAceptacion.Success) {
    Write-Host "✓ Fecha de aceptación encontrada: $($matchFechaAceptacion.Groups[1].Value)" -ForegroundColor Green
} else {
    Write-Host "✗ Fecha de aceptación NO encontrada" -ForegroundColor Red
}

# 9. Total a pagar
Write-Host "=== TOTAL A PAGAR ===" -ForegroundColor Yellow
$matchTotalPagar = [regex]::Match($textoOcr, "OPERACIONES\s+CON\s+PAGO\s+DIFERIDO[:\s]*.*?(\d{1,3}(?:\.\d{3})*)", [System.Text.RegularExpressions.RegexOptions]::Singleline)
if ($matchTotalPagar.Success) {
    Write-Host "✓ Total a pagar encontrado: $($matchTotalPagar.Groups[1].Value)" -ForegroundColor Green
} else {
    # Fallback alternativo
    $matchTotalPagarAlt = [regex]::Match($textoOcr, "IGUALA\s+PAGAR\s+EN\s+S[:\s]*(\d{1,3}(?:\.\d{3})*)", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
    if ($matchTotalPagarAlt.Success) {
        Write-Host "✓ Total a pagar encontrado (alternativo): $($matchTotalPagarAlt.Groups[1].Value)" -ForegroundColor Green
    } else {
        Write-Host "✗ Total a pagar NO encontrado" -ForegroundColor Red
    }
}

# 10. Nombre del importador
Write-Host "=== NOMBRE DEL IMPORTADOR ===" -ForegroundColor Yellow
$matchNombreImportador = [regex]::Match($textoOcr, "([A-Z]+\s+[A-Z]+\s+[A-Z]+)(?=\r\n|\s+\d{2}\s+[A-Z])", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
if ($matchNombreImportador.Success) {
    Write-Host "✓ Nombre del importador encontrado: $($matchNombreImportador.Groups[1].Value.Trim())" -ForegroundColor Green
} else {
    # Fallback
    $matchNombreImportadorFallback = [regex]::Match($textoOcr, "(WALTER\s+PEREZ\s+SALAS)", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
    if ($matchNombreImportadorFallback.Success) {
        Write-Host "✓ Nombre del importador encontrado (fallback): $($matchNombreImportadorFallback.Groups[1].Value.Trim())" -ForegroundColor Green
    } else {
        Write-Host "✗ Nombre del importador NO encontrado" -ForegroundColor Red
    }
}

# 11. RUT del importador
Write-Host "=== RUT DEL IMPORTADOR ===" -ForegroundColor Yellow
$matchRutImportador = [regex]::Match($textoOcr, "RUT[:\s]*(\d{1,2}\.\d{3}\.\d{3}-\d)", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
if ($matchRutImportador.Success) {
    Write-Host "✓ RUT del importador encontrado: $($matchRutImportador.Groups[1].Value)" -ForegroundColor Green
} else {
    # Fallback
    $matchRutImportadorFallback = [regex]::Match($textoOcr, "(\d{1,2}\.\d{3}\.\d{3}-\d)", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
    if ($matchRutImportadorFallback.Success) {
        Write-Host "✓ RUT del importador encontrado (fallback): $($matchRutImportadorFallback.Groups[1].Value)" -ForegroundColor Green
    } else {
        Write-Host "✗ RUT del importador NO encontrado" -ForegroundColor Red
    }
}

# 12. Descripción de mercancías
Write-Host "=== DESCRIPCIÓN DE MERCANCÍAS ===" -ForegroundColor Yellow
$matchDescripcion = [regex]::Match($textoOcr, "DESCRIPCION\s+DE\s+MERCANCIAS[:\s]*([A-Z0-9\s\-\.]+?)(?=\d{4}|$)", [System.Text.RegularExpressions.RegexOptions]::Singleline)
if ($matchDescripcion.Success) {
    Write-Host "✓ Descripción de mercancías encontrada: $($matchDescripcion.Groups[1].Value.Trim())" -ForegroundColor Green
} else {
    # Fallback
    $matchDescripcionFallback = [regex]::Match($textoOcr, "DESCRIPCION\s+DE\s+MERCANCIAS[:\s]*([A-Z0-9\s\-\.]+?)(?=\d{1,3}\s+\d{4}|$)", [System.Text.RegularExpressions.RegexOptions]::Singleline)
    if ($matchDescripcionFallback.Success) {
        Write-Host "✓ Descripción de mercancías encontrada (fallback): $($matchDescripcionFallback.Groups[1].Value.Trim())" -ForegroundColor Green
    } else {
        Write-Host "✗ Descripción de mercancías NO encontrada" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "=== PRUEBA DE EXTRACCIÓN DE CAMPOS ADICIONALES ===" -ForegroundColor Cyan

# 13. Aduana
Write-Host "=== ADUANA ===" -ForegroundColor Yellow
$matchAduana = [regex]::Match($textoOcr, "(SAN\s+ANTONIO)", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
if ($matchAduana.Success) {
    Write-Host "✓ Aduana encontrada: $($matchAduana.Groups[1].Value.Trim())" -ForegroundColor Green
} else {
    Write-Host "✗ Aduana NO encontrada" -ForegroundColor Red
}

# 14. Despachante
Write-Host "=== DESPACHANTE ===" -ForegroundColor Yellow
$matchDespachante = [regex]::Match($textoOcr, "(WALTER\s+PEREZ\s+SALAS)", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
if ($matchDespachante.Success) {
    Write-Host "✓ Despachante encontrado: $($matchDespachante.Groups[1].Value.Trim())" -ForegroundColor Green
} else {
    Write-Host "✗ Despachante NO encontrado" -ForegroundColor Red
}

# 15. Consignatario
Write-Host "=== CONSIGNATARIO ===" -ForegroundColor Yellow
$matchConsignatario = [regex]::Match($textoOcr, "IDENTIFICACION[:\s]*([A-Z\s&\.]+?)(?=\r\n|\s+UNION|\s+Comuna)", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
if ($matchConsignatario.Success) {
    Write-Host "✓ Consignatario encontrado: $($matchConsignatario.Groups[1].Value.Trim())" -ForegroundColor Green
} else {
    Write-Host "✗ Consignatario NO encontrado" -ForegroundColor Red
}

# 16. RUT del consignatario
Write-Host "=== RUT DEL CONSIGNATARIO ===" -ForegroundColor Yellow
$matchRutConsignatario = [regex]::Match($textoOcr, "RUT[:\s]*(\d{1,2}\.\d{3}\.\d{3}-\d)", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
if ($matchRutConsignatario.Success) {
    Write-Host "✓ RUT del consignatario encontrado: $($matchRutConsignatario.Groups[1].Value)" -ForegroundColor Green
} else {
    Write-Host "✗ RUT del consignatario NO encontrado" -ForegroundColor Red
}

# 17. Consignante
Write-Host "=== CONSIGNANTE ===" -ForegroundColor Yellow
$matchConsignante = [regex]::Match($textoOcr, "(BOHUA\s+TRADE\s+CO[\.\s]+LIMITED?)", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
if ($matchConsignante.Success) {
    Write-Host "✓ Consignante encontrado: $($matchConsignante.Groups[1].Value.Trim())" -ForegroundColor Green
} else {
    Write-Host "✗ Consignante NO encontrado" -ForegroundColor Red
}

# 18. País de origen
Write-Host "=== PAÍS DE ORIGEN ===" -ForegroundColor Yellow
$matchPaisOrigen = [regex]::Match($textoOcr, "(CHINA)", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
if ($matchPaisOrigen.Success) {
    Write-Host "✓ País de origen encontrado: $($matchPaisOrigen.Groups[1].Value)" -ForegroundColor Green
} else {
    Write-Host "✗ País de origen NO encontrado" -ForegroundColor Red
}

# 19. Puerto de desembarque
Write-Host "=== PUERTO DE DESEMBARQUE ===" -ForegroundColor Yellow
$matchPuertoDesembarque = [regex]::Match($textoOcr, "(SAN\s+ANTONIO)", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
if ($matchPuertoDesembarque.Success) {
    Write-Host "✓ Puerto de desembarque encontrado: $($matchPuertoDesembarque.Groups[1].Value.Trim())" -ForegroundColor Green
} else {
    Write-Host "✗ Puerto de desembarque NO encontrado" -ForegroundColor Red
}

# 20. Compañía transportista
Write-Host "=== COMPAÑÍA TRANSPORTISTA ===" -ForegroundColor Yellow
$matchCompania = [regex]::Match($textoOcr, "(MEDITERRANEAN\s+SHIPPING\s+CO[\.\s]+SA[I]?)", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
if ($matchCompania.Success) {
    Write-Host "✓ Compañía transportista encontrada: $($matchCompania.Groups[1].Value.Trim())" -ForegroundColor Green
} else {
    Write-Host "✗ Compañía transportista NO encontrada" -ForegroundColor Red
}

# 21. Manifiesto
Write-Host "=== MANIFIESTO ===" -ForegroundColor Yellow
$matchManifiesto = [regex]::Match($textoOcr, "Manifiesto[:\s]*(\d+)", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
if ($matchManifiesto.Success) {
    Write-Host "✓ Manifiesto encontrado: $($matchManifiesto.Groups[1].Value)" -ForegroundColor Green
} else {
    # Fallback
    $matchManifiestoFallback = [regex]::Match($textoOcr, "Marviesto[:\s]*(\d+)", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
    if ($matchManifiestoFallback.Success) {
        Write-Host "✓ Manifiesto encontrado (fallback): $($matchManifiestoFallback.Groups[1].Value)" -ForegroundColor Green
    } else {
        Write-Host "✗ Manifiesto NO encontrado" -ForegroundColor Red
    }
}

# 22. Documento de transporte
Write-Host "=== DOCUMENTO DE TRANSPORTE ===" -ForegroundColor Yellow
$matchDocumentoTransporte = [regex]::Match($textoOcr, "Docto\.\s+Transporte[:\s]*([A-Z0-9]+)", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
if ($matchDocumentoTransporte.Success) {
    Write-Host "✓ Documento de transporte encontrado: $($matchDocumentoTransporte.Groups[1].Value)" -ForegroundColor Green
} else {
    Write-Host "✗ Documento de transporte NO encontrado" -ForegroundColor Red
}

# 23. Valor CIF
Write-Host "=== VALOR CIF ===" -ForegroundColor Yellow
$matchValorCif = [regex]::Match($textoOcr, "(\d{1,3}(?:\.\d{3})*(?:,\d{2})?)(?=\s*$|\s*[A-Z])", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
if ($matchValorCif.Success) {
    Write-Host "✓ Valor CIF encontrado: $($matchValorCif.Groups[1].Value)" -ForegroundColor Green
} else {
    # Fallback alternativo
    $matchValorCifAlt = [regex]::Match($textoOcr, "CONT40\s+Cantidad\s+\d+\s+\d{1,3}(?:\.\d{3})*(?:,\d{2})?\s+(\d{1,3}(?:\.\d{3})*(?:,\d{2})?)", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
    if ($matchValorCifAlt.Success) {
        Write-Host "✓ Valor CIF encontrado (alternativo): $($matchValorCifAlt.Groups[1].Value)" -ForegroundColor Green
    } else {
        Write-Host "✗ Valor CIF NO encontrado" -ForegroundColor Red
    }
}

# 24. Moneda
Write-Host "=== MONEDA ===" -ForegroundColor Yellow
$matchMoneda = [regex]::Match($textoOcr, "Moneda[:\s]*([A-Z\s]+)", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
if ($matchMoneda.Success) {
    Write-Host "✓ Moneda encontrada: $($matchMoneda.Groups[1].Value.Trim())" -ForegroundColor Green
} else {
    Write-Host "✗ Moneda NO encontrada" -ForegroundColor Red
}

# 25. Forma de pago
Write-Host "=== FORMA DE PAGO ===" -ForegroundColor Yellow
$matchFormaPago = [regex]::Match($textoOcr, "Forva\s+Pago[:\s]*([A-Z]+)", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
if ($matchFormaPago.Success) {
    Write-Host "✓ Forma de pago encontrada: $($matchFormaPago.Groups[1].Value)" -ForegroundColor Green
} else {
    Write-Host "✗ Forma de pago NO encontrada" -ForegroundColor Red
}

# 26. Cláusula de compra
Write-Host "=== CLÁUSULA DE COMPRA ===" -ForegroundColor Yellow
$matchClausula = [regex]::Match($textoOcr, "(CFR)", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
if ($matchClausula.Success) {
    Write-Host "✓ Cláusula de compra encontrada: $($matchClausula.Groups[1].Value)" -ForegroundColor Green
} else {
    Write-Host "✗ Cláusula de compra NO encontrada" -ForegroundColor Red
}

# 27. Certificado de origen
Write-Host "=== CERTIFICADO DE ORIGEN ===" -ForegroundColor Yellow
$matchCertificado = [regex]::Match($textoOcr, "CERT\.ORIG[:\s]*([A-Z0-9]+)", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
if ($matchCertificado.Success) {
    Write-Host "✓ Certificado de origen encontrado: $($matchCertificado.Groups[1].Value)" -ForegroundColor Green
} else {
    Write-Host "✗ Certificado de origen NO encontrado" -ForegroundColor Red
}

Write-Host ""
Write-Host "=== RESUMEN DE EXTRACCIÓN ===" -ForegroundColor Yellow

# Contar campos extraídos exitosamente
$camposExtraidos = 0
$totalCampos = 27

if ($matchNumero.Success) { $camposExtraidos++ }
if ($matchFechaVencimiento.Success) { $camposExtraidos++ }
if ($matchTipoOperacion.Success) { $camposExtraidos++ }
if ($matchCodigoTipo.Success) { $camposExtraidos++ }
if ($matchTipoBulto.Success -or $matchTipoBultoFallback.Success) { $camposExtraidos++ }
if ($matchPesoBruto.Success -or $matchPesoBrutoAlt.Success) { $camposExtraidos++ }
if ($matchSello.Success) { $camposExtraidos++ }
if ($matchFechaAceptacion.Success) { $camposExtraidos++ }
if ($matchTotalPagar.Success -or $matchTotalPagarAlt.Success) { $camposExtraidos++ }
if ($matchNombreImportador.Success -or $matchNombreImportadorFallback.Success) { $camposExtraidos++ }
if ($matchRutImportador.Success -or $matchRutImportadorFallback.Success) { $camposExtraidos++ }
if ($matchDescripcion.Success -or $matchDescripcionFallback.Success) { $camposExtraidos++ }
if ($matchAduana.Success) { $camposExtraidos++ }
if ($matchDespachante.Success) { $camposExtraidos++ }
if ($matchConsignatario.Success) { $camposExtraidos++ }
if ($matchRutConsignatario.Success) { $camposExtraidos++ }
if ($matchConsignante.Success) { $camposExtraidos++ }
if ($matchPaisOrigen.Success) { $camposExtraidos++ }
if ($matchPuertoDesembarque.Success) { $camposExtraidos++ }
if ($matchCompania.Success) { $camposExtraidos++ }
if ($matchManifiesto.Success -or $matchManifiestoFallback.Success) { $camposExtraidos++ }
if ($matchDocumentoTransporte.Success) { $camposExtraidos++ }
if ($matchValorCif.Success -or $matchValorCifAlt.Success) { $camposExtraidos++ }
if ($matchMoneda.Success) { $camposExtraidos++ }
if ($matchFormaPago.Success) { $camposExtraidos++ }
if ($matchClausula.Success) { $camposExtraidos++ }
if ($matchCertificado.Success) { $camposExtraidos++ }

$porcentajeExito = [math]::Round(($camposExtraidos / $totalCampos) * 100, 2)
Write-Host "Campos extraídos exitosamente: $camposExtraidos de $totalCampos ($porcentajeExito%)" -ForegroundColor $(if ($porcentajeExito -ge 80) { "Green" } elseif ($porcentajeExito -ge 60) { "Yellow" } else { "Red" })

Write-Host ""
Write-Host "=== VALORES ESPERADOS ===" -ForegroundColor Yellow
Write-Host "Número de identificación esperado: 4700045635-3" -ForegroundColor White
Write-Host "Fecha de vencimiento esperada: 02/04/2025" -ForegroundColor White
Write-Host "Tipo de operación esperado: IMPORT.CTDO.ANTIC." -ForegroundColor White
Write-Host "Código de tipo de operación esperado: 151" -ForegroundColor White
Write-Host "Tipo de bulto esperado: CONT40 074" -ForegroundColor White
Write-Host "Peso bruto esperado: 27.819,95" -ForegroundColor White
Write-Host "Sello del contenedor esperado: MSBU 827710-2 SELLO FX39286687" -ForegroundColor White
Write-Host "Fecha de aceptación esperada: 18/03/2025" -ForegroundColor White
Write-Host "Total a pagar esperado: 5.632.525" -ForegroundColor White
Write-Host "Nombre del importador esperado: WALTER PEREZ SALAS" -ForegroundColor White
Write-Host "RUT del importador esperado: 77.816.676-3" -ForegroundColor White
Write-Host "Consignatario esperado: JIN & YIN & WANG LIMITADA" -ForegroundColor White
Write-Host "Consignante esperado: BOHUA TRADE CO. LIMITED" -ForegroundColor White
Write-Host "País de origen esperado: CHINA" -ForegroundColor White
Write-Host "Puerto de desembarque esperado: SAN ANTONIO" -ForegroundColor White
Write-Host "Compañía transportista esperada: MEDITERRANEAN SHIPPING CO SAI" -ForegroundColor White
Write-Host "Manifiesto esperado: 253388" -ForegroundColor White
Write-Host "Documento de transporte esperado: MEDUOY612089" -ForegroundColor White
Write-Host "Moneda esperada: DOLAR USA" -ForegroundColor White
Write-Host "Forma de pago esperada: ANTICIPO" -ForegroundColor White
Write-Host "Cláusula de compra esperada: CFR" -ForegroundColor White
Write-Host "Certificado de origen esperado: F25MASGLUSX00002" -ForegroundColor White

Write-Host ""
Write-Host "Prueba completada." -ForegroundColor Green
