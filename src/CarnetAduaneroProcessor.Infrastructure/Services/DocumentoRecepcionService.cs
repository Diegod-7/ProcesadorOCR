using CarnetAduaneroProcessor.Core.Models;
using CarnetAduaneroProcessor.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Drawing;
using Azure.AI.Vision.ImageAnalysis;

namespace CarnetAduaneroProcessor.Infrastructure.Services
{
    /// <summary>
    /// Servicio para procesar Documentos de Recepción (DR)
    /// </summary>
    public class DocumentoRecepcionService : IDocumentoRecepcionService
    {
        private readonly ILogger<DocumentoRecepcionService> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _azureVisionKey;
        private readonly string _azureVisionEndpoint;

        public DocumentoRecepcionService(ILogger<DocumentoRecepcionService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            
            // Configuración de Azure Computer Vision
            _azureVisionKey = configuration["AzureVision:Key"] ?? string.Empty;
            _azureVisionEndpoint = configuration["AzureVision:Endpoint"] ?? string.Empty;
        }

        /// <summary>
        /// Extrae datos de un archivo PNG de Documento de Recepción
        /// </summary>
        public async Task<DocumentoRecepcion> ExtraerDatosAsync(string filePath)
        {
            try
            {
                _logger.LogInformation("Iniciando extracción de datos de DR desde archivo: {FilePath}", filePath);

                // Validar archivo
                if (!await ValidarPngAsync(filePath))
                {
                    throw new ArgumentException("El archivo no es un PNG válido");
                }

                // Obtener información del archivo
                var fileInfo = new FileInfo(filePath);
                var hash = await CalcularHashArchivoAsync(filePath);

                // Extraer datos usando método manual
                var documento = await ExtraerManualAsync(filePath);

                // Configurar metadatos del archivo
                documento.NombreArchivo = fileInfo.Name;
                documento.RutaArchivo = filePath;
                documento.TamanioArchivo = fileInfo.Length;
                documento.HashArchivo = hash;
                documento.MetodoExtraccion = "Extracción Manual";

                _logger.LogInformation("Extracción completada para DR: {NumeroDocumento}", documento.NumeroDocumento);

                return documento;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extrayendo datos de DR desde archivo: {FilePath}", filePath);
                throw;
            }
        }

        /// <summary>
        /// Extrae datos de un stream de archivo PNG
        /// </summary>
        public async Task<DocumentoRecepcion> ExtraerDatosAsync(Stream fileStream, string fileName)
        {
            try
            {
                _logger.LogInformation("Iniciando extracción de datos de DR desde stream: {FileName}", fileName);

                // Validar stream
                if (!await ValidarPngAsync(fileStream))
                {
                    throw new ArgumentException("El stream no contiene un archivo PNG válido");
                }

                // Calcular hash
                var hash = await CalcularHashArchivoAsync(fileStream);

                // Extraer datos usando método manual
                var documento = await ExtraerManualAsync(fileStream);

                // Configurar metadatos
                documento.NombreArchivo = fileName;
                documento.HashArchivo = hash;
                documento.MetodoExtraccion = "Extracción Manual";

                _logger.LogInformation("Extracción completada para DR: {NumeroDocumento}", documento.NumeroDocumento);

                return documento;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extrayendo datos de DR desde stream: {FileName}", fileName);
                throw;
            }
        }

        /// <summary>
        /// Extrae datos de un array de bytes
        /// </summary>
        public async Task<DocumentoRecepcion> ExtraerDatosAsync(byte[] fileBytes, string fileName)
        {
            using var stream = new MemoryStream(fileBytes);
            return await ExtraerDatosAsync(stream, fileName);
        }

        /// <summary>
        /// Procesa texto OCR para extraer datos de documento de recepción
        /// </summary>
        public async Task<DocumentoRecepcion> ProcesarTextoOcrAsync(string textoOcr)
        {
            _logger.LogInformation("Iniciando procesamiento de texto OCR para documento de recepción");

            var documento = new DocumentoRecepcion();

            try
            {
                // Normalizar el texto OCR
                var textoNormalizado = NormalizarTexto(textoOcr);
                _logger.LogInformation("Texto normalizado: {Texto}", textoNormalizado);

                // Extraer campos críticos
                await ExtraerCamposCriticosAsync(documento, textoNormalizado);

                // Extraer campos adicionales
                await ExtraerCamposAdicionalesAsync(documento, textoNormalizado);

                // Guardar texto extraído
                documento.TextoExtraido = textoNormalizado;
                documento.ConfianzaExtraccion = 0.8m;

                if (!documento.EsValido)
                {
                    documento.Comentarios = "No se pudieron extraer todos los campos requeridos del documento de recepción";
                    _logger.LogWarning("Extracción incompleta: {Error}", documento.Comentarios);
                }
                else
                {
                    _logger.LogInformation("Procesamiento completado exitosamente para DR: {NumeroDocumento}", documento.NumeroDocumento);
                }
            }
            catch (Exception ex)
            {
                documento.Comentarios = $"Error durante el procesamiento: {ex.Message}";
                _logger.LogError(ex, "Error procesando texto OCR para documento de recepción");
            }

            return await Task.FromResult(documento);
        }

        /// <summary>
        /// Extrae campos críticos del documento
        /// </summary>
        private async Task ExtraerCamposCriticosAsync(DocumentoRecepcion documento, string texto)
        {
            await Task.Run(() =>
            {
                _logger.LogInformation("Iniciando extracción de campos críticos desde texto OCR para DR");

                // 1. Número del documento (ej: 2025-10718) - buscar patrones más flexibles
                var numeroDocumentoMatch = Regex.Match(texto, @"(?:Nº|N°|No\.?)\s*D\.?R\.?\s*\.?\s*:?\s*(\d{4}[-\s]*\d+)");
                if (numeroDocumentoMatch.Success)
                {
                    documento.NumeroDocumento = numeroDocumentoMatch.Groups[1].Value.Replace(" ", "");
                    _logger.LogInformation("Número del documento extraído: {Numero}", documento.NumeroDocumento);
                }
                else
                {
                    // Fallback: buscar con espacios adicionales como en el texto OCR
                    var numeroDocumentoFallback = Regex.Match(texto, @"Nº\s+D\.R\s*\.\s*:\s*(\d{4}\s*-\s*\d+)");
                    if (numeroDocumentoFallback.Success)
                    {
                        documento.NumeroDocumento = numeroDocumentoFallback.Groups[1].Value.Replace(" ", "");
                        _logger.LogInformation("Número del documento extraído (fallback): {Numero}", documento.NumeroDocumento);
                    }
                    else
                    {
                        // Segundo fallback: buscar el patrón exacto del texto OCR
                        var numeroDocumentoFallback2 = Regex.Match(texto, @"Nº\s+D\.R\s*\.\s*:\s*(\d{4}-\s*\d+)");
                        if (numeroDocumentoFallback2.Success)
                        {
                            documento.NumeroDocumento = numeroDocumentoFallback2.Groups[1].Value.Replace(" ", "");
                            _logger.LogInformation("Número del documento extraído (fallback 2): {Numero}", documento.NumeroDocumento);
                        }
                    }
                }

                // 2. Situación del documento (ej: NORMAL)
                var situacionMatch = Regex.Match(texto, @"(?:Situación|Situacion)\s+D\.?R\.?\s*\.?\s*:?\s*([A-Z]+)");
                if (situacionMatch.Success)
                {
                    documento.SituacionDocumento = situacionMatch.Groups[1].Value;
                    _logger.LogInformation("Situación del documento extraída: {Situacion}", documento.SituacionDocumento);
                }
                else
                {
                    // Fallback: buscar con espacios adicionales como en el texto OCR
                    var situacionFallback = Regex.Match(texto, @"Situación\s+D\.R\s*\.\s*:\s*([A-Z]+)");
                    if (situacionFallback.Success)
                    {
                        documento.SituacionDocumento = situacionFallback.Groups[1].Value;
                        _logger.LogInformation("Situación del documento extraída (fallback): {Situacion}", documento.SituacionDocumento);
                    }
                    else
                    {
                        // Segundo fallback: buscar el patrón exacto del texto OCR
                        var situacionFallback2 = Regex.Match(texto, @"Situación\s+D\.R\s*\.\s*:\s*([A-Z]+)");
                        if (situacionFallback2.Success)
                        {
                            documento.SituacionDocumento = situacionFallback2.Groups[1].Value;
                            _logger.LogInformation("Situación del documento extraída (fallback 2): {Situacion}", documento.SituacionDocumento);
                        }
                    }
                }

                // 3. Número de manifiesto (ej: 257809)
                var manifiestoMatch = Regex.Match(texto, @"(?:Manifiesto|Mnfto)\s*:?\s*(\d+)");
                if (manifiestoMatch.Success)
                {
                    documento.NumeroManifiesto = manifiestoMatch.Groups[1].Value;
                    _logger.LogInformation("Número de manifiesto extraído: {Manifiesto}", documento.NumeroManifiesto);
                }

                // 4. Fecha del manifiesto SNA (ej: 19/06/2025)
                var fechaManifiestoMatch = Regex.Match(texto, @"(?:Fch\.?|Fecha)\s*(?:Mnfto|Manifiesto)\s+SNA\s*:?\s*(\d{2}/\d{2}/\d{4})");
                if (fechaManifiestoMatch.Success)
                {
                    var fechaStr = fechaManifiestoMatch.Groups[1].Value;
                    if (DateTime.TryParseExact(fechaStr, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out var fecha))
                    {
                        documento.FechaManifiestoSna = fecha;
                        _logger.LogInformation("Fecha del manifiesto SNA extraída: {Fecha}", fecha.ToString("dd/MM/yyyy"));
                    }
                }

                // 5. Fecha de inicio de almacenaje (ej: 25/06/2025)
                var fechaInicioMatch = Regex.Match(texto, @"(?:Fch\.?|Fecha)\s*Inicio\s+(?:Alm|Almacenaje)\s*\.?\s*:?\s*(\d{2}/\d{2}/\d{4})");
                if (fechaInicioMatch.Success)
                {
                    var fechaStr = fechaInicioMatch.Groups[1].Value;
                    if (DateTime.TryParseExact(fechaStr, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out var fecha))
                    {
                        documento.FechaInicioAlmacenaje = fecha;
                        _logger.LogInformation("Fecha de inicio de almacenaje extraída: {Fecha}", fecha.ToString("dd/MM/yyyy"));
                    }
                }

                // 6. Tipo de documento (ej: CONTENEDOR IMPORTACION - POR MANIFIESTO - INDIRECTO)
                var tipoDocumentoMatch = Regex.Match(texto, @"Tipo\s+D\.?R\.?\s*\.?\s*:?\s*([A-Z\s\-]+?)(?=\r\n|\n|$)");
                if (tipoDocumentoMatch.Success)
                {
                    documento.TipoDocumento = tipoDocumentoMatch.Groups[1].Value.Trim();
                    _logger.LogInformation("Tipo de documento extraído: {Tipo}", documento.TipoDocumento);
                }
                else
                {
                    // Fallback: buscar con espacios adicionales como en el texto OCR
                    var tipoDocumentoFallback = Regex.Match(texto, @"Tipo\s+D\.R\s*\.\s*:\s*([A-Z\s\-]+?)(?=\r\n|\n|$)");
                    if (tipoDocumentoFallback.Success)
                    {
                        documento.TipoDocumento = tipoDocumentoFallback.Groups[1].Value.Trim();
                        _logger.LogInformation("Tipo de documento extraído (fallback): {Tipo}", documento.TipoDocumento);
                    }
                    else
                    {
                        // Segundo fallback: buscar el patrón exacto del texto OCR
                        var tipoDocumentoFallback2 = Regex.Match(texto, @"Tipo\s+D\.R\s*\.\s*:\s*([A-Z\s\-]+?)(?=\r\n|\n|$)");
                        if (tipoDocumentoFallback2.Success)
                        {
                            documento.TipoDocumento = tipoDocumentoFallback2.Groups[1].Value.Trim();
                            _logger.LogInformation("Tipo de documento extraído (fallback 2): {Tipo}", documento.TipoDocumento);
                        }
                    }
                }

                // 7. BL Armador (ej: (M)BAC0549074/ (H)DACA78565)
                var blArmadorMatch = Regex.Match(texto, @"BL\s+(?:Armador|Arm)\s*:?\s*([A-Z0-9\(\)\/\s]+?)(?=\r\n|\n|$)");
                if (blArmadorMatch.Success)
                {
                    documento.BlArmador = blArmadorMatch.Groups[1].Value.Trim();
                    _logger.LogInformation("BL Armador extraído: {BL}", documento.BlArmador);
                }
                else
                {
                    // Fallback: buscar solo "BL Armador:"
                    var blArmadorFallback = Regex.Match(texto, @"BL\s+Armador\s*:?\s*([A-Z0-9\(\)\/\s]+?)(?=\r\n|\n|$)");
                    if (blArmadorFallback.Success)
                    {
                        documento.BlArmador = blArmadorFallback.Groups[1].Value.Trim();
                        _logger.LogInformation("BL Armador extraído (fallback): {BL}", documento.BlArmador);
                    }
                }

                // 8. Consignatario (ej: (86963200-7) FORUS S A)
                var consignatarioMatch = Regex.Match(texto, @"Consignatario\s*:?\s*\((\d{8}-\d)\)\s*([A-Z\s]+?)(?=\r\n|\n|$)");
                if (consignatarioMatch.Success)
                {
                    documento.RutConsignatario = consignatarioMatch.Groups[1].Value;
                    documento.Consignatario = consignatarioMatch.Groups[2].Value.Trim();
                    _logger.LogInformation("Consignatario extraído: {Consignatario} (RUT: {Rut})", documento.Consignatario, documento.RutConsignatario);
                }
                else
                {
                    // Fallback: buscar solo el RUT del consignatario
                    var rutConsignatarioFallback = Regex.Match(texto, @"Consignatario\s*:?\s*\((\d{8}-\d)\)");
                    if (rutConsignatarioFallback.Success)
                    {
                        documento.RutConsignatario = rutConsignatarioFallback.Groups[1].Value;
                        _logger.LogInformation("RUT del consignatario extraído (fallback): {Rut}", documento.RutConsignatario);
                    }
                }

                // 9. Dirección del consignatario (ej: AV LAS CONDES NRO 11281, BLOCK C - SANTIAGO)
                var direccionMatch = Regex.Match(texto, @"(?:Dirección|Direccion)\s*:?\s*([A-Z0-9\s\-,]+?)(?=\r\n|\n|$)");
                if (direccionMatch.Success)
                {
                    documento.DireccionConsignatario = direccionMatch.Groups[1].Value.Trim();
                    _logger.LogInformation("Dirección del consignatario extraída: {Direccion}", documento.DireccionConsignatario);
                }
                else
                {
                    // Fallback: buscar después de "Dirección:"
                    var direccionFallback = Regex.Match(texto, @"Dirección\s*:?\s*([A-Z0-9\s\-,]+?)(?=\r\n|\n|$)");
                    if (direccionFallback.Success)
                    {
                        documento.DireccionConsignatario = direccionFallback.Groups[1].Value.Trim();
                        _logger.LogInformation("Dirección del consignatario extraída (fallback): {Direccion}", documento.DireccionConsignatario);
                    }
                }

                // 10. Línea operadora (ej: CMA-CGM CHILE S.A)
                var lineaMatch = Regex.Match(texto, @"(?:Linea|Línea)\s+Operadora\s*:?\s*([A-Z\s\-\.]+?)(?=\r\n|\n|$)");
                if (lineaMatch.Success)
                {
                    documento.LineaOperadora = lineaMatch.Groups[1].Value.Trim();
                    _logger.LogInformation("Línea operadora extraída: {Linea}", documento.LineaOperadora);
                }
                else
                {
                    // Fallback: buscar solo "Linea Operadora:"
                    var lineaFallback = Regex.Match(texto, @"Linea\s+Operadora\s*:?\s*([A-Z\s\-\.]+?)(?=\r\n|\n|$)");
                    if (lineaFallback.Success)
                    {
                        documento.LineaOperadora = lineaFallback.Groups[1].Value.Trim();
                        _logger.LogInformation("Línea operadora extraída (fallback): {Linea}", documento.LineaOperadora);
                    }
                }

                // 11. Puerto de origen (ej: CHITTAGONG)
                var puertoOrigenMatch = Regex.Match(texto, @"(?:Pto\.?|Puerto)\s*Origen\s*:?\s*([A-Z]+)");
                if (puertoOrigenMatch.Success)
                {
                    documento.PuertoOrigen = puertoOrigenMatch.Groups[1].Value;
                    _logger.LogInformation("Puerto de origen extraído: {Puerto}", documento.PuertoOrigen);
                }

                // 12. Puerto de embarque (ej: CHITTAGONG)
                var puertoEmbarqueMatch = Regex.Match(texto, @"(?:Pto\.?|Puerto)\s*Embarque\s*:?\s*([A-Z]+)");
                if (puertoEmbarqueMatch.Success)
                {
                    documento.PuertoEmbarque = puertoEmbarqueMatch.Groups[1].Value;
                    _logger.LogInformation("Puerto de embarque extraído: {Puerto}", documento.PuertoEmbarque);
                }

                // 13. Puerto de descarga (ej: SAN ANTONIO)
                var puertoDescargaMatch = Regex.Match(texto, @"(?:Pto\.?|Puerto)\s*Descarga\s*:?\s*([A-Z\s]+?)(?=\r\n|\n|$)");
                if (puertoDescargaMatch.Success)
                {
                    documento.PuertoDescarga = puertoDescargaMatch.Groups[1].Value.Trim();
                    _logger.LogInformation("Puerto de descarga extraído: {Puerto}", documento.PuertoDescarga);
                }
                else
                {
                    // Fallback: buscar solo "Pto.Descarga:"
                    var puertoDescargaFallback = Regex.Match(texto, @"Pto\.Descarga\s*:?\s*([A-Z\s]+?)(?=\r\n|\n|$)");
                    if (puertoDescargaFallback.Success)
                    {
                        documento.PuertoDescarga = puertoDescargaFallback.Groups[1].Value.Trim();
                        _logger.LogInformation("Puerto de descarga extraído (fallback): {Puerto}", documento.PuertoDescarga);
                    }
                }

                // 14. Nave/Viaje (ej: CMA CGM BEIRA / OLISSN1)
                var naveViajeMatch = Regex.Match(texto, @"(?:Nave/Viaje|Nave|Viaje)\s*:?\s*([A-Z\s]+/\s*[A-Z0-9]+?)(?=\r\n|\n|$)");
                if (naveViajeMatch.Success)
                {
                    documento.NaveViaje = naveViajeMatch.Groups[1].Value.Trim();
                    _logger.LogInformation("Nave/Viaje extraído: {NaveViaje}", documento.NaveViaje);
                }
                else
                {
                    // Fallback: buscar solo "Nave/Viaje:"
                    var naveViajeFallback = Regex.Match(texto, @"Nave/Viaje\s*:?\s*([A-Z\s]+/\s*[A-Z0-9]+?)(?=\r\n|\n|$)");
                    if (naveViajeFallback.Success)
                    {
                        documento.NaveViaje = naveViajeFallback.Groups[1].Value.Trim();
                        _logger.LogInformation("Nave/Viaje extraído (fallback): {NaveViaje}", documento.NaveViaje);
                    }
                }

                // 15. Campos específicos del texto OCR que no se están capturando
                // Consignatario específico del texto OCR
                var consignatarioEspecificoMatch = Regex.Match(texto, @"CONSIGNATA\s*RIO\s*:\s*([A-Z\s]+)");
                if (consignatarioEspecificoMatch.Success && string.IsNullOrEmpty(documento.Consignatario))
                {
                    documento.Consignatario = consignatarioEspecificoMatch.Groups[1].Value.Trim();
                    _logger.LogInformation("Consignatario extraído (específico): {Consignatario}", documento.Consignatario);
                }

                // Contenedor específico del texto OCR
                var contenedorEspecificoMatch = Regex.Match(texto, @"Contenedor\s*:\s*([A-Z0-9\s\-]+?)(?=\r\n|\n|$)");
                if (contenedorEspecificoMatch.Success)
                {
                    documento.Contenedor = contenedorEspecificoMatch.Groups[1].Value.Trim();
                    _logger.LogInformation("Contenedor extraído (específico): {Contenedor}", documento.Contenedor);
                }

                // TATC específico del texto OCR
                var tatcEspecificoMatch = Regex.Match(texto, @"TATC\s*:\s*(\d+)");
                if (tatcEspecificoMatch.Success)
                {
                    documento.Tatc = tatcEspecificoMatch.Groups[1].Value;
                    _logger.LogInformation("TATC extraído (específico): {TATC}", documento.Tatc);
                }

                // Cantidad específica del texto OCR
                var cantidadEspecificaMatch = Regex.Match(texto, @"Cantidad\s*:\s*(\d+)");
                if (cantidadEspecificaMatch.Success)
                {
                    documento.Cantidad = cantidadEspecificaMatch.Groups[1].Value;
                    _logger.LogInformation("Cantidad extraída (específica): {Cantidad}", documento.Cantidad);
                }

                // Peso específico del texto OCR
                var pesoEspecificoMatch = Regex.Match(texto, @"Peso\s*:\s*([\d\.,]+)");
                if (pesoEspecificoMatch.Success)
                {
                    documento.Peso = pesoEspecificoMatch.Groups[1].Value;
                    _logger.LogInformation("Peso extraído (específico): {Peso}", documento.Peso);
                }

                // Volumen específico del texto OCR
                var volumenEspecificoMatch = Regex.Match(texto, @"Volumen\s*:\s*([\d\.,]+)");
                if (volumenEspecificoMatch.Success)
                {
                    documento.Volumen = volumenEspecificoMatch.Groups[1].Value;
                    _logger.LogInformation("Volumen extraído (específico): {Volumen}", documento.Volumen);
                }

                // Estado específico del texto OCR
                var estadoEspecificoMatch = Regex.Match(texto, @"Estado\s*:\s*([A-Z]+)");
                if (estadoEspecificoMatch.Success)
                {
                    documento.Estado = estadoEspecificoMatch.Groups[1].Value;
                    _logger.LogInformation("Estado extraído (específico): {Estado}", documento.Estado);
                }

                // Ubicación específica del texto OCR
                var ubicacionEspecificaMatch = Regex.Match(texto, @"Ubicación\s*:\s*(\d+)");
                if (ubicacionEspecificaMatch.Success)
                {
                    documento.Ubicacion = ubicacionEspecificaMatch.Groups[1].Value;
                    _logger.LogInformation("Ubicación extraída (específica): {Ubicacion}", documento.Ubicacion);
                }

                // 15. Almacén (ej: PATIO ZONA PRIMARIA)
                var almacenMatch = Regex.Match(texto, @"(?:Almacén|Almacen)\s*:?\s*([A-Z\s]+?)(?=\r\n|\n|$)");
                if (almacenMatch.Success)
                {
                    documento.Almacen = almacenMatch.Groups[1].Value.Trim();
                    _logger.LogInformation("Almacén extraído: {Almacen}", documento.Almacen);
                }
                else
                {
                    // Fallback: buscar solo "Almacén:"
                    var almacenFallback = Regex.Match(texto, @"Almacén\s*:?\s*([A-Z\s]+?)(?=\r\n|\n|$)");
                    if (almacenFallback.Success)
                    {
                        documento.Almacen = almacenFallback.Groups[1].Value.Trim();
                        _logger.LogInformation("Almacén extraído (fallback): {Almacen}", documento.Almacen);
                    }
                }

                // 16. Puerto de transbordo (ej: CALLAO)
                var puertoTransbordoMatch = Regex.Match(texto, @"(?:Pto\.?|Puerto)\s*Transbordo\s*:?\s*([A-Z]+)");
                if (puertoTransbordoMatch.Success)
                {
                    documento.PuertoTransbordo = puertoTransbordoMatch.Groups[1].Value;
                    _logger.LogInformation("Puerto de transbordo extraído: {Puerto}", documento.PuertoTransbordo);
                }

                // 17. Destino de carga (ej: IMPORTACION)
                var destinoCargaMatch = Regex.Match(texto, @"Destino\s+Carga\s*:?\s*([A-Z]+)");
                if (destinoCargaMatch.Success)
                {
                    documento.DestinoCarga = destinoCargaMatch.Groups[1].Value;
                    _logger.LogInformation("Destino de carga extraído: {Destino}", documento.DestinoCarga);
                }

                // 18. Zona (ej: PRIMARIA)
                var zonaMatch = Regex.Match(texto, @"Zona\s*:?\s*([A-Z]+)");
                if (zonaMatch.Success)
                {
                    documento.Zona = zonaMatch.Groups[1].Value;
                    _logger.LogInformation("Zona extraída: {Zona}", documento.Zona);
                }

                // 19. Servicio de almacenaje (ej: ALMACENAJE DE CONTENEDOR 40' NORMAL (ZP))
                var servicioAlmacenajeMatch = Regex.Match(texto, @"(?:Srv\.?|Servicio)\s*Almacenaje\s*:?\s*([A-Z0-9\s\-\'\(\)]+?)(?=\r\n|\n|$)");
                if (servicioAlmacenajeMatch.Success)
                {
                    documento.ServicioAlmacenaje = servicioAlmacenajeMatch.Groups[1].Value.Trim();
                    _logger.LogInformation("Servicio de almacenaje extraído: {Servicio}", documento.ServicioAlmacenaje);
                }

                // 20. Agencia de aduana
                var agenciaAduanaMatch = Regex.Match(texto, @"Agencia\s+de\s+Aduana\s*:?\s*([A-Z\s]+?)(?=\r\n|\n|$)");
                if (agenciaAduanaMatch.Success)
                {
                    documento.AgenciaAduana = agenciaAduanaMatch.Groups[1].Value.Trim();
                    _logger.LogInformation("Agencia de aduana extraída: {Agencia}", documento.AgenciaAduana);
                }
                else
                {
                    // Fallback: buscar solo "Agencia de Aduana:"
                    var agenciaAduanaFallback = Regex.Match(texto, @"Agencia\s+de\s+Aduana\s*:?\s*([A-Z\s]+?)(?=\r\n|\n|$)");
                    if (agenciaAduanaFallback.Success)
                    {
                        documento.AgenciaAduana = agenciaAduanaFallback.Groups[1].Value.Trim();
                        _logger.LogInformation("Agencia de aduana extraída (fallback): {Agencia}", documento.AgenciaAduana);
                    }
                }

                // 21. Guarda almacén (ej: MELLA JUAN (13196396-3))
                var guardaAlmacenMatch = Regex.Match(texto, @"(?:Guarda\s+)?Almacén\s*:?\s*([A-Z\s]+\([0-9\-]+\)?)(?=\r\n|\n|$)");
                if (guardaAlmacenMatch.Success)
                {
                    documento.GuardaAlmacen = guardaAlmacenMatch.Groups[1].Value.Trim();
                    _logger.LogInformation("Guarda almacén extraído: {Guarda}", documento.GuardaAlmacen);
                }
                else
                {
                    // Fallback: buscar solo "Guarda Almacén:"
                    var guardaAlmacenFallback = Regex.Match(texto, @"Guarda\s+Almacén\s*:?\s*([A-Z\s]+\([0-9\-]+\)?)(?=\r\n|\n|$)");
                    if (guardaAlmacenFallback.Success)
                    {
                        documento.GuardaAlmacen = guardaAlmacenFallback.Groups[1].Value.Trim();
                        _logger.LogInformation("Guarda almacén extraído (fallback): {Guarda}", documento.GuardaAlmacen);
                    }
                }

                // 22. Fechas de inicio y término de 90 días
                var inicio90DiasMatch = Regex.Match(texto, @"Inicio\s+90\s+(?:Dias|Días)\s*:?\s*(\d{2}/\d{2}/\d{4})");
                if (inicio90DiasMatch.Success)
                {
                    var fechaStr = inicio90DiasMatch.Groups[1].Value;
                    if (DateTime.TryParseExact(fechaStr, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out var fecha))
                    {
                        documento.FechaInicio90Dias = fecha;
                        _logger.LogInformation("Fecha de inicio 90 días extraída: {Fecha}", fecha.ToString("dd/MM/yyyy"));
                    }
                }

                var termino90DiasMatch = Regex.Match(texto, @"(?:Término|Termino)\s+90\s+(?:Días|Dias)\s*:?\s*(\d{2}/\d{2}/\d{4})");
                if (termino90DiasMatch.Success)
                {
                    var fechaStr = termino90DiasMatch.Groups[1].Value;
                    if (DateTime.TryParseExact(fechaStr, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out var fecha))
                    {
                        documento.FechaTermino90Dias = fecha;
                        _logger.LogInformation("Fecha de término 90 días extraída: {Fecha}", fecha.ToString("dd/MM/yyyy"));
                    }
                }

                // 23. Emitido por (ej: 12452809-7 el 25-06-2025 15:10:48 (WEB))
                var emitidoPorMatch = Regex.Match(texto, @"Emitido\s+por\s*:?\s*(\d{8}-\d)\s+el\s+(\d{2}-\d{2}-\d{4}\s+\d{2}:\d{2}:\d{2})\s+\(([A-Z]+)\)");
                if (emitidoPorMatch.Success)
                {
                    documento.RutEmisor = emitidoPorMatch.Groups[1].Value;
                    documento.FechaEmision = emitidoPorMatch.Groups[2].Value;
                    documento.MedioEmision = emitidoPorMatch.Groups[3].Value;
                    _logger.LogInformation("Emitido por extraído: RUT {Rut}, Fecha {Fecha}, Medio {Medio}", 
                        documento.RutEmisor, documento.FechaEmision, documento.MedioEmision);
                }

                // 24. Forwarder (puede estar vacío)
                var forwarderMatch = Regex.Match(texto, @"Forwarder\s*:?\s*([A-Z\s]+?)(?=\r\n|\n|$)");
                if (forwarderMatch.Success)
                {
                    documento.Forwarder = forwarderMatch.Groups[1].Value.Trim();
                    _logger.LogInformation("Forwarder extraído: {Forwarder}", documento.Forwarder);
                }

                // 25. Campos adicionales específicos del texto OCR
                // Contenedor
                var contenedorMatch = Regex.Match(texto, @"Contenedor\s*:?\s*([A-Z0-9\s\-]+?)(?=\r\n|\n|$)");
                if (contenedorMatch.Success)
                {
                    documento.Contenedor = contenedorMatch.Groups[1].Value.Trim();
                    _logger.LogInformation("Contenedor extraído: {Contenedor}", documento.Contenedor);
                }

                // TATC
                var tatcMatch = Regex.Match(texto, @"TATC\s*:?\s*(\d+)");
                if (tatcMatch.Success)
                {
                    documento.Tatc = tatcMatch.Groups[1].Value;
                    _logger.LogInformation("TATC extraído: {TATC}", documento.Tatc);
                }

                // Cantidad
                var cantidadMatch = Regex.Match(texto, @"Cantidad\s*:?\s*(\d+)");
                if (cantidadMatch.Success)
                {
                    documento.Cantidad = cantidadMatch.Groups[1].Value;
                    _logger.LogInformation("Cantidad extraída: {Cantidad}", documento.Cantidad);
                }

                // Peso
                var pesoMatch = Regex.Match(texto, @"Peso\s*:?\s*([\d\.,]+)");
                if (pesoMatch.Success)
                {
                    documento.Peso = pesoMatch.Groups[1].Value;
                    _logger.LogInformation("Peso extraído: {Peso}", documento.Peso);
                }

                // Volumen
                var volumenMatch = Regex.Match(texto, @"Volumen\s*:?\s*([\d\.,]+)");
                if (volumenMatch.Success)
                {
                    documento.Volumen = volumenMatch.Groups[1].Value;
                    _logger.LogInformation("Volumen extraído: {Volumen}", documento.Volumen);
                }

                // Estado
                var estadoMatch = Regex.Match(texto, @"Estado\s*:?\s*([A-Z]+)");
                if (estadoMatch.Success)
                {
                    documento.Estado = estadoMatch.Groups[1].Value;
                    _logger.LogInformation("Estado extraído: {Estado}", documento.Estado);
                }

                // Ubicación
                var ubicacionMatch = Regex.Match(texto, @"Ubicación\s*:?\s*(\d+)");
                if (ubicacionMatch.Success)
                {
                    documento.Ubicacion = ubicacionMatch.Groups[1].Value;
                    _logger.LogInformation("Ubicación extraída: {Ubicacion}", documento.Ubicacion);
                }

                // Tipo Bulto
                var tipoBultoMatch = Regex.Match(texto, @"Tipo\s+Bulto\s*:?\s*([A-Z0-9\s\-]+?)(?=\r\n|\n|$)");
                if (tipoBultoMatch.Success)
                {
                    documento.TipoBulto = tipoBultoMatch.Groups[1].Value.Trim();
                    _logger.LogInformation("Tipo Bulto extraído: {TipoBulto}", documento.TipoBulto);
                }

                // Validar que se extrajeron los campos críticos
                documento.EsValido = !string.IsNullOrEmpty(documento.NumeroDocumento) && 
                                   !string.IsNullOrEmpty(documento.SituacionDocumento) && 
                                   !string.IsNullOrEmpty(documento.NumeroManifiesto);

                _logger.LogInformation("Extracción de campos críticos completada. Documento válido: {EsValido}", documento.EsValido);
            });
        }

        /// <summary>
        /// Extrae campos adicionales del documento
        /// </summary>
        private async Task ExtraerCamposAdicionalesAsync(DocumentoRecepcion documento, string texto)
        {
            await Task.Run(() =>
            {
                // Fechas de 90 días
                var fechas90Match = Regex.Match(texto, @"Inicio\s+90\s+Días\s*[:\s]*(\d{2}/\d{2}/\d{4})");
                if (fechas90Match.Success)
                {
                    var fechaStr = fechas90Match.Groups[1].Value;
                    if (DateTime.TryParseExact(fechaStr, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out var fecha))
                    {
                        documento.FechaInicio90Dias = fecha;
                    }
                }

                var termino90Match = Regex.Match(texto, @"Término\s+90\s+Días\s*[:\s]*(\d{2}/\d{2}/\d{4})");
                if (termino90Match.Success)
                {
                    var fechaStr = termino90Match.Groups[1].Value;
                    if (DateTime.TryParseExact(fechaStr, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out var fecha))
                    {
                        documento.FechaTermino90Dias = fecha;
                    }
                }

                // Servicio de almacenaje
                var servicioMatch = Regex.Match(texto, @"Srv\.\s+Almacenaje\s*[:\s]*([A-Z0-9\s\(\)]+)");
                if (servicioMatch.Success)
                {
                    documento.ServicioAlmacenaje = servicioMatch.Groups[1].Value.Trim();
                    _logger.LogInformation("Servicio de almacenaje extraído: {Servicio}", documento.ServicioAlmacenaje);
                }
                else
                {
                    // Fallback: buscar con diferentes patrones
                    var servicioFallback = Regex.Match(texto, @"(?:Srv\.?|Servicio)\s+Almacenaje\s*[:\s]*([A-Z0-9\s\(\)]+)");
                    if (servicioFallback.Success)
                    {
                        documento.ServicioAlmacenaje = servicioFallback.Groups[1].Value.Trim();
                        _logger.LogInformation("Servicio de almacenaje extraído (fallback): {Servicio}", documento.ServicioAlmacenaje);
                    }
                }

                // Guarda almacén
                var guardaMatch = Regex.Match(texto, @"Guarda\s+Almacén\s*[:\s]*([A-Z\s]+)\s*\((\d{8}-\d)\)");
                if (guardaMatch.Success)
                {
                    documento.GuardaAlmacen = guardaMatch.Groups[1].Value.Trim();
                    documento.RutGuardaAlmacen = guardaMatch.Groups[2].Value;
                    _logger.LogInformation("Guarda almacén extraído: {Guarda} (RUT: {Rut})", documento.GuardaAlmacen, documento.RutGuardaAlmacen);
                }
                else
                {
                    // Fallback: buscar solo el nombre del guarda almacén
                    var guardaFallback = Regex.Match(texto, @"Guarda\s+Almacén\s*[:\s]*([A-Z\s]+)");
                    if (guardaFallback.Success)
                    {
                        documento.GuardaAlmacen = guardaFallback.Groups[1].Value.Trim();
                        _logger.LogInformation("Guarda almacén extraído (fallback): {Guarda}", documento.GuardaAlmacen);
                    }
                }

                // Puerto de destino
                var puertoDestinoMatch = Regex.Match(texto, @"Pto\.\s+Destino\s*[:\s]*([A-Z\s]+)");
                if (puertoDestinoMatch.Success)
                {
                    documento.PuertoDestino = puertoDestinoMatch.Groups[1].Value.Trim();
                }

                // Destino de carga
                var destinoCargaMatch = Regex.Match(texto, @"Destino\s+Carga\s*[:\s]*([A-Z]+)");
                if (destinoCargaMatch.Success)
                {
                    documento.DestinoCarga = destinoCargaMatch.Groups[1].Value;
                }

                // Zona
                var zonaMatch = Regex.Match(texto, @"Zona\s*[:\s]*([A-Z]+)");
                if (zonaMatch.Success)
                {
                    documento.Zona = zonaMatch.Groups[1].Value;
                }

                // Origen
                var origenMatch = Regex.Match(texto, @"Origen\s*[:\s]*([A-Z]+)");
                if (origenMatch.Success)
                {
                    documento.Origen = origenMatch.Groups[1].Value;
                }

                // Ubicación (ej: 10010101)
                var ubicacionMatch = Regex.Match(texto, @"Ubicación\s*[:\s]*(\d+)");
                if (ubicacionMatch.Success)
                {
                    documento.Ubicacion = ubicacionMatch.Groups[1].Value;
                }

                // Marcas (ej: CONSIGNATARIO: JIN & YIN & WANG LTDA N M (GCI #59111-hum1))
                var marcasMatch = Regex.Match(texto, @"Marcas\s*[:\s]*([A-Z0-9\s&\(\)#\-\.]+)");
                if (marcasMatch.Success)
                {
                    documento.Marcas = marcasMatch.Groups[1].Value.Trim();
                    _logger.LogInformation("Marcas extraídas: {Marcas}", documento.Marcas);
                }
                else
                {
                    // Fallback: buscar marcas en formato específico del OCR
                    var marcasFallback = Regex.Match(texto, @"\(H40\)\s*40");
                    if (marcasFallback.Success)
                    {
                        documento.Marcas = marcasFallback.Value.Trim();
                        _logger.LogInformation("Marcas extraídas (fallback): {Marcas}", documento.Marcas);
                    }
                }

                // Campos adicionales específicos del texto OCR
                // Contenedor
                var contenedorMatch = Regex.Match(texto, @"Contenedor\s*[:\s]*([A-Z0-9\s\-]+)");
                if (contenedorMatch.Success)
                {
                    documento.Contenedor = contenedorMatch.Groups[1].Value.Trim();
                    _logger.LogInformation("Contenedor extraído: {Contenedor}", documento.Contenedor);
                }

                // TATC
                var tatcMatch = Regex.Match(texto, @"TATC\s*[:\s]*(\d+)");
                if (tatcMatch.Success)
                {
                    documento.Tatc = tatcMatch.Groups[1].Value;
                    _logger.LogInformation("TATC extraído: {TATC}", documento.Tatc);
                }

                // Cantidad
                var cantidadMatch = Regex.Match(texto, @"Cantidad\s*[:\s]*(\d+)");
                if (cantidadMatch.Success)
                {
                    documento.Cantidad = cantidadMatch.Groups[1].Value;
                    _logger.LogInformation("Cantidad extraída: {Cantidad}", documento.Cantidad);
                }

                // Peso
                var pesoMatch = Regex.Match(texto, @"Peso\s*[:\s]*([\d\.,]+)");
                if (pesoMatch.Success)
                {
                    documento.Peso = pesoMatch.Groups[1].Value;
                    _logger.LogInformation("Peso extraído: {Peso}", documento.Peso);
                }

                // Volumen
                var volumenMatch = Regex.Match(texto, @"Volumen\s*[:\s]*([\d\.,]+)");
                if (volumenMatch.Success)
                {
                    documento.Volumen = volumenMatch.Groups[1].Value;
                    _logger.LogInformation("Volumen extraído: {Volumen}", documento.Volumen);
                }

                // Estado
                var estadoMatch = Regex.Match(texto, @"Estado\s*[:\s]*([A-Z]+)");
                if (estadoMatch.Success)
                {
                    documento.Estado = estadoMatch.Groups[1].Value;
                    _logger.LogInformation("Estado extraído: {Estado}", documento.Estado);
                }

                // Tipo Bulto
                var tipoBultoMatch = Regex.Match(texto, @"Tipo\s+Bulto\s*[:\s]*([A-Z0-9\s\-]+)");
                if (tipoBultoMatch.Success)
                {
                    documento.TipoBulto = tipoBultoMatch.Groups[1].Value.Trim();
                    _logger.LogInformation("Tipo Bulto extraído: {TipoBulto}", documento.TipoBulto);
                }
            });
        }

        /// <summary>
        /// Extrae datos usando métodos manuales desde archivo
        /// </summary>
        private async Task<DocumentoRecepcion> ExtraerManualAsync(string filePath)
        {
            using var stream = File.OpenRead(filePath);
            return await ExtraerManualAsync(stream);
        }

        /// <summary>
        /// Extrae datos usando métodos manuales desde stream
        /// </summary>
        private async Task<DocumentoRecepcion> ExtraerManualAsync(Stream fileStream)
        {
            var texto = await ExtraerTextoPngAsync(fileStream);
            var documento = new DocumentoRecepcion();

            await AplicarExtraccionManualAsync(documento, texto);

            // Guardar texto extraído
            documento.TextoExtraido = texto;
            documento.ConfianzaExtraccion = 0.7m; // Confianza media para extracción manual

            return documento;
        }

        /// <summary>
        /// Aplica extracción manual usando patrones y regex
        /// </summary>
        private async Task AplicarExtraccionManualAsync(DocumentoRecepcion documento, string texto)
        {
            // Normalizar el texto OCR
            var textoNormalizado = NormalizarTexto(texto);
            _logger.LogInformation("Texto normalizado: {Texto}", textoNormalizado);

            // Extraer campos críticos
            await ExtraerCamposCriticosAsync(documento, textoNormalizado);

            // Extraer campos adicionales
            await ExtraerCamposAdicionalesAsync(documento, textoNormalizado);

            // Guardar texto extraído
            documento.TextoExtraido = textoNormalizado;
            documento.ConfianzaExtraccion = 0.8m;

            if (!documento.EsValido)
            {
                documento.Comentarios = "No se pudieron extraer todos los campos requeridos del documento de recepción";
                _logger.LogWarning("Extracción incompleta: {Error}", documento.Comentarios);
            }
            else
            {
                _logger.LogInformation("Procesamiento completado exitosamente para DR: {NumeroDocumento}", documento.NumeroDocumento);
            }
        }

        /// <summary>
        /// Extrae texto de un archivo PNG usando Azure Vision
        /// </summary>
        private async Task<string> ExtraerTextoPngAsync(Stream fileStream)
        {
            try
            {
                // Convertir stream a Bitmap
                using var bitmap = new Bitmap(fileStream);
                
                // Usar Azure Vision para extraer texto
                return await ExtraerTextoConAzureVisionAsync(bitmap);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extrayendo texto de PNG");
                return string.Empty;
            }
        }

        /// <summary>
        /// Extrae texto usando Azure Computer Vision
        /// </summary>
        private async Task<string> ExtraerTextoConAzureVisionAsync(Bitmap image)
        {
            try
            {
                // Verificar configuración de Azure
                if (string.IsNullOrEmpty(_azureVisionKey) || string.IsNullOrEmpty(_azureVisionEndpoint))
                {
                    return "Azure Computer Vision no configurado. Configure AzureVision:Key y AzureVision:Endpoint en appsettings.json";
                }
                
                // Convertir Bitmap a bytes
                using var ms = new MemoryStream();
                image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                var imageBytes = ms.ToArray();
                
                // Configurar cliente de Azure Computer Vision
                var credential = new Azure.AzureKeyCredential(_azureVisionKey);
                var client = new ImageAnalysisClient(new Uri(_azureVisionEndpoint), credential);
                
                // Analizar imagen para extraer texto usando la API correcta
                var result = await client.AnalyzeAsync(
                    BinaryData.FromBytes(imageBytes),
                    VisualFeatures.Read
                );
                
                if (result.Value.Read != null && result.Value.Read.Blocks.Count > 0)
                {
                    var texto = new StringBuilder();
                    foreach (var block in result.Value.Read.Blocks)
                    {
                        foreach (var line in block.Lines)
                        {
                            foreach (var word in line.Words)
                            {
                                texto.Append(word.Text + " ");
                            }
                            texto.AppendLine();
                        }
                    }
                    return texto.ToString().Trim();
                }
                
                return "Azure Computer Vision no detectó texto en la imagen";
            }
            catch (Exception ex)
            {
                return $"Error en Azure Computer Vision: {ex.Message}";
            }
        }

        /// <summary>
        /// Normaliza el texto OCR
        /// </summary>
        private string NormalizarTexto(string texto)
        {
            return texto
                .Replace("\r\n", " ")
                .Replace("\n", " ")
                .Replace("  ", " ")
                .Trim();
        }

        /// <summary>
        /// Valida si el archivo es un PNG válido
        /// </summary>
        public async Task<bool> ValidarPngAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return false;

                using var stream = File.OpenRead(filePath);
                return await ValidarPngAsync(stream);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Valida si el stream es un PNG válido
        /// </summary>
        public async Task<bool> ValidarPngAsync(Stream fileStream)
        {
            try
            {
                var buffer = new byte[8];
                await fileStream.ReadAsync(buffer, 0, 8);
                fileStream.Position = 0;

                // Verificar firma PNG (89 50 4E 47 0D 0A 1A 0A)
                return buffer[0] == 0x89 && buffer[1] == 0x50 && buffer[2] == 0x4E && buffer[3] == 0x47 &&
                       buffer[4] == 0x0D && buffer[5] == 0x0A && buffer[6] == 0x1A && buffer[7] == 0x0A;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Calcula el hash SHA256 de un archivo
        /// </summary>
        private async Task<string> CalcularHashArchivoAsync(string filePath)
        {
            using var sha256 = SHA256.Create();
            using var stream = File.OpenRead(filePath);
            var hash = await sha256.ComputeHashAsync(stream);
            return Convert.ToHexString(hash).ToLower();
        }

        /// <summary>
        /// Calcula el hash SHA256 de un stream
        /// </summary>
        private async Task<string> CalcularHashArchivoAsync(Stream fileStream)
        {
            using var sha256 = SHA256.Create();
            var hash = await sha256.ComputeHashAsync(fileStream);
            fileStream.Position = 0; // Restaurar posición
            return Convert.ToHexString(hash).ToLower();
        }
    }
} 