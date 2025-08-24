using CarnetAduaneroProcessor.Core.Models;
using CarnetAduaneroProcessor.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Drawing;


namespace CarnetAduaneroProcessor.Infrastructure.Services
{
    /// <summary>
    /// Servicio para procesar documentos de Comprobante de Transacción
    /// </summary>
    public class ComprobanteTransaccionService : IComprobanteTransaccionService
    {
        private readonly ILogger<ComprobanteTransaccionService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IHybridOcrService _hybridOcrService;


        public ComprobanteTransaccionService(ILogger<ComprobanteTransaccionService> logger, IConfiguration configuration, IHybridOcrService hybridOcrService)
        {
            _logger = logger;
            _configuration = configuration;
            _hybridOcrService = hybridOcrService;
        }

        /// <summary>
        /// Extrae datos de un archivo PNG de documento de Comprobante de Transacción
        /// </summary>
        public async Task<ComprobanteTransaccion> ExtraerDatosAsync(string filePath)
        {
            try
            {
                _logger.LogInformation("Iniciando extracción de datos de Comprobante de Transacción desde archivo: {FilePath}", filePath);

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
                _logger.LogError(ex, "Error extrayendo datos de Comprobante de Transacción desde archivo: {FilePath}", filePath);
                throw;
            }
        }

        /// <summary>
        /// Extrae datos de un stream de archivo PNG
        /// </summary>
        public async Task<ComprobanteTransaccion> ExtraerDatosAsync(Stream fileStream, string fileName)
        {
            try
            {
                _logger.LogInformation("Iniciando extracción de datos de Comprobante de Transacción desde stream: {FileName}", fileName);

                // Calcular hash del archivo
                var hash = await CalcularHashAsync(fileStream);
                fileStream.Position = 0;

                // Extraer texto usando Tesseract
                var textoExtraido = await ExtraerTextoPngAsync(fileStream);
                _logger.LogInformation("Texto extraído de Comprobante de Transacción: {Texto}", textoExtraido?.Substring(0, Math.Min(100, textoExtraido?.Length ?? 0)));

                // Procesar el texto extraído
                var documento = await ProcesarTextoOcrAsync(textoExtraido);

                // Configurar metadatos
                documento.NombreArchivo = fileName;
                documento.HashArchivo = hash;
                documento.MetodoExtraccion = "Tesseract OCR";

                _logger.LogInformation("Extracción completada para Comprobante de Transacción: {NumeroFolio}", documento.NumeroFolio);

                return documento;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extrayendo datos de Comprobante de Transacción desde stream: {FileName}", fileName);
                throw;
            }
        }

        /// <summary>
        /// Extrae datos de un array de bytes
        /// </summary>
        public async Task<ComprobanteTransaccion> ExtraerDatosAsync(byte[] fileBytes, string fileName)
        {
            using var stream = new MemoryStream(fileBytes);
            return await ExtraerDatosAsync(stream, fileName);
        }

        /// <summary>
        /// Procesa texto OCR para extraer datos de documento de Comprobante de Transacción
        /// </summary>
        public async Task<ComprobanteTransaccion> ProcesarTextoOcrAsync(string textoOcr)
        {
            _logger.LogInformation("Iniciando procesamiento de texto OCR para documento de Comprobante de Transacción");

            var documento = new ComprobanteTransaccion();

            try
            {
                // Normalizar el texto OCR
                var textoNormalizado = NormalizarTexto(textoOcr);
                _logger.LogInformation("Texto normalizado: {Texto}", textoNormalizado);

                // Extraer campos críticos
                await ExtraerCamposCriticosAsync(documento, textoNormalizado);

                // Extraer campos adicionales
                await ExtraerCamposAdicionalesAsync(documento, textoNormalizado);

                // Limpiar valores extraídos para corregir errores de OCR
                LimpiarValoresExtraidos(documento);

                // Guardar texto extraído
                documento.TextoExtraido = textoNormalizado;
                documento.ConfianzaExtraccion = 0.8m;

                // Validar documento
                documento.EsValido = ValidarDocumento(documento);

                if (!documento.EsValido)
                {
                    documento.Comentarios = "No se pudieron extraer todos los campos requeridos del documento de Comprobante de Transacción";
                    _logger.LogWarning("Extracción incompleta: {Error}", documento.Comentarios);
                }
                else
                {
                    _logger.LogInformation("Procesamiento completado exitosamente para Comprobante de Transacción: {NumeroFolio}", documento.NumeroFolio);
                }

                return documento;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando texto OCR para Comprobante de Transacción");
                documento.Comentarios = $"Error procesando texto: {ex.Message}";
                return documento;
            }
        }

        /// <summary>
        /// Extrae campos críticos del documento de Comprobante de Transacción
        /// </summary>
        private async Task ExtraerCamposCriticosAsync(ComprobanteTransaccion documento, string texto)
        {
            _logger.LogInformation("Extrayendo campos críticos de Comprobante de Transacción");

            // Extraer número de folio (formato: 4560010758)
            var matchFolio = Regex.Match(texto, @"Fo1io\s+(\d+)", RegexOptions.IgnoreCase);
            if (matchFolio.Success)
            {
                documento.NumeroFolio = matchFolio.Groups[1].Value;
                _logger.LogInformation("Número de folio extraído: {Folio}", documento.NumeroFolio);
            }
            else
            {
                // Fallback: buscar "Folio" (tolerante a errores de OCR)
                var matchFolioFallback = Regex.Match(texto, @"Fo1?io\s+(\d+)", RegexOptions.IgnoreCase);
                if (matchFolioFallback.Success)
                {
                    documento.NumeroFolio = matchFolioFallback.Groups[1].Value;
                    _logger.LogInformation("Número de folio extraído (fallback): {Folio}", documento.NumeroFolio);
                }
                else
                {
                    // Último fallback: buscar después de "Folio" hasta el siguiente campo
                    var matchFolioUltimo = Regex.Match(texto, @"Fo1?io\s+([\d]+?)(?=\s+[A-Z]|$)", RegexOptions.IgnoreCase);
                    if (matchFolioUltimo.Success)
                    {
                        documento.NumeroFolio = matchFolioUltimo.Groups[1].Value;
                        _logger.LogInformation("Número de folio extraído (último fallback): {Folio}", documento.NumeroFolio);
                    }
                }
            }

            // Extraer total pagado (formato: 8.153.962)
            var matchTotal = Regex.Match(texto, @"Tota1 Pa9ado\s+([\d\.,]+)", RegexOptions.IgnoreCase);
            if (matchTotal.Success)
            {
                var totalStr = matchTotal.Groups[1].Value.Replace(".", "").Replace(",", ".");
                if (decimal.TryParse(totalStr, out var total))
                {
                    documento.TotalPagado = total;
                    _logger.LogInformation("Total pagado extraído: {Total}", documento.TotalPagado);
                }
            }
            else
            {
                // Fallback: buscar "Total Pagado" (tolerante a errores de OCR)
                var matchTotalFallback = Regex.Match(texto, @"Tota1? Pa9?ado\s+([\d\.,]+)", RegexOptions.IgnoreCase);
                if (matchTotalFallback.Success)
                {
                    var totalStr = matchTotalFallback.Groups[1].Value.Replace(".", "").Replace(",", ".");
                    if (decimal.TryParse(totalStr, out var total))
                    {
                        documento.TotalPagado = total;
                        _logger.LogInformation("Total pagado extraído (fallback): {Total}", documento.TotalPagado);
                    }
                }
                else
                {
                    // Último fallback: buscar después de "Total Pagado" hasta el siguiente campo
                    var matchTotalUltimo = Regex.Match(texto, @"Tota1? Pa9?ado\s+([\d\.,]+?)(?=\s+[A-Z]|$)", RegexOptions.IgnoreCase);
                    if (matchTotalUltimo.Success)
                    {
                        var totalStr = matchTotalUltimo.Groups[1].Value.Replace(".", "").Replace(",", ".");
                        if (decimal.TryParse(totalStr, out var total))
                        {
                            documento.TotalPagado = total;
                            _logger.LogInformation("Total pagado extraído (último fallback): {Total}", documento.TotalPagado);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Extrae campos adicionales del documento de Comprobante de Transacción
        /// </summary>
        private async Task ExtraerCamposAdicionalesAsync(ComprobanteTransaccion documento, string texto)
        {
            _logger.LogInformation("Extrayendo campos adicionales de Comprobante de Transacción");

            // Extraer RUT (formato: 77591058-5)
            var matchRut = Regex.Match(texto, @"RUT\s+(\d{8}-\d)", RegexOptions.IgnoreCase);
            if (matchRut.Success)
            {
                documento.Rut = matchRut.Groups[1].Value;
                _logger.LogInformation("RUT extraído: {Rut}", documento.Rut);
            }
            else
            {
                // Fallback: buscar "Rut - Ro1" (tolerante a errores de OCR)
                var matchRutFallback = Regex.Match(texto, @"Rut\s*-\s*Ro1?\s+(\d{8}-\d)", RegexOptions.IgnoreCase);
                if (matchRutFallback.Success)
                {
                    documento.Rut = matchRutFallback.Groups[1].Value;
                    _logger.LogInformation("RUT extraído (fallback): {Rut}", documento.Rut);
                }
                else
                {
                    // Fallback adicional: buscar cualquier RUT con formato XX.XXX.XXX-X
                    var matchRutFormato = Regex.Match(texto, @"(\d{1,2}\.\d{3}\.\d{3}-\d)", RegexOptions.IgnoreCase);
                    if (matchRutFormato.Success)
                    {
                        documento.Rut = matchRutFormato.Groups[1].Value;
                        _logger.LogInformation("RUT extraído (formato): {Rut}", documento.Rut);
                    }
                    else
                    {
                        // Último fallback: buscar cualquier secuencia de 8 dígitos seguida de guion y dígito
                        var matchRutUltimo = Regex.Match(texto, @"(\d{8}-\d)", RegexOptions.IgnoreCase);
                        if (matchRutUltimo.Success)
                        {
                            documento.Rut = matchRutUltimo.Groups[1].Value;
                            _logger.LogInformation("RUT extraído (último fallback): {Rut}", documento.Rut);
                        }
                    }
                }
            }

            // Extraer formulario (formato: 15)
            var matchFormulario = Regex.Match(texto, @"Formu1ario\s+(\d+)", RegexOptions.IgnoreCase);
            if (matchFormulario.Success)
            {
                documento.Formulario = matchFormulario.Groups[1].Value;
                _logger.LogInformation("Formulario extraído: {Formulario}", documento.Formulario);
            }
            else
            {
                // Fallback: buscar "Formulario" (tolerante a errores de OCR)
                var matchFormularioFallback = Regex.Match(texto, @"Formu1?ario\s+(\d+)", RegexOptions.IgnoreCase);
                if (matchFormularioFallback.Success)
                {
                    documento.Formulario = matchFormularioFallback.Groups[1].Value;
                    _logger.LogInformation("Formulario extraído (fallback): {Formulario}", documento.Formulario);
                }
                else
                {
                    // Último fallback: buscar después de "Formulario" hasta el siguiente campo
                    var matchFormularioUltimo = Regex.Match(texto, @"Formu1?ario\s+([\d]+?)(?=\s+[A-Z]|$)", RegexOptions.IgnoreCase);
                    if (matchFormularioUltimo.Success)
                    {
                        documento.Formulario = matchFormularioUltimo.Groups[1].Value;
                        _logger.LogInformation("Formulario extraído (último fallback): {Formulario}", documento.Formulario);
                    }
                }
            }

            // Extraer fecha de vencimiento (formato: 09-07-2025)
            var matchVencimiento = Regex.Match(texto, @"Vencimiento\s+(\d{2}-\d{2}-\d{4})", RegexOptions.IgnoreCase);
            if (matchVencimiento.Success && DateTime.TryParse(matchVencimiento.Groups[1].Value, out var vencimiento))
            {
                documento.FechaVencimiento = vencimiento;
                _logger.LogInformation("Fecha de vencimiento extraída: {Vencimiento}", documento.FechaVencimiento);
            }
            else
            {
                // Fallback: buscar después de "Vencimiento" hasta el siguiente campo
                var matchVencimientoFallback = Regex.Match(texto, @"Vencimiento\s+(\d{2}-\d{2}-\d{4})(?=\s+[A-Z]|$)", RegexOptions.IgnoreCase);
                if (matchVencimientoFallback.Success && DateTime.TryParse(matchVencimientoFallback.Groups[1].Value, out var vencimientoFallback))
                {
                    documento.FechaVencimiento = vencimientoFallback;
                    _logger.LogInformation("Fecha de vencimiento extraída (fallback): {Vencimiento}", documento.FechaVencimiento);
                }
            }

            // Extraer moneda de pago (formato: CLP)
            var matchMoneda = Regex.Match(texto, @"Moneda de Pa9o\s+([A-Z]+)", RegexOptions.IgnoreCase);
            if (matchMoneda.Success)
            {
                documento.MonedaPago = matchMoneda.Groups[1].Value;
                _logger.LogInformation("Moneda de pago extraída: {Moneda}", documento.MonedaPago);
            }
            else
            {
                // Fallback: buscar "Moneda de Pago" (tolerante a errores de OCR)
                var matchMonedaFallback = Regex.Match(texto, @"Moneda de Pa9?o\s+([A-Z]+)", RegexOptions.IgnoreCase);
                if (matchMonedaFallback.Success)
                {
                    documento.MonedaPago = matchMonedaFallback.Groups[1].Value;
                    _logger.LogInformation("Moneda de pago extraída (fallback): {Moneda}", documento.MonedaPago);
                }
                else
                {
                    // Último fallback: buscar después de "Moneda de Pago" hasta el siguiente campo
                    var matchMonedaUltimo = Regex.Match(texto, @"Moneda de Pa9?o\s+([A-Z]+?)(?=\s+[A-Z]|$)", RegexOptions.IgnoreCase);
                    if (matchMonedaUltimo.Success)
                    {
                        documento.MonedaPago = matchMonedaUltimo.Groups[1].Value;
                        _logger.LogInformation("Moneda de pago extraída (último fallback): {Moneda}", documento.MonedaPago);
                    }
                }
            }

            // Extraer fecha de pago (formato: 24-06-2025 17:44:12)
            var matchFechaPago = Regex.Match(texto, @"Fecha Pa9o\s+(\d{2}-\d{2}-\d{4}\s+\d{2}:\d{2}:\d{2})", RegexOptions.IgnoreCase);
            if (matchFechaPago.Success && DateTime.TryParse(matchFechaPago.Groups[1].Value, out var fechaPago))
            {
                documento.FechaPago = fechaPago;
                _logger.LogInformation("Fecha de pago extraída: {FechaPago}", documento.FechaPago);
            }
            else
            {
                // Fallback: buscar "Fecha Pago" (tolerante a errores de OCR)
                var matchFechaPagoFallback = Regex.Match(texto, @"Fecha Pa9?o\s+(\d{2}-\d{2}-\d{4}\s+\d{2}:\d{2}:\d{2})", RegexOptions.IgnoreCase);
                if (matchFechaPagoFallback.Success && DateTime.TryParse(matchFechaPagoFallback.Groups[1].Value, out var fechaPagoFallback))
                {
                    documento.FechaPago = fechaPagoFallback;
                    _logger.LogInformation("Fecha de pago extraída (fallback): {FechaPago}", documento.FechaPago);
                }
                else
                {
                    // Último fallback: buscar después de "Fecha Pago" hasta el siguiente campo
                    var matchFechaPagoUltimo = Regex.Match(texto, @"Fecha Pa9?o\s+(\d{2}-\d{2}-\d{4}\s+\d{2}:\d{2}:\d{2})(?=\s+[A-Z]|$)", RegexOptions.IgnoreCase);
                    if (matchFechaPagoUltimo.Success && DateTime.TryParse(matchFechaPagoUltimo.Groups[1].Value, out var fechaPagoUltimo))
                    {
                        documento.FechaPago = fechaPagoUltimo;
                        _logger.LogInformation("Fecha de pago extraída (último fallback): {FechaPago}", documento.FechaPago);
                    }
                }
            }

            // Extraer institución recaudadora
            var matchInstitucion = Regex.Match(texto, @"1nstitución Recaudadora\s+([A-Z\s]+)", RegexOptions.IgnoreCase);
            if (matchInstitucion.Success)
            {
                documento.InstitucionRecaudadora = matchInstitucion.Groups[1].Value.Trim();
                _logger.LogInformation("Institución recaudadora extraída: {Institucion}", documento.InstitucionRecaudadora);
            }
            else
            {
                // Fallback: buscar "Institución Recaudadora" (tolerante a errores de OCR)
                var matchInstitucionFallback = Regex.Match(texto, @"1?nstitución Recaudadora\s+([A-Z\s]+)", RegexOptions.IgnoreCase);
                if (matchInstitucionFallback.Success)
                {
                    documento.InstitucionRecaudadora = matchInstitucionFallback.Groups[1].Value.Trim();
                    _logger.LogInformation("Institución recaudadora extraída (fallback): {Institucion}", documento.InstitucionRecaudadora);
                }
                else
                {
                    // Último fallback: buscar después de "Institución Recaudadora" hasta "Identificador"
                    var matchInstitucionUltimo = Regex.Match(texto, @"1?nstitución Recaudadora\s+([A-Z\s]+?)(?:\s+1?dentificador|$)", RegexOptions.IgnoreCase);
                    if (matchInstitucionUltimo.Success)
                    {
                        documento.InstitucionRecaudadora = matchInstitucionUltimo.Groups[1].Value.Trim();
                        _logger.LogInformation("Institución recaudadora extraída (último fallback): {Institucion}", documento.InstitucionRecaudadora);
                    }
                }
            }

            // Extraer identificador de transacción (formato: 02847341-57208059)
            var matchIdentificador = Regex.Match(texto, @"1dentificador de Transacción\s+(\d+-\d+)", RegexOptions.IgnoreCase);
            if (matchIdentificador.Success)
            {
                documento.IdentificadorTransaccion = matchIdentificador.Groups[1].Value;
                _logger.LogInformation("Identificador de transacción extraído: {Identificador}", documento.IdentificadorTransaccion);
            }
            else
            {
                // Fallback: buscar "Identificador de Transacción" (tolerante a errores de OCR)
                var matchIdentificadorFallback = Regex.Match(texto, @"1?dentificador de Transacción\s+(\d+-\d+)", RegexOptions.IgnoreCase);
                if (matchIdentificadorFallback.Success)
                {
                    documento.IdentificadorTransaccion = matchIdentificadorFallback.Groups[1].Value;
                    _logger.LogInformation("Identificador de transacción extraído (fallback): {Identificador}", documento.IdentificadorTransaccion);
                }
                else
                {
                    // Último fallback: buscar después de "Identificador de Transacción" hasta el final de línea
                    var matchIdentificadorUltimo = Regex.Match(texto, @"1?dentificador de Transacción\s+([A-Z0-9\s\-]+?)(?:\s+No vá1ido|$)", RegexOptions.IgnoreCase);
                    if (matchIdentificadorUltimo.Success)
                    {
                        documento.IdentificadorTransaccion = matchIdentificadorUltimo.Groups[1].Value.Trim();
                        _logger.LogInformation("Identificador de transacción extraído (último fallback): {Identificador}", documento.IdentificadorTransaccion);
                    }
                }
            }

            // Extraer código de barras (número largo al final del documento)
            var matchCodigoBarras = Regex.Match(texto, @"(\d{26})", RegexOptions.IgnoreCase);
            if (matchCodigoBarras.Success)
            {
                documento.CodigoBarras = matchCodigoBarras.Groups[1].Value;
                _logger.LogInformation("Código de barras extraído: {CodigoBarras}", documento.CodigoBarras);
            }
            else
            {
                // Fallback: buscar después de "No válido para pago en Instituciones Recaudadoras"
                var matchCodigoBarrasFallback = Regex.Match(texto, @"No válido para pago en Instituciones Recaudadoras\s+(\d{26})", RegexOptions.IgnoreCase);
                if (matchCodigoBarrasFallback.Success)
                {
                    documento.CodigoBarras = matchCodigoBarrasFallback.Groups[1].Value;
                    _logger.LogInformation("Código de barras extraído (fallback): {CodigoBarras}", documento.CodigoBarras);
                }
                else
                {
                    // Fallback adicional: buscar cualquier número de 26 dígitos
                    var matchCodigoBarrasGeneral = Regex.Match(texto, @"(\d{26})", RegexOptions.IgnoreCase);
                    if (matchCodigoBarrasGeneral.Success)
                    {
                        documento.CodigoBarras = matchCodigoBarrasGeneral.Groups[1].Value;
                        _logger.LogInformation("Código de barras extraído (general): {CodigoBarras}", documento.CodigoBarras);
                    }
                }
            }

            // Extraer número de referencia (formato: 06240508201625063001504715)
            if (string.IsNullOrEmpty(documento.CodigoBarras))
            {
                // Si no se extrajo código de barras, usar el número de referencia
                var matchReferencia = Regex.Match(texto, @"(\d{26})", RegexOptions.IgnoreCase);
                if (matchReferencia.Success)
                {
                    documento.NumeroReferencia = matchReferencia.Groups[1].Value;
                    _logger.LogInformation("Número de referencia extraído: {Referencia}", documento.NumeroReferencia);
                }
            }
            else
            {
                // Si ya se extrajo código de barras, usar el mismo valor para referencia
                documento.NumeroReferencia = documento.CodigoBarras;
                _logger.LogInformation("Número de referencia establecido igual al código de barras: {Referencia}", documento.NumeroReferencia);
            }

            // Patrones adicionales específicos para el texto OCR
            // Buscar RUT con formato específico "77.965.620-9"
            if (string.IsNullOrEmpty(documento.Rut))
            {
                var matchRutEspecifico = Regex.Match(texto, @"RUT\s+(\d{2}\.\d{3}\.\d{3}-\d)", RegexOptions.IgnoreCase);
                if (matchRutEspecifico.Success)
                {
                    documento.Rut = matchRutEspecifico.Groups[1].Value;
                    _logger.LogInformation("RUT extraído (específico): {Rut}", documento.Rut);
                }
            }

            // Buscar identificador de transacción con formato específico "80084201 - 30252371"
            if (string.IsNullOrEmpty(documento.IdentificadorTransaccion))
            {
                var matchIdentificadorEspecifico = Regex.Match(texto, @"Identificador de Transacción\s+(\d+\s*-\s*\d+)", RegexOptions.IgnoreCase);
                if (matchIdentificadorEspecifico.Success)
                {
                    documento.IdentificadorTransaccion = matchIdentificadorEspecifico.Groups[1].Value.Trim();
                    _logger.LogInformation("Identificador de transacción extraído (específico): {Identificador}", documento.IdentificadorTransaccion);
                }
            }

            // Buscar código de barras con formato específico "0619050668962506300150201K"
            if (string.IsNullOrEmpty(documento.CodigoBarras))
            {
                var matchCodigoBarrasEspecifico = Regex.Match(texto, @"(\d{26}[A-Z])", RegexOptions.IgnoreCase);
                if (matchCodigoBarrasEspecifico.Success)
                {
                    documento.CodigoBarras = matchCodigoBarrasEspecifico.Groups[1].Value;
                    _logger.LogInformation("Código de barras extraído (específico): {CodigoBarras}", documento.CodigoBarras);
                }
            }

            // Buscar institución recaudadora específica "BANCO INTERNACIONAL"
            if (string.IsNullOrEmpty(documento.InstitucionRecaudadora))
            {
                var matchInstitucionEspecifica = Regex.Match(texto, @"Institución Recaudadora\s+([A-Z\s]+?)(?:\s+Identificador|$)", RegexOptions.IgnoreCase);
                if (matchInstitucionEspecifica.Success)
                {
                    documento.InstitucionRecaudadora = matchInstitucionEspecifica.Groups[1].Value.Trim();
                    _logger.LogInformation("Institución recaudadora extraída (específica): {Institucion}", documento.InstitucionRecaudadora);
                }
            }
        }

        /// <summary>
        /// Valida si el documento tiene todos los campos requeridos
        /// </summary>
        private bool ValidarDocumento(ComprobanteTransaccion documento)
        {
            // Verificar que se haya extraído el número de folio
            if (string.IsNullOrWhiteSpace(documento.NumeroFolio))
                return false;

            // Verificar que se haya extraído el total pagado
            if (documento.TotalPagado <= 0)
                return false;

            // Verificar que se haya extraído el RUT
            if (string.IsNullOrWhiteSpace(documento.Rut))
                return false;

            // Verificar que se haya extraído el formulario
            if (string.IsNullOrWhiteSpace(documento.Formulario))
                return false;

            // Verificar que se haya extraído la fecha de vencimiento
            if (documento.FechaVencimiento == null)
                return false;

            // Verificar que se haya extraído la moneda de pago
            if (string.IsNullOrWhiteSpace(documento.MonedaPago))
                return false;

            // Verificar que se haya extraído la fecha de pago
            if (documento.FechaPago == null)
                return false;

            // Verificar que se haya extraído la institución recaudadora
            if (string.IsNullOrWhiteSpace(documento.InstitucionRecaudadora))
                return false;

            // Verificar que se haya extraído el identificador de transacción
            if (string.IsNullOrWhiteSpace(documento.IdentificadorTransaccion))
                return false;

            // Verificar que se haya extraído el código de barras
            if (string.IsNullOrWhiteSpace(documento.CodigoBarras))
                return false;

            // Verificar que se haya extraído el número de referencia
            if (string.IsNullOrWhiteSpace(documento.NumeroReferencia))
                return false;

            return true;
        }

        /// <summary>
        /// Limpia los valores extraídos para corregir errores de OCR
        /// </summary>
        private void LimpiarValoresExtraidos(ComprobanteTransaccion documento)
        {
            // Limpiar RUT
            if (!string.IsNullOrEmpty(documento.Rut))
            {
                documento.Rut = LimpiarRut(documento.Rut);
            }

            // Limpiar institución recaudadora
            if (!string.IsNullOrEmpty(documento.InstitucionRecaudadora))
            {
                documento.InstitucionRecaudadora = LimpiarInstitucion(documento.InstitucionRecaudadora);
            }

            // Limpiar identificador de transacción
            if (!string.IsNullOrEmpty(documento.IdentificadorTransaccion))
            {
                documento.IdentificadorTransaccion = LimpiarIdentificador(documento.IdentificadorTransaccion);
            }
        }

        /// <summary>
        /// Limpia el RUT de errores de OCR
        /// </summary>
        private string LimpiarRut(string rut)
        {
            if (string.IsNullOrEmpty(rut))
                return rut;

            // Corregir errores comunes de OCR en RUT
            return rut
                .Replace("O", "0") // O por 0
                .Replace("l", "1") // l por 1
                .Replace("I", "1") // I por 1
                .Replace("S", "5") // S por 5
                .Replace("B", "8"); // B por 8
        }

        /// <summary>
        /// Limpia la institución recaudadora de errores de OCR
        /// </summary>
        private string LimpiarInstitucion(string institucion)
        {
            if (string.IsNullOrEmpty(institucion))
                return institucion;

            // Corregir errores comunes de OCR en nombres de instituciones
            return institucion
                .Replace("1TAU", "ITAU") // 1 por I
                .Replace("8ANCO", "BANCO") // 8 por B
                .Replace("0", "O") // 0 por O
                .Replace("1", "I") // 1 por I
                .Replace("5", "S"); // 5 por S
        }

        /// <summary>
        /// Limpia el identificador de transacción de errores de OCR
        /// </summary>
        private string LimpiarIdentificador(string identificador)
        {
            if (string.IsNullOrEmpty(identificador))
                return identificador;

            // Corregir errores comunes de OCR en identificadores
            return identificador
                .Replace("O", "0") // O por 0
                .Replace("l", "1") // l por 1
                .Replace("I", "1") // I por 1
                .Replace("S", "5") // S por 5
                .Replace("B", "8"); // B por 8
        }

        /// <summary>
        /// Extrae texto de un archivo PNG usando Tesseract
        /// </summary>
        private async Task<string> ExtraerTextoPngAsync(Stream fileStream)
        {
            try
            {
                // Usar Tesseract para extraer texto
                return await _hybridOcrService.ExtractTextAsync(fileStream);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extrayendo texto de PNG con Tesseract");
                return string.Empty;
            }
        }



        /// <summary>
        /// Normaliza el texto OCR
        /// </summary>
        private string NormalizarTexto(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return string.Empty;

            // Limpiar solo caracteres de control y espacios, sin corromper datos
            var textoLimpio = texto
                .Replace("\r\n", " ")
                .Replace("\n", " ")
                .Replace("\r", " ")
                .Replace("\t", " ")
                .Replace("|", "I") // Solo corregir caracteres obviamente mal interpretados
                .Replace("£", "E") // Solo corregir caracteres obviamente mal interpretados
                .Replace("¿", "?") // Solo corregir caracteres obviamente mal interpretados
                .Replace("—", "-") // Solo corregir caracteres obviamente mal interpretados
                .Replace("º", "o"); // Solo corregir caracteres obviamente mal interpretados

            // Normalizar espacios múltiples
            textoLimpio = Regex.Replace(textoLimpio, @"\s+", " ");
            
            // Limpiar caracteres extraños al inicio y final
            textoLimpio = Regex.Replace(textoLimpio, @"^[^A-Za-z0-9\s]+", "");
            textoLimpio = Regex.Replace(textoLimpio, @"[^A-Za-z0-9\s]+$", "");

            return textoLimpio.Trim();
        }

        /// <summary>
        /// Calcula el hash SHA256 del archivo
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
                // Verificar que el stream tenga al menos 8 bytes (cabecera PNG)
                if (fileStream.Length < 8)
                    return false;

                var buffer = new byte[8];
                await fileStream.ReadAsync(buffer, 0, 8);
                fileStream.Position = 0;

                // Verificar la firma PNG: 89 50 4E 47 0D 0A 1A 0A
                return buffer[0] == 0x89 && buffer[1] == 0x50 && buffer[2] == 0x4E && buffer[3] == 0x47 &&
                       buffer[4] == 0x0D && buffer[5] == 0x0A && buffer[6] == 0x1A && buffer[7] == 0x0A;
            }
            catch
            {
                return false;
            }
        }
    }
} 