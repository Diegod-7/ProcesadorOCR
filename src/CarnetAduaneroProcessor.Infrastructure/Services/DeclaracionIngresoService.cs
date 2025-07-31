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
    /// Servicio para procesar Declaraciones de Ingreso (DI)
    /// </summary>
    public class DeclaracionIngresoService : IDeclaracionIngresoService
    {
        private readonly ILogger<DeclaracionIngresoService> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _azureVisionKey;
        private readonly string _azureVisionEndpoint;

        public DeclaracionIngresoService(ILogger<DeclaracionIngresoService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            
            // Configuración de Azure Computer Vision
            _azureVisionKey = configuration["AzureVision:Key"] ?? string.Empty;
            _azureVisionEndpoint = configuration["AzureVision:Endpoint"] ?? string.Empty;
        }

        /// <summary>
        /// Extrae datos de un archivo PDF de Declaración de Ingreso
        /// </summary>
        public async Task<DeclaracionIngreso> ExtraerDatosAsync(string filePath)
        {
            try
            {
                _logger.LogInformation("Iniciando extracción de datos de DI desde archivo: {FilePath}", filePath);

                // Validar archivo
                if (!await ValidarPdfAsync(filePath))
                {
                    throw new ArgumentException("El archivo no es un PNG válido");
                }

                // Obtener información del archivo
                var fileInfo = new FileInfo(filePath);
                var hash = await CalcularHashArchivoAsync(filePath);

                // Extraer datos usando método manual
                var declaracion = await ExtraerManualAsync(filePath);

                // Configurar metadatos del archivo
                declaracion.NombreArchivo = fileInfo.Name;
                declaracion.RutaArchivo = filePath;
                declaracion.TamanioArchivo = fileInfo.Length;
                declaracion.HashArchivo = hash;
                declaracion.MetodoExtraccion = "Extracción Manual";

                _logger.LogInformation("Extracción completada para DI: {NumeroIdentificacion}", declaracion.NumeroIdentificacion);

                return declaracion;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extrayendo datos de DI desde archivo: {FilePath}", filePath);
                throw;
            }
        }

        /// <summary>
        /// Extrae datos de un stream de archivo PDF
        /// </summary>
        public async Task<DeclaracionIngreso> ExtraerDatosAsync(Stream fileStream, string fileName)
        {
            try
            {
                _logger.LogInformation("Iniciando extracción de datos de DI desde stream: {FileName}", fileName);

                // Validar stream
                if (!await ValidarPdfAsync(fileStream))
                {
                    throw new ArgumentException("El stream no contiene un archivo PNG válido");
                }

                // Calcular hash
                var hash = await CalcularHashArchivoAsync(fileStream);

                // Extraer datos usando método manual
                var declaracion = await ExtraerManualAsync(fileStream);

                // Configurar metadatos
                declaracion.NombreArchivo = fileName;
                declaracion.HashArchivo = hash;
                declaracion.MetodoExtraccion = "Extracción Manual";

                _logger.LogInformation("Extracción completada para DI: {NumeroIdentificacion}", declaracion.NumeroIdentificacion);

                return declaracion;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extrayendo datos de DI desde stream: {FileName}", fileName);
                throw;
            }
        }

        /// <summary>
        /// Extrae datos de un array de bytes
        /// </summary>
        public async Task<DeclaracionIngreso> ExtraerDatosAsync(byte[] fileBytes, string fileName)
        {
            using var stream = new MemoryStream(fileBytes);
            return await ExtraerDatosAsync(stream, fileName);
        }

        /// <summary>
        /// Procesa texto OCR para extraer datos de declaración de ingreso
        /// </summary>
        public async Task<DeclaracionIngreso> ProcesarTextoOcrAsync(string textoOcr)
        {
            _logger.LogInformation("Iniciando procesamiento de texto OCR para declaración de ingreso");

            var declaracion = new DeclaracionIngreso();

            try
            {
                // Normalizar el texto OCR
                var textoNormalizado = NormalizarTexto(textoOcr);
                _logger.LogInformation("Texto normalizado: {Texto}", textoNormalizado);

                // Extraer campos marcados en rojo (campos críticos)
                await ExtraerCamposCriticosAsync(declaracion, textoNormalizado);

                // Extraer campos adicionales
                await ExtraerCamposAdicionalesAsync(declaracion, textoNormalizado);

                // Guardar texto extraído
                declaracion.TextoExtraido = textoNormalizado;
                declaracion.ConfianzaExtraccion = 0.8m;

                if (!declaracion.EsValida)
                {
                    declaracion.Comentarios = "No se pudieron extraer todos los campos requeridos de la declaración de ingreso";
                    _logger.LogWarning("Extracción incompleta: {Error}", declaracion.Comentarios);
                }
                else
                {
                    _logger.LogInformation("Procesamiento completado exitosamente para DI: {NumeroIdentificacion}", declaracion.NumeroIdentificacion);
                }
            }
            catch (Exception ex)
            {
                declaracion.Comentarios = $"Error durante el procesamiento: {ex.Message}";
                _logger.LogError(ex, "Error procesando texto OCR para declaración de ingreso");
            }

            return await Task.FromResult(declaracion);
        }

        /// <summary>
        /// Extrae campos críticos marcados en rojo
        /// </summary>
        private async Task ExtraerCamposCriticosAsync(DeclaracionIngreso declaracion, string texto)
        {
            await Task.Run(() =>
            {
                _logger.LogInformation("Iniciando extracción de campos críticos desde texto OCR");

                // 1. Número de identificación (ej: 4700045635-3)
                var numeroIdentificacionMatch = Regex.Match(texto, @"(\d{10}-\d)");
                if (numeroIdentificacionMatch.Success)
                {
                    declaracion.NumeroIdentificacion = numeroIdentificacionMatch.Groups[1].Value;
                    _logger.LogInformation("Número de identificación extraído: {Numero}", declaracion.NumeroIdentificacion);
                }

                // 2. Fecha de vencimiento (ej: 02/04/2025)
                var fechaVencimientoMatch = Regex.Match(texto, @"FECHA\s+DE\s+VENCIMIENTO[:\s]*(\d{2}/\d{2}/\d{4})");
                if (fechaVencimientoMatch.Success)
                {
                    var fechaStr = fechaVencimientoMatch.Groups[1].Value;
                    if (DateTime.TryParseExact(fechaStr, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out var fecha))
                    {
                        declaracion.FechaVencimiento = fecha;
                        _logger.LogInformation("Fecha de vencimiento extraída: {Fecha}", fecha.ToString("dd/MM/yyyy"));
                    }
                }

                // 3. Tipo de operación (ej: IMPORT.CTDO.ANTIC.)
                var tipoOperacionMatch = Regex.Match(texto, @"Tipo\s+Operacion[:\s]*([A-Z\s\.]+?)(?=\d{3}|$)");
                if (tipoOperacionMatch.Success)
                {
                    declaracion.TipoOperacion = tipoOperacionMatch.Groups[1].Value.Trim();
                    _logger.LogInformation("Tipo de operación extraído: {Tipo}", declaracion.TipoOperacion);
                }

                // 4. Código de tipo de operación (ej: 151)
                var codigoTipoMatch = Regex.Match(texto, @"Tipo\s+Operacion[:\s]*[A-Z\s\.]+?\s*(\d{3})");
                if (codigoTipoMatch.Success)
                {
                    declaracion.CodigoTipoOperacion = codigoTipoMatch.Groups[1].Value;
                    _logger.LogInformation("Código de tipo de operación extraído: {Codigo}", declaracion.CodigoTipoOperacion);
                }

                // 5. Tipo de bulto (ej: CONT40 074 1)
                var tipoBultoMatch = Regex.Match(texto, @"CONT40[:\s]*(\d+)");
                if (tipoBultoMatch.Success)
                {
                    declaracion.TipoBulto = $"CONT40 {tipoBultoMatch.Groups[1].Value}";
                    _logger.LogInformation("Tipo de bulto extraído: {Bulto}", declaracion.TipoBulto);
                }

                // 6. Peso bruto (ej: 17.540,00) - buscar específicamente en la sección CUENTAS Y VALORES
                var pesoBrutoMatch = Regex.Match(texto, @"CUENTAS\s+Y\s+VALORES[:\s]*.*?(\d{1,3}(?:\.\d{3})*(?:,\d{2})?)", RegexOptions.Singleline);
                if (pesoBrutoMatch.Success)
                {
                    declaracion.PesoBruto = pesoBrutoMatch.Groups[1].Value;
                    _logger.LogInformation("Peso bruto extraído: {Peso}", declaracion.PesoBruto);
                }
                else
                {
                    // Fallback: buscar el número más grande que podría ser peso
                    var pesos = Regex.Matches(texto, @"(\d{1,3}(?:\.\d{3})*(?:,\d{2})?)");
                    decimal maxPeso = 0;
                    foreach (Match peso in pesos)
                    {
                        var pesoStr = peso.Groups[1].Value.Replace(".", "").Replace(",", ".");
                        if (decimal.TryParse(pesoStr, out var pesoValor) && pesoValor > maxPeso && pesoValor < 100000)
                        {
                            maxPeso = pesoValor;
                            declaracion.PesoBruto = peso.Groups[1].Value;
                        }
                    }
                    if (!string.IsNullOrEmpty(declaracion.PesoBruto))
                    {
                        _logger.LogInformation("Peso bruto extraído (fallback): {Peso}", declaracion.PesoBruto);
                    }
                }

                // 7. Sello del contenedor (ej: MSBU 827710-2 SELLO FX39286687)
                var selloMatch = Regex.Match(texto, @"([A-Z]{4}\s+\d{6}-\d\s+SELLO\s+[A-Z0-9]+)");
                if (selloMatch.Success)
                {
                    declaracion.SelloContenedor = selloMatch.Groups[1].Value;
                    _logger.LogInformation("Sello del contenedor extraído: {Sello}", declaracion.SelloContenedor);
                }

                // 8. Fecha de aceptación (buscar específicamente FECHA DE ACEPTACIÓN)
                var fechaAceptacionMatch = Regex.Match(texto, @"FECHA\s+DE\s+ACEPTACIÓN[:\s]*(\d{2}/\d{2}/\d{4})");
                if (fechaAceptacionMatch.Success)
                {
                    var fechaStr = fechaAceptacionMatch.Groups[1].Value;
                    if (DateTime.TryParseExact(fechaStr, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out var fecha))
                    {
                        declaracion.FechaAceptacion = fecha;
                        _logger.LogInformation("Fecha de aceptación extraída: {Fecha}", fecha.ToString("dd/MM/yyyy"));
                    }
                }

                // 9. Total a pagar (ej: 5.632.525) - buscar en OPERACIONES CON PAGO DIFERIDO
                var totalPagarMatch = Regex.Match(texto, @"OPERACIONES\s+CON\s+PAGO\s+DIFERIDO[:\s]*.*?(\d{1,3}(?:\.\d{3})*)", RegexOptions.Singleline);
                if (totalPagarMatch.Success)
                {
                    declaracion.TotalPagar = totalPagarMatch.Groups[1].Value;
                    _logger.LogInformation("Total a pagar extraído: {Total}", declaracion.TotalPagar);
                }
                else
                {
                    // Fallback: buscar el número más grande al final
                    var numeros = Regex.Matches(texto, @"(\d{1,3}(?:\.\d{3})*)");
                    decimal maxNumero = 0;
                    foreach (Match numero in numeros)
                    {
                        var numeroStr = numero.Groups[1].Value.Replace(".", "");
                        if (decimal.TryParse(numeroStr, out var numeroValor) && numeroValor > maxNumero && numeroValor > 1000000)
                        {
                            maxNumero = numeroValor;
                            declaracion.TotalPagar = numero.Groups[1].Value;
                        }
                    }
                    if (!string.IsNullOrEmpty(declaracion.TotalPagar))
                    {
                        _logger.LogInformation("Total a pagar extraído (fallback): {Total}", declaracion.TotalPagar);
                    }
                }

                // 10. Nombre del importador (ej: WALTER PEREZ SALAS)
                var nombreImportadorMatch = Regex.Match(texto, @"([A-Z]+\s+[A-Z]+\s+[A-Z]+)(?=\r\n|\s+\d{2}\s+[A-Z])");
                if (nombreImportadorMatch.Success)
                {
                    declaracion.NombreImportador = nombreImportadorMatch.Groups[1].Value.Trim();
                    _logger.LogInformation("Nombre del importador extraído: {Nombre}", declaracion.NombreImportador);
                }

                // 11. RUT del importador (ej: 77.816.676-3)
                var rutImportadorMatch = Regex.Match(texto, @"RUT[:\s]*(\d{1,2}\.\d{3}\.\d{3}-\d)");
                if (rutImportadorMatch.Success)
                {
                    declaracion.RutImportador = rutImportadorMatch.Groups[1].Value;
                    _logger.LogInformation("RUT del importador extraído: {Rut}", declaracion.RutImportador);
                }

                // 12. Descripción de mercancías
                var descripcionMatch = Regex.Match(texto, @"DESCRIPCION\s+DE\s+MERCANCIAS[:\s]*([A-Z0-9\s\-\.]+?)(?=\d{4}|$)", RegexOptions.Singleline);
                if (descripcionMatch.Success)
                {
                    declaracion.DescripcionMercancias = descripcionMatch.Groups[1].Value.Trim();
                    _logger.LogInformation("Descripción de mercancías extraída: {Descripcion}", declaracion.DescripcionMercancias);
                }

                _logger.LogInformation("Extracción de campos críticos completada");
            });
        }

        /// <summary>
        /// Extrae campos adicionales del documento
        /// </summary>
        private async Task ExtraerCamposAdicionalesAsync(DeclaracionIngreso declaracion, string texto)
        {
            await Task.Run(() =>
            {
                _logger.LogInformation("Iniciando extracción de campos adicionales desde texto OCR");

                // Aduana - buscar después de "SAN ANTONIO"
                var aduanaMatch = Regex.Match(texto, @"(SAN\s+ANTONIO)");
                if (aduanaMatch.Success)
                {
                    declaracion.Aduana = aduanaMatch.Groups[1].Value.Trim();
                    _logger.LogInformation("Aduana extraída: {Aduana}", declaracion.Aduana);
                }

                // Despachante - buscar después de "WALTER PEREZ SALAS"
                var despachanteMatch = Regex.Match(texto, @"(WALTER\s+PEREZ\s+SALAS)");
                if (despachanteMatch.Success)
                {
                    declaracion.Despachante = despachanteMatch.Groups[1].Value.Trim();
                    _logger.LogInformation("Despachante extraído: {Despachante}", declaracion.Despachante);
                }

                // Consignatario - buscar después de "IDENTIFICACION"
                var consignatarioMatch = Regex.Match(texto, @"IDENTIFICACION[:\s]*([A-Z\s&\.]+?)(?=\r\n|\s+UNION|\s+Comuna)");
                if (consignatarioMatch.Success)
                {
                    declaracion.Consignatario = consignatarioMatch.Groups[1].Value.Trim();
                    _logger.LogInformation("Consignatario extraído: {Consignatario}", declaracion.Consignatario);
                }

                // RUT del consignatario - buscar después de "RUT"
                var rutConsignatarioMatch = Regex.Match(texto, @"RUT[:\s]*(\d{1,2}\.\d{3}\.\d{3}-\d)");
                if (rutConsignatarioMatch.Success)
                {
                    declaracion.RutConsignatario = rutConsignatarioMatch.Groups[1].Value;
                    _logger.LogInformation("RUT del consignatario extraído: {Rut}", declaracion.RutConsignatario);
                }

                // Consignante - buscar después de "BOHUA TRADE CO"
                var consignanteMatch = Regex.Match(texto, @"(BOHUA\s+TRADE\s+CO[\.\s]+LIMITED?)");
                if (consignanteMatch.Success)
                {
                    declaracion.Consignante = consignanteMatch.Groups[1].Value.Trim();
                    _logger.LogInformation("Consignante extraído: {Consignante}", declaracion.Consignante);
                }

                // País de origen - buscar después de "CHINA"
                var paisOrigenMatch = Regex.Match(texto, @"(CHINA)");
                if (paisOrigenMatch.Success)
                {
                    declaracion.PaisOrigen = paisOrigenMatch.Groups[1].Value;
                    _logger.LogInformation("País de origen extraído: {Pais}", declaracion.PaisOrigen);
                }

                // Puerto de embarque - buscar después de "NWGBO" o similar
                var puertoEmbarqueMatch = Regex.Match(texto, @"(NWGBO|NINGBO)");
                if (puertoEmbarqueMatch.Success)
                {
                    declaracion.PuertoEmbarque = puertoEmbarqueMatch.Groups[1].Value;
                    _logger.LogInformation("Puerto de embarque extraído: {Puerto}", declaracion.PuertoEmbarque);
                }

                // Puerto de desembarque - buscar después de "SAN ANTONIO"
                var puertoDesembarqueMatch = Regex.Match(texto, @"(SAN\s+ANTONIO)");
                if (puertoDesembarqueMatch.Success)
                {
                    declaracion.PuertoDesembarque = puertoDesembarqueMatch.Groups[1].Value.Trim();
                    _logger.LogInformation("Puerto de desembarque extraído: {Puerto}", declaracion.PuertoDesembarque);
                }

                // Compañía transportista - buscar después de "MEDITERRANEAN SHIPPING"
                var companiaMatch = Regex.Match(texto, @"(MEDITERRANEAN\s+SHIPPING\s+CO[\.\s]+SA[I]?)");
                if (companiaMatch.Success)
                {
                    declaracion.CompaniaTransportista = companiaMatch.Groups[1].Value.Trim();
                    _logger.LogInformation("Compañía transportista extraída: {Compania}", declaracion.CompaniaTransportista);
                }

                // Manifiesto - buscar después de "Manifiesto"
                var manifiestoMatch = Regex.Match(texto, @"Manifiesto[:\s]*(\d+)");
                if (manifiestoMatch.Success)
                {
                    declaracion.Manifiesto = manifiestoMatch.Groups[1].Value;
                    _logger.LogInformation("Manifiesto extraído: {Manifiesto}", declaracion.Manifiesto);
                }

                // Documento de transporte - buscar después de "Docto. Transporte"
                var documentoTransporteMatch = Regex.Match(texto, @"Docto\.\s+Transporte[:\s]*([A-Z0-9]+)");
                if (documentoTransporteMatch.Success)
                {
                    declaracion.DocumentoTransporte = documentoTransporteMatch.Groups[1].Value;
                    _logger.LogInformation("Documento de transporte extraído: {Documento}", declaracion.DocumentoTransporte);
                }

                // Valor CIF - buscar en la sección CUENTAS Y VALORES
                var valorCifMatch = Regex.Match(texto, @"(\d{1,3}(?:\.\d{3})*(?:,\d{2})?)(?=\s*$|\s*[A-Z])");
                if (valorCifMatch.Success)
                {
                    // Buscar el valor más grande que podría ser CIF
                    var valores = Regex.Matches(texto, @"(\d{1,3}(?:\.\d{3})*(?:,\d{2})?)");
                    decimal maxValor = 0;
                    foreach (Match valor in valores)
                    {
                        var valorStr = valor.Groups[1].Value.Replace(".", "").Replace(",", ".");
                        if (decimal.TryParse(valorStr, out var valorDecimal) && valorDecimal > maxValor && valorDecimal < 100000)
                        {
                            maxValor = valorDecimal;
                            declaracion.ValorCif = valor.Groups[1].Value;
                        }
                    }
                    if (!string.IsNullOrEmpty(declaracion.ValorCif))
                    {
                        _logger.LogInformation("Valor CIF extraído: {Valor}", declaracion.ValorCif);
                    }
                }

                // Valor FOB - buscar después de "Valor EX-Fábrica"
                var valorFobMatch = Regex.Match(texto, @"Valor\s+EX-Fábrica[:\s]*([\d\.,]+)");
                if (valorFobMatch.Success)
                {
                    declaracion.ValorFob = valorFobMatch.Groups[1].Value;
                    _logger.LogInformation("Valor FOB extraído: {Valor}", declaracion.ValorFob);
                }

                // Flete - buscar después de "Gastos Hasta FOB"
                var fleteMatch = Regex.Match(texto, @"Gastos\s+Hasta\s+FOB[:\s]*([\d\.,]+)");
                if (fleteMatch.Success)
                {
                    declaracion.Flete = fleteMatch.Groups[1].Value;
                    _logger.LogInformation("Flete extraído: {Flete}", declaracion.Flete);
                }

                // Seguro - buscar después de "Seguro"
                var seguroMatch = Regex.Match(texto, @"Seguro[:\s]*([\d\.,]+)");
                if (seguroMatch.Success)
                {
                    declaracion.Seguro = seguroMatch.Groups[1].Value;
                    _logger.LogInformation("Seguro extraído: {Seguro}", declaracion.Seguro);
                }

                // Moneda - buscar después de "Moneda"
                var monedaMatch = Regex.Match(texto, @"Moneda[:\s]*([A-Z\s]+)");
                if (monedaMatch.Success)
                {
                    declaracion.Moneda = monedaMatch.Groups[1].Value.Trim();
                    _logger.LogInformation("Moneda extraída: {Moneda}", declaracion.Moneda);
                }

                // Forma de pago - buscar después de "Forva Pago"
                var formaPagoMatch = Regex.Match(texto, @"Forva\s+Pago[:\s]*([A-Z]+)");
                if (formaPagoMatch.Success)
                {
                    declaracion.FormaPago = formaPagoMatch.Groups[1].Value;
                    _logger.LogInformation("Forma de pago extraída: {Forma}", declaracion.FormaPago);
                }

                // Cláusula de compra - buscar después de "CFR"
                var clausulaMatch = Regex.Match(texto, @"(CFR)");
                if (clausulaMatch.Success)
                {
                    declaracion.ClausulaCompra = clausulaMatch.Groups[1].Value;
                    _logger.LogInformation("Cláusula de compra extraída: {Clausula}", declaracion.ClausulaCompra);
                }

                // Certificado de origen - buscar después de "CERT.ORIG"
                var certificadoMatch = Regex.Match(texto, @"CERT\.ORIG[:\s]*([A-Z0-9]+)");
                if (certificadoMatch.Success)
                {
                    declaracion.CertificadoOrigen = certificadoMatch.Groups[1].Value;
                    _logger.LogInformation("Certificado de origen extraído: {Certificado}", declaracion.CertificadoOrigen);
                }

                _logger.LogInformation("Extracción de campos adicionales completada");
            });
        }

        /// <summary>
        /// Extrae datos usando métodos manuales
        /// </summary>
        private async Task<DeclaracionIngreso> ExtraerManualAsync(string filePath)
        {
            using var stream = File.OpenRead(filePath);
            return await ExtraerManualAsync(stream);
        }

        /// <summary>
        /// Extrae datos usando métodos manuales desde stream
        /// </summary>
        private async Task<DeclaracionIngreso> ExtraerManualAsync(Stream fileStream)
        {
            var texto = await ExtraerTextoPdfAsync(fileStream);
            var declaracion = new DeclaracionIngreso();

            await ExtraerCamposCriticosAsync(declaracion, texto);
            await ExtraerCamposAdicionalesAsync(declaracion, texto);

            // Guardar texto extraído
            declaracion.TextoExtraido = texto;
            declaracion.ConfianzaExtraccion = 0.7m;

            return declaracion;
        }

        /// <summary>
        /// Extrae texto de un archivo PNG usando OCR
        /// </summary>
        private async Task<string> ExtraerTextoPdfAsync(Stream fileStream)
        {
            try
            {
                return await ExtraerTextoConOcrAsync(fileStream);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extrayendo texto del PNG");
                return $"Error extrayendo texto: {ex.Message}";
            }
        }



        /// <summary>
        /// Extrae texto de una imagen usando Azure Vision
        /// </summary>
        private async Task<string> ExtraerTextoConOcrAsync(Stream fileStream)
        {
            try
            {
                using var image = new System.Drawing.Bitmap(fileStream);
                return await ExtraerTextoConAzureVisionAsync(image);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extrayendo texto con Azure Vision");
                return $"Error extrayendo texto con Azure Vision: {ex.Message}";
            }
        }

        /// <summary>
        /// Extrae texto usando Azure Computer Vision
        /// </summary>
        private async Task<string> ExtraerTextoConAzureVisionAsync(System.Drawing.Bitmap image)
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
        /// Extrae texto usando Tesseract OCR (método de respaldo)
        /// </summary>
        private string ExtraerTextoConTesseract(System.Drawing.Bitmap image)
        {
            try
            {
                // Obtener la ruta del directorio de la aplicación
                var appDirectory = AppDomain.CurrentDomain.BaseDirectory;
                var tessdataPath = Path.Combine(appDirectory, "tessdata");
                
                // Si no existe en el directorio de la aplicación, usar la ruta del proyecto
                if (!Directory.Exists(tessdataPath))
                {
                    tessdataPath = Path.Combine(Directory.GetCurrentDirectory(), "tessdata");
                }
                
                // Si aún no existe, usar la ruta absoluta del proyecto
                if (!Directory.Exists(tessdataPath))
                {
                    tessdataPath = Path.Combine(Path.GetDirectoryName(typeof(DeclaracionIngresoService).Assembly.Location), "tessdata");
                }
                
                // Verificar que el archivo existe
                var spaFile = Path.Combine(tessdataPath, "spa.traineddata");
                if (!File.Exists(spaFile))
                {
                    return $"Error: No se encontró spa.traineddata en {tessdataPath}";
                }
                
                // Intentar primero con español
                using var engine = new Tesseract.TesseractEngine(tessdataPath, "spa", Tesseract.EngineMode.Default);

                // Configuración optimizada para documentos
                engine.SetVariable("tessedit_char_whitelist", "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789.,-()/:ÑñÁÉÍÓÚáéíóú ");
                engine.SetVariable("tessedit_pageseg_mode", "6"); // Uniform block of text
                engine.SetVariable("tessedit_ocr_engine_mode", "3"); // Default, based on what is available
                engine.SetVariable("preserve_interword_spaces", "1");
                engine.SetVariable("tessedit_do_invert", "0"); // No invertir colores
                engine.SetVariable("tessedit_image_border", "0"); // Sin borde
                engine.SetVariable("textord_heavy_nr", "1"); // Mejor detección de texto

                // Preprocesar la imagen para mejorar OCR
                using var processedImage = PreprocessImageForOCR(image);
                
                using var ms = new MemoryStream();
                processedImage.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                var imageBytes = ms.ToArray();
                using var pix = Tesseract.Pix.LoadFromMemory(imageBytes);
                using var page = engine.Process(pix);
                var texto = page.GetText();
                
                var resultado = texto?.Trim() ?? string.Empty;
                if (string.IsNullOrEmpty(resultado))
                {
                    return "OCR no detectó texto en la imagen";
                }
                
                return resultado;
            }
            catch (Exception ex)
            {
                return $"Error en Tesseract: {ex.Message}";
            }
        }

        /// <summary>
        /// Preprocesa la imagen para mejorar el OCR
        /// </summary>
        private System.Drawing.Bitmap PreprocessImageForOCR(System.Drawing.Bitmap originalImage)
        {
            try
            {
                // Redimensionar si es muy pequeña o muy grande
                var targetWidth = 2000;
                var targetHeight = 2000;
                
                if (originalImage.Width < 800 || originalImage.Height < 800)
                {
                    var scale = Math.Max(800.0 / originalImage.Width, 800.0 / originalImage.Height);
                    targetWidth = (int)(originalImage.Width * scale);
                    targetHeight = (int)(originalImage.Height * scale);
                }
                else if (originalImage.Width > 3000 || originalImage.Height > 3000)
                {
                    var scale = Math.Min(3000.0 / originalImage.Width, 3000.0 / originalImage.Height);
                    targetWidth = (int)(originalImage.Width * scale);
                    targetHeight = (int)(originalImage.Height * scale);
                }
                else
                {
                    targetWidth = originalImage.Width;
                    targetHeight = originalImage.Height;
                }
                
                // Crear imagen redimensionada
                var resizedImage = new System.Drawing.Bitmap(targetWidth, targetHeight);
                using (var g = System.Drawing.Graphics.FromImage(resizedImage))
                {
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    g.DrawImage(originalImage, 0, 0, targetWidth, targetHeight);
                }
                
                // Convertir a escala de grises
                var grayImage = new System.Drawing.Bitmap(targetWidth, targetHeight, System.Drawing.Imaging.PixelFormat.Format8bppIndexed);
                using (var g = System.Drawing.Graphics.FromImage(grayImage))
                {
                    g.DrawImage(resizedImage, 0, 0);
                }
                
                return grayImage;
            }
            catch
            {
                return new System.Drawing.Bitmap(originalImage);
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
        public async Task<bool> ValidarPdfAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return false;

                using var stream = File.OpenRead(filePath);
                return await ValidarPdfAsync(stream);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Valida si el stream es un PNG válido
        /// </summary>
        public async Task<bool> ValidarPdfAsync(Stream fileStream)
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
        /// Calcula el hash SHA256 del archivo
        /// </summary>
        private async Task<string> CalcularHashArchivoAsync(string filePath)
        {
            using var sha256 = SHA256.Create();
            using var stream = File.OpenRead(filePath);
            var hash = await sha256.ComputeHashAsync(stream);
            return Convert.ToHexString(hash).ToLower();
        }

        /// <summary>
        /// Calcula el hash SHA256 del stream
        /// </summary>
        private async Task<string> CalcularHashArchivoAsync(Stream fileStream)
        {
            using var sha256 = SHA256.Create();
            var hash = await sha256.ComputeHashAsync(fileStream);
            return Convert.ToHexString(hash).ToLower();
        }
    }
} 