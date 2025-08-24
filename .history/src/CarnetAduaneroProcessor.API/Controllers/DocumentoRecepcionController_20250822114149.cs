using CarnetAduaneroProcessor.Core.Models;
using CarnetAduaneroProcessor.Core.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using CarnetAduaneroProcessor.API.Models.Dto;

namespace CarnetAduaneroProcessor.API.Controllers
{
    /// <summary>
    /// Controlador para procesar Documentos de Recepción (DR)
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class DocumentoRecepcionController : ControllerBase
    {
        private readonly IDocumentoRecepcionService _documentoRecepcionService;
        private readonly ILogger<DocumentoRecepcionController> _logger;

        public DocumentoRecepcionController(
            IDocumentoRecepcionService documentoRecepcionService,
            ILogger<DocumentoRecepcionController> logger)
        {
            _documentoRecepcionService = documentoRecepcionService;
            _logger = logger;
        }

        /// <summary>
        /// Procesa un archivo PNG de Documento de Recepción
        /// </summary>
        /// <param name="file">Archivo PNG a procesar</param>
        /// <returns>Documento de recepción procesado con todos los campos</returns>
        [HttpPost("procesar")]
        public async Task<ActionResult<object>> ProcesarDocumentoRecepcion(IFormFile file)
        {
            try
            {
                _logger.LogInformation("Procesando Documento de Recepción desde archivo PNG: {FileName}", file?.FileName);

                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { Status = false, Message = "No se proporcionó ningún archivo" });
                }

                // Validar que sea PNG
                var contentType = file.ContentType.ToLower();
                var extension = Path.GetExtension(file.FileName).ToLower();

                bool isPngFile = contentType.Contains("png") || extension == ".png";

                if (!isPngFile)
                {
                    return BadRequest(new { Status = false, Message = "Solo se permiten archivos PNG" });
                }

                // Procesar el archivo
                using var stream = file.OpenReadStream();
                var documento = await _documentoRecepcionService.ExtraerDatosAsync(stream, file.FileName);

                // Verificar si se extrajeron campos críticos
                var camposExtraidos = !string.IsNullOrEmpty(documento.NumeroDocumento) ||
                                    !string.IsNullOrEmpty(documento.NumeroManifiesto) ||
                                    !string.IsNullOrEmpty(documento.BlArmador);

                _logger.LogInformation("Documento de Recepción procesado exitosamente: {NumeroDocumento}", documento.NumeroDocumento);

                return Ok(new
                {
                    Status = true,
                    Message = "Documento de Recepción procesado correctamente",
                    Data = documento,
                    CamposExtraidos = camposExtraidos,
                    ArchivoProcesado = file.FileName,
                    TamanioArchivo = file.Length,
                    TipoArchivo = file.ContentType
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Error de validación al procesar archivo DR: {FileName}", file?.FileName);
                return BadRequest(new { Status = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando Documento de Recepción con archivo PNG: {FileName}", file?.FileName);
                return StatusCode(500, new { Status = false, Message = "Error interno del servidor" });
            }
        }
        /// <summary>
        /// Extrae datos de un archivo PNG de Documento de Recepción
        /// </summary>
        /// <param name="file">Archivo PNG a procesar</param>
        /// <returns>Documento de recepción procesado</returns>
        [HttpPost("extraer-archivo")]
        public async Task<ActionResult<DocumentoRecepcion>> ExtraerArchivo(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest("No se proporcionó ningún archivo");
                }

                // Validar tipo de archivo
                var allowedExtensions = new[] { ".png", ".jpg", ".jpeg" };
                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                
                if (!allowedExtensions.Contains(fileExtension))
                {
                    return BadRequest("Solo se permiten archivos PNG, JPG o JPEG");
                }

                _logger.LogInformation("Procesando archivo DR: {FileName} ({Size} bytes)", 
                    file.FileName, file.Length);

                using var stream = file.OpenReadStream();
                var documento = await _documentoRecepcionService.ExtraerDatosAsync(stream, file.FileName);

                return Ok(documento);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Error de validación al procesar archivo DR");
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando archivo DR");
                return StatusCode(500, new { error = "Error interno del servidor al procesar el archivo" });
            }
        }

        /// <summary>
        /// Extrae campos críticos de un archivo PNG de Documento de Recepción
        /// </summary>
        /// <param name="file">Archivo PNG a procesar</param>
        /// <returns>Campos críticos extraídos</returns>
        [HttpPost("campos-criticos")]
        public async Task<ActionResult<object>> CamposCriticos(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest("No se proporcionó ningún archivo");
                }

                // Validar tipo de archivo
                var allowedExtensions = new[] { ".png", ".jpg", ".jpeg" };
                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                
                if (!allowedExtensions.Contains(fileExtension))
                {
                    return BadRequest("Solo se permiten archivos PNG, JPG o JPEG");
                }

                _logger.LogInformation("Extrayendo campos críticos de DR: {FileName}", file.FileName);

                using var stream = file.OpenReadStream();
                var documento = await _documentoRecepcionService.ExtraerDatosAsync(stream, file.FileName);

                // Retornar solo los campos críticos
                var camposCriticos = new
                {
                    NumeroDocumento = documento.NumeroDocumento,
                    SituacionDocumento = documento.SituacionDocumento,
                    NumeroManifiesto = documento.NumeroManifiesto,
                    FechaManifiestoSna = documento.FechaManifiestoSna,
                    FechaInicioAlmacenaje = documento.FechaInicioAlmacenaje,
                    TipoDocumento = documento.TipoDocumento,
                    BlArmador = documento.BlArmador,
                    Consignatario = documento.Consignatario,
                    RutConsignatario = documento.RutConsignatario,
                    DireccionConsignatario = documento.DireccionConsignatario,
                    NaveViaje = documento.NaveViaje,
                    LineaOperadora = documento.LineaOperadora,
                    PuertoOrigen = documento.PuertoOrigen,
                    PuertoEmbarque = documento.PuertoEmbarque,
                    PuertoDescarga = documento.PuertoDescarga,
                    PuertoTransbordo = documento.PuertoTransbordo,
                    Almacen = documento.Almacen,
                    DestinoCarga = documento.DestinoCarga,
                    Zona = documento.Zona,
                    ServicioAlmacenaje = documento.ServicioAlmacenaje,
                    GuardaAlmacen = documento.GuardaAlmacen,
                    AgenciaAduana = documento.AgenciaAduana,
                    FechaInicio90Dias = documento.FechaInicio90Dias,
                    FechaTermino90Dias = documento.FechaTermino90Dias,
                    RutEmisor = documento.RutEmisor,
                    FechaEmision = documento.FechaEmision,
                    MedioEmision = documento.MedioEmision,
                    Forwarder = documento.Forwarder,
                    EsValido = documento.EsValido,
                    ConfianzaExtraccion = documento.ConfianzaExtraccion,
                    Comentarios = documento.Comentarios
                };

                return Ok(camposCriticos);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Error de validación al extraer campos críticos de DR");
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extrayendo campos críticos de DR");
                return StatusCode(500, new { error = "Error interno del servidor al extraer campos críticos" });
            }
        }

        /// <summary>
        /// Valida un archivo PNG de Documento de Recepción
        /// </summary>
        /// <param name="file">Archivo PNG a validar</param>
        /// <returns>Resultado de la validación</returns>
        [HttpPost("validar")]
        public async Task<ActionResult<object>> ValidarArchivo(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest("No se proporcionó ningún archivo");
                }

                // Validar tipo de archivo
                var allowedExtensions = new[] { ".png", ".jpg", ".jpeg" };
                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                
                if (!allowedExtensions.Contains(fileExtension))
                {
                    return BadRequest("Solo se permiten archivos PNG, JPG o JPEG");
                }

                _logger.LogInformation("Validando archivo DR: {FileName}", file.FileName);

                using var stream = file.OpenReadStream();
                var esValido = await _documentoRecepcionService.ValidarPngAsync(stream);

                var resultado = new
                {
                    EsValido = esValido,
                    NombreArchivo = file.FileName,
                    TamanioArchivo = file.Length,
                    TipoArchivo = file.ContentType,
                    Mensaje = esValido ? "Archivo válido" : "Archivo no válido"
                };

                return Ok(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validando archivo DR");
                return StatusCode(500, new { error = "Error interno del servidor al validar el archivo" });
            }
        }

        /// <summary>
        /// Procesa texto OCR para extraer datos de Documento de Recepción
        /// </summary>
        /// <param name="request">Solicitud con texto OCR</param>
        /// <returns>Documento de recepción procesado</returns>
        [HttpPost("procesar-texto")]
        public async Task<ActionResult<DocumentoRecepcion>> ProcesarTexto([FromBody] ProcesarTextoRequestDto request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.TextoOcr))
                {
                    return BadRequest("El texto OCR es requerido");
                }

                _logger.LogInformation("Procesando texto OCR para DR");

                var documento = await _documentoRecepcionService.ProcesarTextoOcrAsync(request.TextoOcr);

                return Ok(documento);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando texto OCR para DR");
                return StatusCode(500, new { error = "Error interno del servidor al procesar el texto" });
            }
        }


        /// <summary>
        /// Obtiene información sobre el servicio de Documento de Recepción
        /// </summary>
        /// <returns>Información del servicio</returns>
        [HttpGet("info")]
        public ActionResult<object> GetInfo()
        {
            var info = new
            {
                Servicio = "Documento de Recepción (DR)",
                Descripcion = "Servicio para procesar Documentos de Recepción de contenedores",
                TiposArchivoSoportados = new[] { "PNG", "JPG", "JPEG" },
                Endpoints = new[]
                {
                    "POST /api/DocumentoRecepcion/extraer-archivo - Extrae todos los datos del documento",
                    "POST /api/DocumentoRecepcion/campos-criticos - Extrae solo campos críticos",
                    "POST /api/DocumentoRecepcion/validar - Valida el archivo",
                    "POST /api/DocumentoRecepcion/procesar-texto - Procesa texto OCR",
                    "GET /api/DocumentoRecepcion/info - Información del servicio"
                },
                CamposCriticos = new[]
                {
                    "NumeroDocumento",
                    "NumeroManifiesto", 
                    "Consignatario",
                    "Contenedor",
                    "NaveViaje",
                    "Peso"
                }
            };

            return Ok(info);
        }
    }
} 