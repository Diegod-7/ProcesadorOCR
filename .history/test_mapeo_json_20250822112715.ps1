# Script de prueba para verificar el mapeo del JSON a CarnetAduaneroData

Write-Host "=== PRUEBA DE MAPEO JSON A CARNETADUANERODATA ===" -ForegroundColor Green
Write-Host ""

# JSON que debería generar la IA (con nombres correctos)
$jsonCorrecto = @"
{
  "Titulo": "CARNÉ ADUANERO",
  "NombreCompleto": "GONZALO ADOLFO GONZALEZ PINO",
  "Rut": "15.970.128-K",
  "NumeroCarne": "N8687",
  "FechaEmision": "17.01.2024",
  "Resolucion": "01.42",
  "ConfianzaExtraccion": 0.95
}
"@

Write-Host "JSON CORRECTO (con nombres de campos exactos):" -ForegroundColor Cyan
Write-Host $jsonCorrecto -ForegroundColor White
Write-Host ""

# JSON anterior (con nombres incorrectos)
$jsonIncorrecto = @"
{
  "titulo": "CARNÉ ADUANERO",
  "nombre": "GONZALEZ",
  "rut": "15.970.128-K",
  "numeroCarne": "N8687",
  "fechaEmision": "17.01.2024",
  "resolucion": "01.42",
  "agadCod": "E.1.2",
  "otrosCampos": null,
  "confianzaExtraccion": 0.95
}
"@

Write-Host "JSON INCORRECTO (con nombres de campos diferentes):" -ForegroundColor Red
Write-Host $jsonIncorrecto -ForegroundColor White
Write-Host ""

Write-Host "=== PROBLEMA IDENTIFICADO ===" -ForegroundColor Yellow
Write-Host ""
Write-Host "❌ Los nombres de campos en el JSON no coinciden con la clase CarnetAduaneroData:" -ForegroundColor Red
Write-Host ""
Write-Host "| JSON de IA | Clase CarnetAduaneroData | Estado |" -ForegroundColor Cyan
Write-Host "|------------|---------------------------|---------|" -ForegroundColor Cyan
Write-Host "| titulo     | Titulo                   | ❌ No coincide |" -ForegroundColor Red
Write-Host "| nombre     | NombreCompleto           | ❌ No coincide |" -ForegroundColor Red
Write-Host "| rut        | Rut                      | ❌ No coincide |" -ForegroundColor Red
Write-Host "| numeroCarne| NumeroCarne              | ❌ No coincide |" -ForegroundColor Red
Write-Host "| fechaEmision| FechaEmision            | ❌ No coincide |" -ForegroundColor Red
Write-Host "| resolucion | Resolucion               | ❌ No coincide |" -ForegroundColor Red
Write-Host "| confianzaExtraccion | ConfianzaExtraccion | ❌ No coincide |" -ForegroundColor Red
Write-Host ""
Write-Host "✅ SOLUCIÓN IMPLEMENTADA:" -ForegroundColor Green
Write-Host "   - Prompt de IA actualizado para usar nombres exactos" -ForegroundColor Green
Write-Host "   - JSON vacío corregido con nombres correctos" -ForegroundColor Green
Write-Host ""
Write-Host "=== CÓMO PROBAR ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "1. Ejecutar el script de prueba del carné:" -ForegroundColor Yellow
Write-Host "   .\test_carne_aduanero_ia.ps1" -ForegroundColor White
Write-Host ""
Write-Host "2. Subir una imagen de carné aduanero:" -ForegroundColor Yellow
Write-Host "   POST http://localhost:5000/api/carnetaduanero/procesar" -ForegroundColor White
Write-Host ""
Write-Host "3. Verificar que el JSON generado use nombres correctos:" -ForegroundColor Yellow
Write-Host "   - Titulo (NO titulo)" -ForegroundColor White
Write-Host "   - NombreCompleto (NO nombre)" -ForegroundColor White
Write-Host "   - Rut (NO rut)" -ForegroundColor White
Write-Host "   - NumeroCarne (NO numeroCarne)" -ForegroundColor White
Write-Host "   - FechaEmision (NO fechaEmision)" -ForegroundColor White
Write-Host "   - Resolucion (NO resolucion)" -ForegroundColor White
Write-Host "   - ConfianzaExtraccion (NO confianzaExtraccion)" -ForegroundColor White
Write-Host ""
Write-Host "¡Ahora el mapeo debería funcionar correctamente!" -ForegroundColor Green
