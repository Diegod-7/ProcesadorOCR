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
    /// Servicio para procesar documentos de Guía de Despacho Electrónica
    /// </summary>
    public class GuiaDespachoService : IGuiaDespachoService
    {
        private readonly ILogger<GuiaDespachoService> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _azureVisionKey;
        private readonly string _azureVisionEndpoint;

        public GuiaDespachoService(ILogger<GuiaDespachoService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            
            // Configuración de Azure Computer Vision
            _azureVisionKey = configuration["AzureVision:Key"] ?? string.Empty;
            _azureVisionEndpoint = configuration["AzureVision:Endpoint"] ?? string.Empty;
        }

        /// <summary>
        /// Extrae datos de un archivo PNG de documento de Guía de Despacho Electrónica
        /// </summary>
        public async Task<GuiaDespacho> ExtraerDatosAsync(string filePath)
        {
            try
            {
                _logger.LogInformation("Iniciando extracción de datos de Guía de Despacho desde archivo: {FilePath}", filePath);

                // Validar archivo
                if (!await ValidarPngAsync(filePath))
                {
                    throw new ArgumentException("El archivo no es un PNG válido");
                }

                // Leer archivo
                using var stream = File.OpenRead(filePath);
                var fileName = Path.GetFileName(filePath);
                return await ExtraerDatosAsync(stream, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extrayendo datos de Guía de Despacho desde archivo: {FilePath}", filePath);
                throw;
            }
        }

        /// <summary>
        /// Extrae datos de un stream de archivo PNG
        /// </summary>
        public async Task<GuiaDespacho> ExtraerDatosAsync(Stream fileStream, string fileName)
        {
            try
            {
                _logger.LogInformation("Iniciando extracción de datos de Guía de Despacho desde stream: {FileName}", fileName);

                // Calcular hash del archivo
                var hash = await CalcularHashAsync(fileStream);
                fileStream.Position = 0;

                // Extraer texto usando Azure Vision
                var textoExtraido = await ExtraerTextoPngAsync(fileStream);
                _logger.LogInformation("Texto extraído de Guía de Despacho: {Texto}", textoExtraido?.Substring(0, Math.Min(100, textoExtraido?.Length ?? 0)));

                // Procesar el texto extraído
                var documento = await ProcesarTextoOcrAsync(textoExtraido);

                // Configurar metadatos
                documento.NombreArchivo = fileName;
                documento.HashArchivo = hash;
                documento.MetodoExtraccion = "Azure Computer Vision";

                _logger.LogInformation("Extracción completada para Guía de Despacho: {NumeroGuia}", documento.NumeroGuia);

                return documento;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extrayendo datos de Guía de Despacho desde stream: {FileName}", fileName);
                throw;
            }
        }

        /// <summary>
        /// Extrae datos de un array de bytes
        /// </summary>
        public async Task<GuiaDespacho> ExtraerDatosAsync(byte[] fileBytes, string fileName)
        {
            using var stream = new MemoryStream(fileBytes);
            return await ExtraerDatosAsync(stream, fileName);
        }

        /// <summary>
        /// Procesa texto OCR para extraer datos de documento de Guía de Despacho Electrónica
        /// </summary>
        public async Task<GuiaDespacho> ProcesarTextoOcrAsync(string textoOcr)
        {
            _logger.LogInformation("Iniciando procesamiento de texto OCR para documento de Guía de Despacho");

            var documento = new GuiaDespacho();

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

                // Validar documento
                documento.ValidarDocumento();

                if (!documento.EsValido)
                {
                    documento.Comentarios = "No se pudieron extraer todos los campos requeridos del documento de Guía de Despacho";
                    _logger.LogWarning("Extracción incompleta: {Error}", documento.Comentarios);
                }
                else
                {
                    _logger.LogInformation("Procesamiento completado exitosamente para Guía de Despacho: {NumeroGuia}", documento.NumeroGuia);
                }

                return documento;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando texto OCR para Guía de Despacho");
                documento.Comentarios = $"Error procesando texto: {ex.Message}";
                return documento;
            }
        }

        /// <summary>
        /// Extrae campos críticos del documento de Guía de Despacho
        /// </summary>
        private async Task ExtraerCamposCriticosAsync(GuiaDespacho documento, string texto)
        {
            _logger.LogInformation("Extrayendo campos críticos de Guía de Despacho");

            // Detectar formato del documento
            var esFormatoJorgeStein = texto.Contains("Jorge Stein") || texto.Contains("stein.cl");
            var esFormatoAlbertoRubio = texto.Contains("Alberto Rubio") || texto.Contains("agenciarubio.cl");
            
            _logger.LogInformation("Formato detectado - Jorge Stein: {JorgeStein}, Alberto Rubio: {AlbertoRubio}", 
                esFormatoJorgeStein, esFormatoAlbertoRubio);

            // Extraer número de guía (diferentes formatos)
            string numeroGuia = string.Empty;
            
            if (esFormatoJorgeStein)
            {
                // Formato Jorge Stein: No 0000659697
                var matchGuia = Regex.Match(texto, @"No\s+(\d+)", RegexOptions.IgnoreCase);
                if (matchGuia.Success)
                {
                    numeroGuia = matchGuia.Groups[1].Value;
                    _logger.LogInformation("Número de guía (Jorge Stein) extraído: {Guia}", numeroGuia);
                }
            }
            else if (esFormatoAlbertoRubio)
            {
                // Formato Alberto Rubio: N°11975 o Nº11975
                var matchGuia = Regex.Match(texto, @"N[°º]\s*(\d+)", RegexOptions.IgnoreCase);
                if (matchGuia.Success)
                {
                    numeroGuia = matchGuia.Groups[1].Value;
                    _logger.LogInformation("Número de guía (Alberto Rubio) extraído: {Guia}", numeroGuia);
                }
            }
            else
            {
                // Intentar ambos formatos como fallback
                var matchGuia = Regex.Match(texto, @"(?:No\s+|N[°º]\s*)(\d+)", RegexOptions.IgnoreCase);
                if (matchGuia.Success)
                {
                    numeroGuia = matchGuia.Groups[1].Value;
                    _logger.LogInformation("Número de guía (fallback) extraído: {Guia}", numeroGuia);
                }
            }

            if (!string.IsNullOrEmpty(numeroGuia))
            {
                documento.NumeroGuia = numeroGuia;
            }
            else
            {
                _logger.LogWarning("No se pudo extraer el número de guía");
            }

            // Extraer RUT del emisor (diferentes formatos)
            string rutEmisor = string.Empty;
            
            if (esFormatoJorgeStein)
            {
                // Formato Jorge Stein: R.U.T .: 89.218.600-6
                var matchRutEmisor = Regex.Match(texto, @"R\.U\.T\s*\.\s*:\s*(\d{1,2}\.\d{3}\.\d{3}-\d)", RegexOptions.IgnoreCase);
                if (matchRutEmisor.Success)
                {
                    rutEmisor = matchRutEmisor.Groups[1].Value;
                    _logger.LogInformation("RUT del emisor (Jorge Stein) extraído: {Rut}", rutEmisor);
                }
            }
            else if (esFormatoAlbertoRubio)
            {
                // Formato Alberto Rubio: R.U.T .: 10.399.142-0
                var matchRutEmisor = Regex.Match(texto, @"R\.U\.T\.\s*:\s*(\d{1,2}\.\d{3}\.\d{3}-\d)", RegexOptions.IgnoreCase);
                if (matchRutEmisor.Success)
                {
                    rutEmisor = matchRutEmisor.Groups[1].Value;
                    _logger.LogInformation("RUT del emisor (Alberto Rubio) extraído: {Rut}", rutEmisor);
                }
            }
            else
            {
                // Intentar ambos formatos como fallback
                var matchRutEmisor = Regex.Match(texto, @"R\.U\.T\s*\.?\s*:\s*(\d{1,2}\.\d{3}\.\d{3}-\d)", RegexOptions.IgnoreCase);
                if (matchRutEmisor.Success)
                {
                    rutEmisor = matchRutEmisor.Groups[1].Value;
                    _logger.LogInformation("RUT del emisor (fallback) extraído: {Rut}", rutEmisor);
                }
            }

            if (!string.IsNullOrEmpty(rutEmisor))
            {
                documento.RutEmisor = rutEmisor;
            }
            else
            {
                _logger.LogWarning("No se pudo extraer el RUT del emisor");
            }

            // Extraer fecha del documento (diferentes formatos)
            DateTime? fechaDocumento = null;
            
            if (esFormatoJorgeStein)
            {
                // Formato Jorge Stein: Fecha : 26 de Junio del 2025
                var matchFecha = Regex.Match(texto, @"Fecha\s*:\s*(\d{1,2})\s+de\s+([A-Za-z]+)\s+del\s+(\d{4})", RegexOptions.IgnoreCase);
                if (matchFecha.Success)
                {
                    var dia = matchFecha.Groups[1].Value;
                    var mes = matchFecha.Groups[2].Value;
                    var año = matchFecha.Groups[3].Value;
                    
                    // Convertir nombre del mes a número
                    var mesNumero = ConvertirMesATexto(mes);
                    if (mesNumero > 0)
                    {
                        var fechaStr = $"{dia.PadLeft(2, '0')}-{mesNumero.ToString().PadLeft(2, '0')}-{año}";
                        if (DateTime.TryParseExact(fechaStr, "dd-MM-yyyy", null, System.Globalization.DateTimeStyles.None, out var fecha))
                        {
                            fechaDocumento = fecha;
                            _logger.LogInformation("Fecha del documento (Jorge Stein) extraída: {Fecha}", fechaDocumento);
                        }
                    }
                }
            }
            else if (esFormatoAlbertoRubio)
            {
                // Formato Alberto Rubio: FECHA : 24-06-2025
                var matchFecha = Regex.Match(texto, @"FECHA\s*:\s*(\d{1,2}-\d{1,2}-\d{4})", RegexOptions.IgnoreCase);
                if (matchFecha.Success)
                {
                    var fechaStr = matchFecha.Groups[1].Value;
                    if (DateTime.TryParseExact(fechaStr, "dd-MM-yyyy", null, System.Globalization.DateTimeStyles.None, out var fecha))
                    {
                        fechaDocumento = fecha;
                        _logger.LogInformation("Fecha del documento (Alberto Rubio) extraída: {Fecha}", fechaDocumento);
                    }
                }
            }
            else
            {
                // Intentar ambos formatos como fallback
                var matchFecha = Regex.Match(texto, @"(?:FECHA|Fecha)\s*:\s*(\d{1,2}-\d{1,2}-\d{4})", RegexOptions.IgnoreCase);
                if (matchFecha.Success)
                {
                    var fechaStr = matchFecha.Groups[1].Value;
                    if (DateTime.TryParseExact(fechaStr, "dd-MM-yyyy", null, System.Globalization.DateTimeStyles.None, out var fecha))
                    {
                        fechaDocumento = fecha;
                        _logger.LogInformation("Fecha del documento (fallback) extraída: {Fecha}", fechaDocumento);
                    }
                }
            }

            if (fechaDocumento.HasValue)
            {
                documento.FechaDocumento = fechaDocumento;
            }
            else
            {
                _logger.LogWarning("No se pudo extraer la fecha del documento");
            }
        }

        /// <summary>
        /// Convierte el nombre del mes a número
        /// </summary>
        private int ConvertirMesATexto(string mes)
        {
            var meses = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                {"enero", 1}, {"febrero", 2}, {"marzo", 3}, {"abril", 4},
                {"mayo", 5}, {"junio", 6}, {"julio", 7}, {"agosto", 8},
                {"septiembre", 9}, {"octubre", 10}, {"noviembre", 11}, {"diciembre", 12}
            };

            return meses.TryGetValue(mes, out var numero) ? numero : 0;
        }

        /// <summary>
        /// Extrae campos adicionales del documento de Guía de Despacho
        /// </summary>
        private async Task ExtraerCamposAdicionalesAsync(GuiaDespacho documento, string texto)
        {
            _logger.LogInformation("Extrayendo campos adicionales de Guía de Despacho");

            // Detectar formato del documento
            var esFormatoJorgeStein = texto.Contains("Jorge Stein") || texto.Contains("stein.cl");
            var esFormatoAlbertoRubio = texto.Contains("Alberto Rubio") || texto.Contains("agenciarubio.cl");

            // Extraer nombre del emisor
            string nombreEmisor = string.Empty;
            if (esFormatoJorgeStein)
            {
                // Formato Jorge Stein: Agencia de Aduanas Jorge Stein y Cia. Ltda.
                var matchNombreEmisor = Regex.Match(texto, @"Agencia\s+de\s+Aduanas\s+([A-ZÁÉÍÓÚÑ\s]+?)\s+y\s+Cia\.", RegexOptions.IgnoreCase);
                if (matchNombreEmisor.Success)
                {
                    nombreEmisor = "Agencia de Aduanas " + matchNombreEmisor.Groups[1].Value.Trim() + " y Cía. Ltda.";
                    _logger.LogInformation("Nombre del emisor (Jorge Stein) extraído: {Nombre}", nombreEmisor);
                }
            }
            else if (esFormatoAlbertoRubio)
            {
                // Formato Alberto Rubio: Razón Social: Ramon Alberto Rubio Soto
                var matchNombreEmisor = Regex.Match(texto, @"Razón Social:\s*([A-ZÁÉÍÓÚÑ\s]+?)\s+Giro:", RegexOptions.IgnoreCase);
                if (matchNombreEmisor.Success)
                {
                    nombreEmisor = matchNombreEmisor.Groups[1].Value.Trim();
                    _logger.LogInformation("Nombre del emisor (Alberto Rubio) extraído: {Nombre}", nombreEmisor);
                }
            }

            if (!string.IsNullOrEmpty(nombreEmisor))
            {
                documento.NombreEmisor = nombreEmisor;
            }
            else
            {
                _logger.LogWarning("No se pudo extraer el nombre del emisor");
            }

            // Extraer giro del emisor
            string giroEmisor = string.Empty;
            if (esFormatoJorgeStein)
            {
                // Formato Jorge Stein: Giro: Agencia de Aduanas
                var matchGiroEmisor = Regex.Match(texto, @"Giro:\s*([A-ZÁÉÍÓÚÑ\s]+?)\s+Casa\s+Matriz:", RegexOptions.IgnoreCase);
                if (matchGiroEmisor.Success)
                {
                    giroEmisor = matchGiroEmisor.Groups[1].Value.Trim();
                    _logger.LogInformation("Giro del emisor (Jorge Stein) extraído: {Giro}", giroEmisor);
                }
            }
            else if (esFormatoAlbertoRubio)
            {
                // Formato Alberto Rubio: Giro: agencia de Aduanas
                var matchGiroEmisor = Regex.Match(texto, @"Giro:\s*([A-ZÁÉÍÓÚÑ\s]+?)\s+Dirección:", RegexOptions.IgnoreCase);
                if (matchGiroEmisor.Success)
                {
                    giroEmisor = matchGiroEmisor.Groups[1].Value.Trim();
                    _logger.LogInformation("Giro del emisor (Alberto Rubio) extraído: {Giro}", giroEmisor);
                }
            }

            if (!string.IsNullOrEmpty(giroEmisor))
            {
                documento.GiroEmisor = giroEmisor;
            }

            // Extraer dirección del emisor
            string direccionEmisor = string.Empty;
            if (esFormatoJorgeStein)
            {
                // Formato Jorge Stein: Casa Matriz: Av. Claudio Arrau 9452 | Piso 3' | Pudahuel | Santiago
                var matchDireccionEmisor = Regex.Match(texto, @"Casa\s+Matriz:\s*([A-ZÁÉÍÓÚÑ\s\d\-\.\|']+?)\s+SGS", RegexOptions.IgnoreCase);
                if (matchDireccionEmisor.Success)
                {
                    direccionEmisor = matchDireccionEmisor.Groups[1].Value.Trim();
                    _logger.LogInformation("Dirección del emisor (Jorge Stein) extraída: {Direccion}", direccionEmisor);
                }
            }
            else if (esFormatoAlbertoRubio)
            {
                // Formato Alberto Rubio: Dirección: Padre Mariano 210 Of 302 Providencia, Santiago
                var matchDireccionEmisor = Regex.Match(texto, @"Dirección:\s*([A-ZÁÉÍÓÚÑ\s\d\-\.]+?)\s+Teléfono:", RegexOptions.IgnoreCase);
                if (matchDireccionEmisor.Success)
                {
                    direccionEmisor = matchDireccionEmisor.Groups[1].Value.Trim();
                    _logger.LogInformation("Dirección del emisor (Alberto Rubio) extraída: {Direccion}", direccionEmisor);
                }
            }

            if (!string.IsNullOrEmpty(direccionEmisor))
            {
                documento.DireccionEmisor = direccionEmisor;
            }

            // Extraer ciudad del emisor
            string ciudadEmisor = string.Empty;
            if (esFormatoJorgeStein)
            {
                // Buscar Santiago en la dirección
                var matchCiudadEmisor = Regex.Match(texto, @"Casa\s+Matriz:\s*[A-ZÁÉÍÓÚÑ\s\d\-\.\|']+?([A-ZÁÉÍÓÚÑ]+)(?:\s+SGS)", RegexOptions.IgnoreCase);
                if (matchCiudadEmisor.Success)
                {
                    ciudadEmisor = matchCiudadEmisor.Groups[1].Value.Trim();
                    _logger.LogInformation("Ciudad del emisor (Jorge Stein) extraída: {Ciudad}", ciudadEmisor);
                }
            }
            else if (esFormatoAlbertoRubio)
            {
                // Buscar Santiago en la dirección
                var matchCiudadEmisor = Regex.Match(texto, @"Dirección:\s*[A-ZÁÉÍÓÚÑ\s\d\-\.]+?([A-ZÁÉÍÓÚÑ]+)(?:\s+Teléfono:)", RegexOptions.IgnoreCase);
                if (matchCiudadEmisor.Success)
                {
                    ciudadEmisor = matchCiudadEmisor.Groups[1].Value.Trim();
                    _logger.LogInformation("Ciudad del emisor (Alberto Rubio) extraída: {Ciudad}", ciudadEmisor);
                }
            }

            if (!string.IsNullOrEmpty(ciudadEmisor))
            {
                documento.CiudadEmisor = ciudadEmisor;
            }

            // Extraer nombre del receptor
            string nombreReceptor = string.Empty;
            if (esFormatoJorgeStein)
            {
                // Formato Jorge Stein: Señores : FORUS S.A.
                var matchNombreReceptor = Regex.Match(texto, @"Señores\s*:\s*([A-ZÁÉÍÓÚÑ\s\.]+?)\s+Dirección", RegexOptions.IgnoreCase);
                if (matchNombreReceptor.Success)
                {
                    nombreReceptor = matchNombreReceptor.Groups[1].Value.Trim();
                    _logger.LogInformation("Nombre del receptor (Jorge Stein) extraído: {Nombre}", nombreReceptor);
                }
            }
            else if (esFormatoAlbertoRubio)
            {
                // Formato Alberto Rubio: SEÑOR (ES) : IMPORTACIONES VK IMPORTS SPA
                var matchNombreReceptor = Regex.Match(texto, @"SEÑOR\s*\(ES\)\s*:\s*([A-ZÁÉÍÓÚÑ\s]+?)\s+RUT\s*:", RegexOptions.IgnoreCase);
                if (matchNombreReceptor.Success)
                {
                    nombreReceptor = matchNombreReceptor.Groups[1].Value.Trim();
                    _logger.LogInformation("Nombre del receptor (Alberto Rubio) extraído: {Nombre}", nombreReceptor);
                }
            }

            if (!string.IsNullOrEmpty(nombreReceptor))
            {
                documento.NombreReceptor = nombreReceptor;
            }
            else
            {
                _logger.LogWarning("No se pudo extraer el nombre del receptor");
            }

            // Extraer RUT del receptor
            string rutReceptor = string.Empty;
            if (esFormatoJorgeStein)
            {
                // Formato Jorge Stein: R.U.T. : 86,963,200-7
                var matchRutReceptor = Regex.Match(texto, @"R\.U\.T\.\s*:\s*(\d{1,2},\d{3},\d{3}-\d)", RegexOptions.IgnoreCase);
                if (matchRutReceptor.Success)
                {
                    rutReceptor = matchRutReceptor.Groups[1].Value.Replace(",", ".");
                    _logger.LogInformation("RUT del receptor (Jorge Stein) extraído: {Rut}", rutReceptor);
                }
            }
            else if (esFormatoAlbertoRubio)
            {
                // Formato Alberto Rubio: RUT : 77.591.058-5
                var matchRutReceptor = Regex.Match(texto, @"RUT\s*:\s*(\d{1,2}\.\d{3}\.\d{3}-\d)", RegexOptions.IgnoreCase);
                if (matchRutReceptor.Success)
                {
                    rutReceptor = matchRutReceptor.Groups[1].Value.Trim();
                    _logger.LogInformation("RUT del receptor (Alberto Rubio) extraído: {Rut}", rutReceptor);
                }
            }

            if (!string.IsNullOrEmpty(rutReceptor))
            {
                documento.RutReceptor = rutReceptor;
            }
            else
            {
                _logger.LogWarning("No se pudo extraer el RUT del receptor");
            }

            // Extraer dirección del receptor
            string direccionReceptor = string.Empty;
            if (esFormatoJorgeStein)
            {
                // Formato Jorge Stein: Dirección : AVENIDA LAS CONDES 11281
                var matchDireccionReceptor = Regex.Match(texto, @"Dirección\s*:\s*([A-ZÁÉÍÓÚÑ\s\d]+?)\s+Comuna", RegexOptions.IgnoreCase);
                if (matchDireccionReceptor.Success)
                {
                    direccionReceptor = matchDireccionReceptor.Groups[1].Value.Trim();
                    _logger.LogInformation("Dirección del receptor (Jorge Stein) extraída: {Direccion}", direccionReceptor);
                }
            }
            else if (esFormatoAlbertoRubio)
            {
                // Formato Alberto Rubio: DIRECCIÓN : PILOTO LAZO 50
                var matchDireccionReceptor = Regex.Match(texto, @"DIRECCIÓN\s*:\s*([A-ZÁÉÍÓÚÑ\s\d\-\.]+?)\s+TIPO\s+DESPACHO\s*:", RegexOptions.IgnoreCase);
                if (matchDireccionReceptor.Success)
                {
                    direccionReceptor = matchDireccionReceptor.Groups[1].Value.Trim();
                    _logger.LogInformation("Dirección del receptor (Alberto Rubio) extraída: {Direccion}", direccionReceptor);
                }
            }

            if (!string.IsNullOrEmpty(direccionReceptor))
            {
                documento.DireccionReceptor = direccionReceptor;
            }

            // Extraer comuna del receptor
            string comunaReceptor = string.Empty;
            if (esFormatoJorgeStein)
            {
                // Formato Jorge Stein: Comuna : LAS CONDES
                var matchComunaReceptor = Regex.Match(texto, @"Comuna\s*:\s*([A-ZÁÉÍÓÚÑ\s]+?)\s+Ciudad", RegexOptions.IgnoreCase);
                if (matchComunaReceptor.Success)
                {
                    comunaReceptor = matchComunaReceptor.Groups[1].Value.Trim();
                    _logger.LogInformation("Comuna del receptor (Jorge Stein) extraída: {Comuna}", comunaReceptor);
                }
            }
            else if (esFormatoAlbertoRubio)
            {
                // Formato Alberto Rubio: COMUNA : CERRILLOS
                var matchComunaReceptor = Regex.Match(texto, @"COMUNA\s*:\s*([A-ZÁÉÍÓÚÑ\s]+?)\s+CIUDAD\s*:", RegexOptions.IgnoreCase);
                if (matchComunaReceptor.Success)
                {
                    comunaReceptor = matchComunaReceptor.Groups[1].Value.Trim();
                    _logger.LogInformation("Comuna del receptor (Alberto Rubio) extraída: {Comuna}", comunaReceptor);
                }
            }

            if (!string.IsNullOrEmpty(comunaReceptor))
            {
                documento.ComunaReceptor = comunaReceptor;
            }

            // Extraer ciudad del receptor
            string ciudadReceptor = string.Empty;
            if (esFormatoJorgeStein)
            {
                // Formato Jorge Stein: Ciudad : SANTIAGO
                var matchCiudadReceptor = Regex.Match(texto, @"Ciudad\s*:\s*([A-ZÁÉÍÓÚÑ\s]+?)\s+Dirección\s+de\s+Entrega", RegexOptions.IgnoreCase);
                if (matchCiudadReceptor.Success)
                {
                    ciudadReceptor = matchCiudadReceptor.Groups[1].Value.Trim();
                    _logger.LogInformation("Ciudad del receptor (Jorge Stein) extraída: {Ciudad}", ciudadReceptor);
                }
            }
            else if (esFormatoAlbertoRubio)
            {
                // Formato Alberto Rubio: CIUDAD : SANTIAGO
                var matchCiudadReceptor = Regex.Match(texto, @"CIUDAD\s*:\s*([A-ZÁÉÍÓÚÑ\s]+?)\s+ADUANA", RegexOptions.IgnoreCase);
                if (matchCiudadReceptor.Success)
                {
                    ciudadReceptor = matchCiudadReceptor.Groups[1].Value.Trim();
                    _logger.LogInformation("Ciudad del receptor (Alberto Rubio) extraída: {Ciudad}", ciudadReceptor);
                }
            }

            if (!string.IsNullOrEmpty(ciudadReceptor))
            {
                documento.CiudadReceptor = ciudadReceptor;
            }

            // Extraer transportista
            string transportista = string.Empty;
            if (esFormatoJorgeStein)
            {
                // Formato Jorge Stein: Transporte : CHILE CARGO
                var matchTransportista = Regex.Match(texto, @"Transporte\s*:\s*([A-ZÁÉÍÓÚÑ\s]+?)(?:\s+Chofer|\s*$)", RegexOptions.IgnoreCase);
                if (matchTransportista.Success)
                {
                    transportista = matchTransportista.Groups[1].Value.Trim();
                    _logger.LogInformation("Transportista (Jorge Stein) extraído: {Transportista}", transportista);
                }
            }
            else if (esFormatoAlbertoRubio)
            {
                // Formato Alberto Rubio: TRANSPORTADO POR : VK IMPORTS
                var matchTransportista = Regex.Match(texto, @"TRANSPORTADO\s+POR\s*:\s*([A-ZÁÉÍÓÚÑ\s]+?)\s+VEHÍCULO\s*:", RegexOptions.IgnoreCase);
                if (matchTransportista.Success)
                {
                    transportista = matchTransportista.Groups[1].Value.Trim();
                    _logger.LogInformation("Transportista (Alberto Rubio) extraído: {Transportista}", transportista);
                }
            }

            if (!string.IsNullOrEmpty(transportista))
            {
                documento.Transportista = transportista;
            }

            // Extraer patente del vehículo
            string patenteVehiculo = string.Empty;
            if (esFormatoJorgeStein)
            {
                // Formato Jorge Stein: Patente Vehiculo :
                var matchPatente = Regex.Match(texto, @"Patente\s+Vehiculo\s*:\s*([A-ZÁÉÍÓÚÑ\s\d]+?)(?:\s+NºSELLO|\s*$)", RegexOptions.IgnoreCase);
                if (matchPatente.Success)
                {
                    patenteVehiculo = matchPatente.Groups[1].Value.Trim();
                    _logger.LogInformation("Patente del vehículo (Jorge Stein) extraída: {Patente}", patenteVehiculo);
                }
            }
            else if (esFormatoAlbertoRubio)
            {
                // Formato Alberto Rubio: PATENTE : hasta CHOFER :
                var matchPatente = Regex.Match(texto, @"PATENTE\s*:\s*([A-ZÁÉÍÓÚÑ\s\d]+?)\s+CHOFER\s*:", RegexOptions.IgnoreCase);
                if (matchPatente.Success)
                {
                    patenteVehiculo = matchPatente.Groups[1].Value.Trim();
                    _logger.LogInformation("Patente del vehículo (Alberto Rubio) extraída: {Patente}", patenteVehiculo);
                }
            }

            if (!string.IsNullOrEmpty(patenteVehiculo))
            {
                documento.PatenteVehiculo = patenteVehiculo;
            }

            // Extraer chofer
            string chofer = string.Empty;
            if (esFormatoJorgeStein)
            {
                // Formato Jorge Stein: Chofer :x
                var matchChofer = Regex.Match(texto, @"Chofer\s*:\s*([A-ZÁÉÍÓÚÑ\s]+?)(?:\s+Patente|\s*$)", RegexOptions.IgnoreCase);
                if (matchChofer.Success)
                {
                    chofer = matchChofer.Groups[1].Value.Trim();
                    _logger.LogInformation("Chofer (Jorge Stein) extraído: {Chofer}", chofer);
                }
            }
            else if (esFormatoAlbertoRubio)
            {
                // Formato Alberto Rubio: CHOFER : hasta DIRECCIÓN DESTINO :
                var matchChofer = Regex.Match(texto, @"CHOFER\s*:\s*([A-ZÁÉÍÓÚÑ\s]+?)\s+DIRECCIÓN\s+DESTINO\s*:", RegexOptions.IgnoreCase);
                if (matchChofer.Success)
                {
                    chofer = matchChofer.Groups[1].Value.Trim();
                    _logger.LogInformation("Chofer (Alberto Rubio) extraído: {Chofer}", chofer);
                }
            }

            if (!string.IsNullOrEmpty(chofer))
            {
                documento.Chofer = chofer;
            }

            // Extraer dirección de destino
            string destino = string.Empty;
            if (esFormatoJorgeStein)
            {
                // Formato Jorge Stein: Dirección de Entrega : CAMINO MELIPILLA 9400B , MAIPU, . .
                var matchDestino = Regex.Match(texto, @"Dirección\s+de\s+Entrega\s*:\s*([A-ZÁÉÍÓÚÑ\s\d\-\.\,]+?)(?:\s+SIRVASE|\s*$)", RegexOptions.IgnoreCase);
                if (matchDestino.Success)
                {
                    destino = matchDestino.Groups[1].Value.Trim();
                    _logger.LogInformation("Dirección de destino (Jorge Stein) extraída: {Destino}", destino);
                }
            }
            else if (esFormatoAlbertoRubio)
            {
                // Formato Alberto Rubio: DIRECCIÓN DESTINO : PILOTO LAZO 50 PILOTO LAZO 50
                var matchDestino = Regex.Match(texto, @"DIRECCIÓN\s+DESTINO\s*:\s*([A-ZÁÉÍÓÚÑ\s\d\-\.]+?)(?:\s+IDENTIFICACIÓN|\s*$)", RegexOptions.IgnoreCase);
                if (matchDestino.Success)
                {
                    destino = matchDestino.Groups[1].Value.Trim();
                    _logger.LogInformation("Dirección de destino (Alberto Rubio) extraída: {Destino}", destino);
                }
            }

            if (!string.IsNullOrEmpty(destino))
            {
                documento.Destino = destino;
            }

            // Extraer número de despacho
            string numeroDespacho = string.Empty;
            if (esFormatoJorgeStein)
            {
                // Formato Jorge Stein: Nuestro Despacho : I-2506193
                var matchDespacho = Regex.Match(texto, @"Nuestro\s+Despacho\s*:\s*([A-ZÁÉÍÓÚÑ\s\d\-]+)", RegexOptions.IgnoreCase);
                if (matchDespacho.Success)
                {
                    numeroDespacho = matchDespacho.Groups[1].Value.Trim();
                    _logger.LogInformation("Número de despacho (Jorge Stein) extraído: {Despacho}", numeroDespacho);
                }
            }
            else if (esFormatoAlbertoRubio)
            {
                // Formato Alberto Rubio: N° DESPACHO : 10758
                var matchDespacho = Regex.Match(texto, @"N°\s+DESPACHO\s*:\s*(\d+)", RegexOptions.IgnoreCase);
                if (matchDespacho.Success)
                {
                    numeroDespacho = matchDespacho.Groups[1].Value;
                    _logger.LogInformation("Número de despacho (Alberto Rubio) extraído: {Despacho}", numeroDespacho);
                }
            }

            if (!string.IsNullOrEmpty(numeroDespacho))
            {
                documento.NumeroDespacho = numeroDespacho;
            }

            // Extraer fecha de despacho - usa la misma fecha del documento
            if (documento.FechaDocumento.HasValue)
            {
                documento.FechaDespacho = documento.FechaDocumento;
                _logger.LogInformation("Fecha de despacho establecida: {Fecha}", documento.FechaDespacho);
            }

            // Extraer aduana
            string aduana = string.Empty;
            if (esFormatoJorgeStein)
            {
                // Formato Jorge Stein: Aduana : SAN ANTONIO
                var matchAduana = Regex.Match(texto, @"Aduana\s*:\s*([A-ZÁÉÍÓÚÑ\s]+?)(?:\s+R\.U\.T\.|\s*$)", RegexOptions.IgnoreCase);
                if (matchAduana.Success)
                {
                    aduana = matchAduana.Groups[1].Value.Trim();
                    _logger.LogInformation("Aduana (Jorge Stein) extraída: {Aduana}", aduana);
                }
            }
            else if (esFormatoAlbertoRubio)
            {
                // Formato Alberto Rubio: ADUANA SAN ANTONIO
                var matchAduana = Regex.Match(texto, @"ADUANA\s+([A-ZÁÉÍÓÚÑ\s]+?)\s+REF\.\s*:", RegexOptions.IgnoreCase);
                if (matchAduana.Success)
                {
                    aduana = matchAduana.Groups[1].Value.Trim();
                    _logger.LogInformation("Aduana (Alberto Rubio) extraída: {Aduana}", aduana);
                }
            }

            if (!string.IsNullOrEmpty(aduana))
            {
                documento.Aduana = aduana;
            }

            // Extraer referencia
            string referencia = string.Empty;
            if (esFormatoJorgeStein)
            {
                // Formato Jorge Stein: Su Referencia : 437662-437661
                var matchReferencia = Regex.Match(texto, @"Su\s+Referencia\s*:\s*([A-ZÁÉÍÓÚÑ\s\d\-\.]+?)\s+Nuestro", RegexOptions.IgnoreCase);
                if (matchReferencia.Success)
                {
                    referencia = matchReferencia.Groups[1].Value.Trim();
                    _logger.LogInformation("Referencia (Jorge Stein) extraída: {Referencia}", referencia);
                }
            }
            else if (esFormatoAlbertoRubio)
            {
                // Formato Alberto Rubio: REF : TCLU9522391
                var matchReferencia = Regex.Match(texto, @"REF\.\s*:\s*([A-ZÁÉÍÓÚÑ\s\d\-\.]+?)\s+NAVE:", RegexOptions.IgnoreCase);
                if (matchReferencia.Success)
                {
                    referencia = matchReferencia.Groups[1].Value.Trim();
                    _logger.LogInformation("Referencia (Alberto Rubio) extraída: {Referencia}", referencia);
                }
            }

            if (!string.IsNullOrEmpty(referencia))
            {
                documento.Referencia = referencia;
            }

            // Extraer conocimiento de embarque
            string conocimientoEmbarque = string.Empty;
            if (esFormatoJorgeStein)
            {
                // Formato Jorge Stein: Conocimiento Nº: DACA78565
                var matchConocimiento = Regex.Match(texto, @"Conocimiento\s+Nº:\s*([A-ZÁÉÍÓÚÑ\s\d]+?)(?:\s+Señores|\s*$)", RegexOptions.IgnoreCase);
                if (matchConocimiento.Success)
                {
                    conocimientoEmbarque = matchConocimiento.Groups[1].Value.Trim();
                    _logger.LogInformation("Conocimiento de embarque (Jorge Stein) extraído: {Conocimiento}", conocimientoEmbarque);
                }
            }
            else if (esFormatoAlbertoRubio)
            {
                // Formato Alberto Rubio: CONOCIMIENTO N° : MEDUJB104621
                var matchConocimiento = Regex.Match(texto, @"CONOCIMIENTO\s+N°\s*:\s*([A-ZÁÉÍÓÚÑ\s\d]+?)(?:\s+IDENTIFICACIÓN|\s*$)", RegexOptions.IgnoreCase);
                if (matchConocimiento.Success)
                {
                    conocimientoEmbarque = matchConocimiento.Groups[1].Value.Trim();
                    _logger.LogInformation("Conocimiento de embarque (Alberto Rubio) extraído: {Conocimiento}", conocimientoEmbarque);
                }
            }

            if (!string.IsNullOrEmpty(conocimientoEmbarque))
            {
                documento.ConocimientoEmbarque = conocimientoEmbarque;
            }

            // Extraer manifiesto
            string manifiesto = string.Empty;
            if (esFormatoJorgeStein)
            {
                // Formato Jorge Stein: Manifiesto Nº: 257809
                var matchManifiesto = Regex.Match(texto, @"Manifiesto\s+Nº:\s*(\d+)", RegexOptions.IgnoreCase);
                if (matchManifiesto.Success)
                {
                    manifiesto = matchManifiesto.Groups[1].Value;
                    _logger.LogInformation("Manifiesto (Jorge Stein) extraído: {Manifiesto}", manifiesto);
                }
            }
            else if (esFormatoAlbertoRubio)
            {
                // Formato Alberto Rubio: MANIFIESTO N° : 257947
                var matchManifiesto = Regex.Match(texto, @"MANIFIESTO\s+N°\s*:\s*(\d+)", RegexOptions.IgnoreCase);
                if (matchManifiesto.Success)
                {
                    manifiesto = matchManifiesto.Groups[1].Value;
                    _logger.LogInformation("Manifiesto (Alberto Rubio) extraído: {Manifiesto}", manifiesto);
                }
            }

            if (!string.IsNullOrEmpty(manifiesto))
            {
                documento.Manifiesto = manifiesto;
            }

            // Extraer peso
            decimal? peso = null;
            if (esFormatoJorgeStein)
            {
                // Formato Jorge Stein: KILOS BRUTOS CMAU6501200 1 CONT40 COD 1 5384,00
                var matchPeso = Regex.Match(texto, @"KILOS\s+BRUTOS\s+[A-ZÁÉÍÓÚÑ\s\d]+\s+\d+\s+[A-ZÁÉÍÓÚÑ\s\d]+\s+[A-ZÁÉÍÓÚÑ\s\d]+\s+([\d\.,]+)", RegexOptions.IgnoreCase);
                if (matchPeso.Success)
                {
                    var pesoStr = matchPeso.Groups[1].Value.Replace(",", ".");
                    if (decimal.TryParse(pesoStr, out var pesoValor))
                    {
                        peso = pesoValor;
                        _logger.LogInformation("Peso (Jorge Stein) extraído: {Peso}", peso);
                    }
                }
            }
            else if (esFormatoAlbertoRubio)
            {
                // Formato Alberto Rubio: PESO BRUTO (Kgs.) : 12.215,00
                var matchPeso = Regex.Match(texto, @"PESO\s+BRUTO\s*\(Kgs\.\)\s*:\s*([\d\.,]+)", RegexOptions.IgnoreCase);
                if (matchPeso.Success)
                {
                    var pesoStr = matchPeso.Groups[1].Value.Replace(",", ".");
                    if (decimal.TryParse(pesoStr, out var pesoValor))
                    {
                        peso = pesoValor;
                        _logger.LogInformation("Peso (Alberto Rubio) extraído: {Peso}", peso);
                    }
                }
            }

            if (peso.HasValue)
            {
                documento.Peso = peso;
            }

            // Extraer CIF USD
            decimal? cifUSD = null;
            if (esFormatoJorgeStein)
            {
                // Formato Jorge Stein: Valor Aduanero US $ 201,649.06
                var matchCIF = Regex.Match(texto, @"Valor\s+Aduanero\s+US\s*\$\s*([\d\.,]+)", RegexOptions.IgnoreCase);
                if (matchCIF.Success)
                {
                    var cifStr = matchCIF.Groups[1].Value.Replace(",", "");
                    if (decimal.TryParse(cifStr, out var cifValor))
                    {
                        cifUSD = cifValor;
                        _logger.LogInformation("CIF USD (Jorge Stein) extraído: {CIF}", cifUSD);
                    }
                }
            }
            else if (esFormatoAlbertoRubio)
            {
                // Formato Alberto Rubio: VALOR CIF : USD $ : 33.177,00
                var matchCIF = Regex.Match(texto, @"VALOR\s+CIF\s*:\s*USD\s*\$\s*:\s*([\d\.,]+)", RegexOptions.IgnoreCase);
                if (matchCIF.Success)
                {
                    var cifStr = matchCIF.Groups[1].Value.Replace(",", ".");
                    if (decimal.TryParse(cifStr, out var cifValor))
                    {
                        cifUSD = cifValor;
                        _logger.LogInformation("CIF USD (Alberto Rubio) extraído: {CIF}", cifUSD);
                    }
                }
            }

            if (cifUSD.HasValue)
            {
                documento.CIFUSD = cifUSD;
            }

            // Extraer cantidad de bultos
            int? cantidadBultos = null;
            if (esFormatoJorgeStein)
            {
                // Formato Jorge Stein: CANTIDAD Y TIPO BULTOS CMAU6501200 1 CONT40
                var matchCantidadBultos = Regex.Match(texto, @"CANTIDAD\s+Y\s+TIPO\s+BULTOS\s+[A-ZÁÉÍÓÚÑ\s\d]+\s+(\d+)", RegexOptions.IgnoreCase);
                if (matchCantidadBultos.Success)
                {
                    if (int.TryParse(matchCantidadBultos.Groups[1].Value, out var cantidad))
                    {
                        cantidadBultos = cantidad;
                        _logger.LogInformation("Cantidad de bultos (Jorge Stein) extraída: {Cantidad}", cantidadBultos);
                    }
                }
            }
            else if (esFormatoAlbertoRubio)
            {
                // Formato Alberto Rubio: CANTIDAD 1
                var matchCantidadBultos = Regex.Match(texto, @"CANTIDAD\s+(\d+)", RegexOptions.IgnoreCase);
                if (matchCantidadBultos.Success)
                {
                    if (int.TryParse(matchCantidadBultos.Groups[1].Value, out var cantidad))
                    {
                        cantidadBultos = cantidad;
                        _logger.LogInformation("Cantidad de bultos (Alberto Rubio) extraída: {Cantidad}", cantidadBultos);
                    }
                }
            }

            if (cantidadBultos.HasValue)
            {
                documento.CantidadBultos = cantidadBultos;
            }

            // Extraer tipo de bulto
            string tipoBulto = string.Empty;
            if (esFormatoJorgeStein)
            {
                // Formato Jorge Stein: CANTIDAD Y TIPO BULTOS CMAU6501200 1 CONT40
                var matchTipoBulto = Regex.Match(texto, @"CANTIDAD\s+Y\s+TIPO\s+BULTOS\s+[A-ZÁÉÍÓÚÑ\s\d]+\s+\d+\s+([A-ZÁÉÍÓÚÑ\s\d]+)", RegexOptions.IgnoreCase);
                if (matchTipoBulto.Success)
                {
                    tipoBulto = matchTipoBulto.Groups[1].Value.Trim();
                    _logger.LogInformation("Tipo de bulto (Jorge Stein) extraído: {TipoBulto}", tipoBulto);
                }
            }
            else if (esFormatoAlbertoRubio)
            {
                // Formato Alberto Rubio: TIPO O CLASE CONT40
                var matchTipoBulto = Regex.Match(texto, @"TIPO\s+O\s+CLASE\s+([A-ZÁÉÍÓÚÑ\s\d]+)", RegexOptions.IgnoreCase);
                if (matchTipoBulto.Success)
                {
                    tipoBulto = matchTipoBulto.Groups[1].Value.Trim();
                    _logger.LogInformation("Tipo de bulto (Alberto Rubio) extraído: {TipoBulto}", tipoBulto);
                }
            }

            if (!string.IsNullOrEmpty(tipoBulto))
            {
                documento.TipoBulto = tipoBulto;
            }

            // Extraer observaciones
            string observaciones = string.Empty;
            if (esFormatoJorgeStein)
            {
                // Formato Jorge Stein: OBSERVACIONES Aplastado Mercaderia a la Vista Carton Rolo
                var matchObservaciones = Regex.Match(texto, @"OBSERVACIONES\s+([A-ZÁÉÍÓÚÑ\s\d\-\.\-]+?)(?:\s+Firma|\s*$)", RegexOptions.IgnoreCase);
                if (matchObservaciones.Success)
                {
                    observaciones = matchObservaciones.Groups[1].Value.Trim();
                    _logger.LogInformation("Observaciones (Jorge Stein) extraídas: {Observaciones}", observaciones);
                }
            }
            else if (esFormatoAlbertoRubio)
            {
                // Formato Alberto Rubio: OBSERVACIONES RECEPCION CONSIGNATARIO:
                var matchObservaciones = Regex.Match(texto, @"OBSERVACIONES\s+RECEPCION\s+CONSIGNATARIO:\s*([A-ZÁÉÍÓÚÑ\s\d\-\.\-]+?)(?:\s+Timbre|\s*$)", RegexOptions.IgnoreCase);
                if (matchObservaciones.Success)
                {
                    observaciones = matchObservaciones.Groups[1].Value.Trim();
                    _logger.LogInformation("Observaciones (Alberto Rubio) extraídas: {Observaciones}", observaciones);
                }
            }

            if (!string.IsNullOrEmpty(observaciones))
            {
                documento.Observaciones = observaciones;
            }

            // Establecer valores por defecto para campos no encontrados
            if (string.IsNullOrEmpty(documento.Origen))
            {
                documento.Origen = "N";
                _logger.LogInformation("Origen establecido por defecto: {Origen}", documento.Origen);
            }

            if (string.IsNullOrEmpty(documento.ConocimientoEmbarque))
            {
                documento.ConocimientoEmbarque = "N";
                _logger.LogInformation("Conocimiento de embarque establecido por defecto: {Conocimiento}", documento.ConocimientoEmbarque);
            }

            if (string.IsNullOrEmpty(documento.TipoBulto))
            {
                documento.TipoBulto = "DESPACHO";
                _logger.LogInformation("Tipo de bulto establecido por defecto: {TipoBulto}", documento.TipoBulto);
            }
        }

        /// <summary>
        /// Extrae texto de un archivo PNG usando Azure Computer Vision
        /// </summary>
        private async Task<string> ExtraerTextoPngAsync(Stream fileStream)
        {
            try
            {
                // Usar Azure Vision directamente sin procesamiento local
                return await CarnetAduaneroProcessorService.ProcessImageWithSkiaSharpAsync(
                    fileStream, 
                    _azureVisionKey, 
                    _azureVisionEndpoint, 
                    _logger
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extrayendo texto de PNG con Azure Vision");
                throw;
            }
        }

        /// <summary>
        /// Extrae texto usando Azure Computer Vision
        /// </summary>
        private async Task<string> ExtraerTextoConAzureVisionAsync(Bitmap image)
        {
            try
            {
                if (string.IsNullOrEmpty(_azureVisionKey) || string.IsNullOrEmpty(_azureVisionEndpoint))
                {
                    throw new InvalidOperationException("Configuración de Azure Vision no encontrada");
                }

                var credential = new Azure.AzureKeyCredential(_azureVisionKey);
                var client = new ImageAnalysisClient(new Uri(_azureVisionEndpoint), credential);

                using var memoryStream = new MemoryStream();
                image.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
                memoryStream.Position = 0;

                var imageData = BinaryData.FromStream(memoryStream);
                var options = new ImageAnalysisOptions
                {
                    Language = "es"
                };

                var result = await client.AnalyzeAsync(imageData, VisualFeatures.Read, options);

                if (result.Value?.Read?.Blocks != null)
                {
                    var textoCompleto = string.Join(" ", result.Value.Read.Blocks.SelectMany(b => b.Lines?.Select(l => l.Text) ?? Array.Empty<string>()));
                    _logger.LogInformation("Texto extraído con Azure Vision: {Texto}", textoCompleto.Substring(0, Math.Min(200, textoCompleto.Length)));
                    return textoCompleto;
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en extracción con Azure Vision");
                throw;
            }
        }

        /// <summary>
        /// Normaliza el texto extraído por OCR
        /// </summary>
        private string NormalizarTexto(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return string.Empty;

            // Normalizar espacios y saltos de línea
            var textoNormalizado = Regex.Replace(texto, @"\s+", " ");
            textoNormalizado = textoNormalizado.Replace("\n", " ").Replace("\r", " ");
            textoNormalizado = Regex.Replace(textoNormalizado, @"\s+", " ");

            return textoNormalizado.Trim();
        }

        /// <summary>
        /// Calcula el hash SHA256 de un stream
        /// </summary>
        private async Task<string> CalcularHashAsync(Stream stream)
        {
            using var sha256 = SHA256.Create();
            var hashBytes = await sha256.ComputeHashAsync(stream);
            return Convert.ToHexString(hashBytes).ToLower();
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validando archivo PNG: {FilePath}", filePath);
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

                // Verificar firma PNG: 89 50 4E 47 0D 0A 1A 0A
                return buffer[0] == 0x89 && buffer[1] == 0x50 && buffer[2] == 0x4E && buffer[3] == 0x47 &&
                       buffer[4] == 0x0D && buffer[5] == 0x0A && buffer[6] == 0x1A && buffer[7] == 0x0A;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validando stream PNG");
                return false;
            }
        }
    }
} 

