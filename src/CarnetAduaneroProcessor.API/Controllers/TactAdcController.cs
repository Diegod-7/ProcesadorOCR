using CarnetAduaneroProcessor.Core.Models;
using CarnetAduaneroProcessor.Core.Services;
using CarnetAduaneroProcessor.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace CarnetAduaneroProcessor.API.Controllers
{
    /// <summary>
    /// Controlador para procesar documentos TACT/ADC (Transport Air Cargo Tariff / Autorización de Despacho de Contenedores)
    /// Soporta formatos de MAERSK, Mediterranean Shipping Company (MSC) e IANTAYLOR
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class TactAdcController : ControllerBase
    {
        private readonly ITactAdcService _tactAdcService;
        private readonly ILogger<TactAdcController> _logger;

        public TactAdcController(ITactAdcService tactAdcService, ILogger<TactAdcController> logger)
        {
            _tactAdcService = tactAdcService;
            _logger = logger;
        }

        /// <summary>
        /// Procesa un archivo PNG de documento TACT/ADC (Autorización de Despacho de Contenedores)
        /// Soporta formatos de MAERSK, Mediterranean Shipping Company (MSC) e IANTAYLOR
        /// </summary>
        /// <param name="file">Archivo PNG del documento TACT/ADC</param>
        /// <returns>Datos extraídos del documento TACT/ADC</returns>
        [HttpPost("procesar")]
        [ProducesResponseType(typeof(TactAdc), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<TactAdc>> ProcesarDocumento(IFormFile file)
        {
            try
            {
                _logger.LogInformation("Iniciando procesamiento de documento TACT/ADC: {FileName}", file?.FileName);

                // Validar archivo
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { message = "No se proporcionó ningún archivo" });
                }

                // Validar tipo de archivo
                var allowedExtensions = new[] { ".png", ".jpg", ".jpeg" };
                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                
                if (!allowedExtensions.Contains(fileExtension))
                {
                    return BadRequest(new { message = "Solo se permiten archivos PNG, JPG o JPEG" });
                }

                // Validar tamaño del archivo (máximo 10MB)
                if (file.Length > 10 * 1024 * 1024)
                {
                    return BadRequest(new { message = "El archivo es demasiado grande. Máximo 10MB permitido" });
                }

                // Procesar archivo
                using var stream = file.OpenReadStream();
                var documento = await _tactAdcService.ExtraerDatosAsync(stream, file.FileName);

                _logger.LogInformation("Documento TACT/ADC procesado exitosamente: {NumeroTatc}", documento.NumeroTatc);

                return Ok(documento);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Error de validación en documento TACT/ADC: {FileName}", file?.FileName);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al procesar documento TACT/ADC: {FileName}", file?.FileName);
                return StatusCode(500, new { message = "Error interno del servidor al procesar el documento TACT/ADC" });
            }
        }

        /// <summary>
        /// Procesa texto OCR para extraer datos de documento TACT/ADC
        /// </summary>
        /// <param name="request">Solicitud con texto OCR</param>
        /// <returns>Datos extraídos del documento TACT/ADC</returns>
        [HttpPost("procesar-texto")]
        [ProducesResponseType(typeof(TactAdc), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<TactAdc>> ProcesarTexto([FromBody] ProcesamientoTextoRequest request)
        {
            try
            {
                _logger.LogInformation("Iniciando procesamiento de texto OCR para documento TACT/ADC");

                if (string.IsNullOrWhiteSpace(request.Texto))
                {
                    return BadRequest(new { message = "El texto OCR es requerido" });
                }

                var documento = await _tactAdcService.ProcesarTextoOcrAsync(request.Texto);

                _logger.LogInformation("Texto OCR procesado exitosamente para documento TACT/ADC: {NumeroTatc}", documento.NumeroTatc);

                return Ok(documento);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al procesar texto OCR para documento TACT/ADC");
                return StatusCode(500, new { message = "Error interno del servidor al procesar el texto OCR" });
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
                var esValido = await _tactAdcService.ValidarPngAsync(stream);

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
        /// Obtiene información sobre el servicio TACT/ADC
        /// </summary>
        /// <returns>Información del servicio</returns>
        [HttpGet("info")]
        [ProducesResponseType(typeof(object), 200)]
        public ActionResult<object> ObtenerInfo()
        {
            return Ok(new
            {
                servicio = "TACT/ADC Processor",
                version = "1.4.0",
                descripcion = "Servicio para procesar documentos TACT/ADC (Transport Air Cargo Tariff / Autorización de Despacho de Contenedores) - Soporta formatos MAERSK, MSC e IANTAYLOR con extracción mejorada de campos críticos",
                camposCriticos = new[]
                {
                    "NumeroTatc",
                    "NumeroContenedor", 
                    "NumeroSellos"
                },
                camposAdicionales = new[]
                {
                    "EmpresaEmisora",
                    "DireccionEmpresa",
                    "RutEmisor",
                    "FechaEmision",
                    "TipoDocumento",
                    "BlArmador",
                    "Consignatario",
                    "RutConsignatario",
                    "DireccionConsignatario",
                    "Forwarder",
                    "LineaOperadora",
                    "ServicioAlmacenaje",
                    "GuardaAlmacen",
                    "RutGuardaAlmacen",
                    "PuertoOrigen",
                    "PuertoDescarga",
                    "PuertoEmbarque",
                    "PuertoDestino",
                    "PuertoTransbordo",
                    "TipoBulto",
                    "Cantidad",
                    "Peso",
                    "Volumen",
                    "Estado"
                },
                formatosSoportados = new[] { ".png", ".jpg", ".jpeg" },
                tamanioMaximo = "10MB",
                metodoExtraccion = "Azure Computer Vision"
            });
        }
    }

} 