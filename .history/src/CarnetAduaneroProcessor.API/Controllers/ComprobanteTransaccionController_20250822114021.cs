using CarnetAduaneroProcessor.Core.Models;
using CarnetAduaneroProcessor.Core.Services;
using CarnetAduaneroProcessor.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Text.Json; // Added for JsonSerializer

namespace CarnetAduaneroProcessor.API.Controllers
{
    /// <summary>
    /// Controlador para procesar documentos de Comprobante de Transacción
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ComprobanteTransaccionController : ControllerBase
    {
        private readonly IComprobanteTransaccionService _comprobanteTransaccionService;
        private readonly ILogger<ComprobanteTransaccionController> _logger;

        public ComprobanteTransaccionController(IComprobanteTransaccionService comprobanteTransaccionService, ILogger<ComprobanteTransaccionController> logger)
        {
            _comprobanteTransaccionService = comprobanteTransaccionService;
            _logger = logger;
        }

        /// <summary>
        /// Procesa un archivo PNG de documento de Comprobante de Transacción usando IA directamente
        /// </summary>
        /// <param name="file">Archivo PNG del documento de Comprobante de Transacción</param>
        /// <returns>Datos extraídos del documento de Comprobante de Transacción usando IA</returns>
        [HttpPost("procesar")]
        [ProducesResponseType(typeof(ComprobanteTransaccion), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<ComprobanteTransaccion>> ProcesarDocumento(IFormFile file)
        {
            try
            {
                _logger.LogInformation("Iniciando procesamiento de documento de Comprobante de Transacción con IA: {FileName}", file?.FileName);

                // Validar archivo
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { message = "No se proporcionó ningún archivo" });
                }

                // Validar tipo de archivo
                var allowedExtensions = new[] { ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".tiff", ".tif", ".webp" };
                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                
                if (!allowedExtensions.Contains(fileExtension))
                {
                    return BadRequest(new { message = "Solo se permiten archivos de imagen válidos" });
                }

                // Validar tamaño del archivo (máximo 20MB para IA)
                if (file.Length > 20 * 1024 * 1024)
                {
                    return BadRequest(new { message = "El archivo es demasiado grande. Máximo 20MB permitido para IA" });
                }

                // Leer bytes de la imagen para procesamiento con IA
                using var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream);
                var imagenBytes = memoryStream.ToArray();

                // Obtener el servicio de IA
                var aiService = HttpContext.RequestServices.GetService<IAiPostProcessorService>();
                if (aiService == null)
                {
                    _logger.LogWarning("Servicio de IA no disponible, usando método tradicional");
                    // Fallback al método tradicional si IA no está disponible
                    using var stream = file.OpenReadStream();
                    var documento = await _comprobanteTransaccionService.ExtraerDatosAsync(stream, file.FileName);
                    return Ok(documento);
                }

                // Procesar imagen directamente con IA
                var jsonExtraido = await aiService.ProcesarImagenDirectamenteAsync(imagenBytes, file.FileName);

                // Convertir JSON a ComprobanteTransaccion
                try
                {
                    var documento = JsonSerializer.Deserialize<ComprobanteTransaccion>(jsonExtraido);
                    if (documento != null)
                    {
                        // Asignar campos adicionales
                        documento.NombreArchivo = file.FileName;
                        documento.MetodoExtraccion = "IA (Gemma 3: 4B)";
                        documento.FechaProcesamiento = DateTime.UtcNow;
                        
                        _logger.LogInformation("Documento de Comprobante de Transacción procesado exitosamente con IA: {NumeroFolio}", documento.NumeroFolio);
                        return Ok(documento);
                    }
                    else
                    {
                        _logger.LogWarning("IA devolvió JSON inválido, usando método tradicional");
                        // Fallback al método tradicional
                        using var stream = file.OpenReadStream();
                        var documentoFallback = await _comprobanteTransaccionService.ExtraerDatosAsync(stream, file.FileName);
                        return Ok(documentoFallback);
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Error parseando JSON de IA, usando método tradicional");
                    // Fallback al método tradicional
                    using var stream = file.OpenReadStream();
                    var documentoFallback = await _comprobanteTransaccionService.ExtraerDatosAsync(stream, file.FileName);
                    return Ok(documentoFallback);
                }
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Error de validación en documento de Comprobante de Transacción: {FileName}", file?.FileName);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al procesar documento de Comprobante de Transacción con IA: {FileName}", file?.FileName);
                return StatusCode(500, new { message = "Error interno del servidor al procesar el documento de Comprobante de Transacción con IA" });
            }
        }

        /// <summary>
        /// Procesa texto OCR para extraer datos de documento de Comprobante de Transacción
        /// </summary>
        /// <param name="request">Solicitud con texto OCR</param>
        /// <returns>Datos extraídos del documento de Comprobante de Transacción</returns>
        [HttpPost("procesar-texto")]
        [ProducesResponseType(typeof(ComprobanteTransaccion), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<ComprobanteTransaccion>> ProcesarTexto([FromBody] ProcesamientoTextoRequest request)
        {
            try
            {
                _logger.LogInformation("Iniciando procesamiento de texto OCR para documento de Comprobante de Transacción");

                if (string.IsNullOrWhiteSpace(request.Texto))
                {
                    return BadRequest(new { message = "El texto OCR es requerido" });
                }

                var documento = await _comprobanteTransaccionService.ProcesarTextoOcrAsync(request.Texto);

                _logger.LogInformation("Texto OCR procesado exitosamente para documento de Comprobante de Transacción: {NumeroFolio}", documento.NumeroFolio);

                return Ok(documento);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al procesar texto OCR para documento de Comprobante de Transacción");
                return StatusCode(500, new { message = "Error interno del servidor al procesar el texto OCR" });
            }
        }

        /// <summary>
        /// Procesa una imagen directamente con IA (Gemma 3: 4B) para extraer el JSON completo
        /// </summary>
        /// <param name="file">Archivo de imagen a procesar (PNG, JPG, JPEG, GIF, BMP, TIFF, WEBP)</param>
        /// <returns>JSON extraído directamente de la imagen por IA</returns>
        [HttpPost("procesar-imagen-ia")]
        [ProducesResponseType(typeof(string), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<string>> ProcesarImagenConIA(IFormFile file)
        {
            try
            {
                _logger.LogInformation("Iniciando procesamiento de imagen con IA: {FileName}", file?.FileName);

                // Validar archivo
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { message = "No se ha proporcionado ningún archivo" });
                }

                // Validar tipo de imagen
                var extensionesPermitidas = new[] { ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".tiff", ".tif", ".webp" };
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                
                if (!extensionesPermitidas.Contains(extension))
                {
                    return BadRequest(new { message = $"Tipo de archivo no soportado. Extensiones permitidas: {string.Join(", ", extensionesPermitidas)}" });
                }

                if (file.Length > 20 * 1024 * 1024) // 20MB máximo para imágenes
                {
                    return BadRequest(new { message = "El archivo no puede exceder 20MB" });
                }

                // Leer bytes de la imagen
                using var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream);
                var imagenBytes = memoryStream.ToArray();

                // Obtener el servicio de IA
                var aiService = HttpContext.RequestServices.GetService<IAiPostProcessorService>();
                if (aiService == null)
                {
                    return StatusCode(500, new { message = "Servicio de IA no disponible" });
                }

                // Procesar imagen directamente con IA
                var jsonExtraido = await aiService.ProcesarImagenDirectamenteAsync(imagenBytes, file.FileName);

                _logger.LogInformation("Imagen procesada exitosamente con IA: {FileName}", file.FileName);

                // Devolver el JSON extraído
                return Ok(new { 
                    mensaje = "Imagen procesada exitosamente con IA",
                    archivo = file.FileName,
                    json = jsonExtraido,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al procesar imagen con IA: {FileName}", file?.FileName);
                return StatusCode(500, new { message = "Error interno del servidor al procesar la imagen con IA" });
            }
        }

        /// <summary>
        /// Valida si un archivo es un PNG válido
        /// </summary>
        /// <param name="file">Archivo a validar</param>
        /// <returns>Resultado de la validación</returns>
        [HttpPost("validar")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<object>> ValidarArchivo(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { message = "No se proporcionó ningún archivo" });
                }

                using var stream = file.OpenReadStream();
                var esValido = await _comprobanteTransaccionService.ValidarPngAsync(stream);

                return Ok(new
                {
                    esValido = esValido,
                    nombreArchivo = file.FileName,
                    tamanioArchivo = file.Length,
                    tipoArchivo = file.ContentType
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al validar archivo: {FileName}", file?.FileName);
                return StatusCode(500, new { message = "Error interno del servidor al validar el archivo" });
            }
        }

        /// <summary>
        /// Obtiene información sobre el servicio de Comprobante de Transacción
        /// </summary>
        /// <returns>Información del servicio</returns>
        [HttpGet("info")]
        [ProducesResponseType(typeof(object), 200)]
        public ActionResult<object> ObtenerInfo()
        {
            return Ok(new
            {
                servicio = "Comprobante de Transacción Processor",
                version = "1.0.0",
                descripcion = "Servicio para procesar documentos de Comprobante de Transacción de la Tesorería General de la República",
                camposCriticos = new[]
                {
                    "NumeroFolio",
                    "TotalPagado"
                },
                camposAdicionales = new[]
                {
                    "Rut",
                    "Formulario",
                    "FechaVencimiento",
                    "MonedaPago",
                    "FechaPago",
                    "InstitucionRecaudadora",
                    "IdentificadorTransaccion",
                    "CodigoBarras",
                    "NumeroReferencia"
                },
                formatosSoportados = new[] { ".png", ".jpg", ".jpeg" },
                tamanioMaximo = "10MB",
                metodoExtraccion = "Azure Computer Vision"
            });
        }
    }

} 