using Microsoft.AspNetCore.Mvc;
using CarnetAduaneroProcessor.Core.Models;
using CarnetAduaneroProcessor.Core.Services;
using AutoMapper;
using System.Text.Json;

namespace CarnetAduaneroProcessor.API.Controllers
{
    /// <summary>
    /// Controlador para procesar documentos de Guía de Despacho Electrónica
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class GuiaDespachoController : ControllerBase
    {
        private readonly IGuiaDespachoService _guiaDespachoService;
        private readonly ILogger<GuiaDespachoController> _logger;
        private readonly IMapper _mapper;

        public GuiaDespachoController(
            IGuiaDespachoService guiaDespachoService,
            ILogger<GuiaDespachoController> logger,
            IMapper mapper)
        {
            _guiaDespachoService = guiaDespachoService;
            _logger = logger;
            _mapper = mapper;
        }

        /// <summary>
        /// Sube y procesa un archivo PNG de Guía de Despacho Electrónica
        /// </summary>
        /// <param name="file">Archivo PNG a procesar</param>
        /// <returns>Datos extraídos de la guía de despacho</returns>
        [HttpPost("procesar")]
        [ProducesResponseType(typeof(GuiaDespacho), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<GuiaDespacho>> ProcesarPng(IFormFile file)
        {
            try
            {
                _logger.LogInformation("Iniciando procesamiento de archivo PNG de Guía de Despacho: {FileName}", file?.FileName);

                // Validar archivo
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { message = "No se ha proporcionado ningún archivo" });
                }

                if (!file.ContentType.Equals("image/png", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(new { message = "El archivo debe ser un PNG" });
                }

                if (file.Length > 10 * 1024 * 1024) // 10MB máximo
                {
                    return BadRequest(new { message = "El archivo no puede exceder 10MB" });
                }

                // Guardar archivo temporalmente
                var tempPath = await GuardarArchivoTemporalAsync(file);
                
                try
                {
                    // Extraer datos del PNG
                    var guiaDespacho = await _guiaDespachoService.ExtraerDatosAsync(tempPath);

                    _logger.LogInformation("Guía de Despacho procesada exitosamente: {NumeroGuia}", guiaDespacho.NumeroGuia);

                    return CreatedAtAction(nameof(ObtenerPorId), new { id = guiaDespacho.Id }, guiaDespacho);
                }
                finally
                {
                    // Limpiar archivo temporal
                    if (System.IO.File.Exists(tempPath))
                    {
                        System.IO.File.Delete(tempPath);
                    }
                }
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Error de validación en archivo: {FileName}", file?.FileName);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al procesar archivo: {FileName}", file?.FileName);
                return StatusCode(500, new { message = "Error interno del servidor al procesar el archivo" });
            }
        }

        /// <summary>
        /// Procesa múltiples archivos PNG en lote
        /// </summary>
        /// <param name="files">Lista de archivos PNG</param>
        /// <returns>Resultados del procesamiento</returns>
        [HttpPost("procesar-lote")]
        [ProducesResponseType(typeof(List<GuiaDespacho>), 201)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<List<GuiaDespacho>>> ProcesarLote(IFormFileCollection files)
        {
            try
            {
                _logger.LogInformation("Iniciando procesamiento de lote con {Count} archivos PNG", files.Count);

                if (files == null || !files.Any())
                {
                    return BadRequest(new { message = "No se han proporcionado archivos" });
                }

                if (files.Count > 10) // Máximo 10 archivos por lote
                {
                    return BadRequest(new { message = "No se pueden procesar más de 10 archivos a la vez" });
                }

                var resultados = new List<GuiaDespacho>();
                var errores = new List<string>();

                foreach (var file in files)
                {
                    try
                    {
                        if (!file.ContentType.Equals("image/png", StringComparison.OrdinalIgnoreCase))
                        {
                            errores.Add($"Archivo {file.FileName}: No es un PNG válido");
                            continue;
                        }

                        var tempPath = await GuardarArchivoTemporalAsync(file);
                        
                        try
                        {
                            var guiaDespacho = await _guiaDespachoService.ExtraerDatosAsync(tempPath);
                            resultados.Add(guiaDespacho);
                        }
                        finally
                        {
                            if (System.IO.File.Exists(tempPath))
                            {
                                System.IO.File.Delete(tempPath);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error procesando archivo: {FileName}", file.FileName);
                        errores.Add($"Archivo {file.FileName}: {ex.Message}");
                    }
                }

                var response = new
                {
                    procesados = resultados.Count,
                    errores = errores.Count,
                    guiasDespacho = resultados,
                    mensajesError = errores
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al procesar lote de archivos PNG");
                return StatusCode(500, new { message = "Error interno del servidor al procesar el lote" });
            }
        }

        /// <summary>
        /// Obtiene una guía de despacho por ID
        /// </summary>
        /// <param name="id">ID de la guía de despacho</param>
        /// <returns>Guía de despacho encontrada</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(GuiaDespacho), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<GuiaDespacho>> ObtenerPorId(int id)
        {
            try
            {
                // Por ahora retornamos un mock, en el futuro se conectará a base de datos
                var guiaDespacho = new GuiaDespacho
                {
                    Id = id,
                    NumeroGuia = "44172",
                    RutEmisor = "13.021.175-5",
                    FechaDocumento = DateTime.Now.AddDays(-30)
                };

                if (guiaDespacho == null)
                {
                    return NotFound(new { message = $"No se encontró la guía de despacho con ID {id}" });
                }

                return Ok(guiaDespacho);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo guía de despacho con ID: {Id}", id);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Obtiene todas las guías de despacho
        /// </summary>
        /// <param name="page">Número de página</param>
        /// <param name="pageSize">Tamaño de página</param>
        /// <param name="search">Término de búsqueda</param>
        /// <returns>Lista de guías de despacho</returns>
        [HttpGet]
        [ProducesResponseType(typeof(List<GuiaDespacho>), 200)]
        public async Task<ActionResult<List<GuiaDespacho>>> ObtenerTodos(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null)
        {
            try
            {
                // Por ahora retornamos datos mock, en el futuro se conectará a base de datos
                var guiasDespacho = new List<GuiaDespacho>
                {
                    new GuiaDespacho
                    {
                        Id = 1,
                        NumeroGuia = "44172",
                        RutEmisor = "13.021.175-5",
                        FechaDocumento = DateTime.Now.AddDays(-30),
                        NombreEmisor = "ALEXIS MONTENEGRO PONCE",
                        NombreReceptor = "COMERCIAL CASA NOVEDAD LIMITADA"
                    }
                };

                return Ok(guiasDespacho);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo guías de despacho");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Procesa texto OCR para extraer datos de Guía de Despacho
        /// </summary>
        /// <param name="request">Solicitud con texto OCR</param>
        /// <returns>Datos extraídos de la guía de despacho</returns>
        [HttpPost("procesar-texto-ocr")]
        [ProducesResponseType(typeof(GuiaDespacho), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<GuiaDespacho>> ProcesarTextoOcr([FromBody] ProcesarTextoOcrGuiaDespachoRequest request)
        {
            try
            {
                _logger.LogInformation("Iniciando procesamiento de texto OCR para Guía de Despacho");

                if (string.IsNullOrWhiteSpace(request.TextoOcr))
                {
                    return BadRequest(new { message = "El texto OCR no puede estar vacío" });
                }

                var guiaDespacho = await _guiaDespachoService.ProcesarTextoOcrAsync(request.TextoOcr);

                _logger.LogInformation("Procesamiento de texto OCR completado para Guía de Despacho: {NumeroGuia}", guiaDespacho.NumeroGuia);

                return Ok(guiaDespacho);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando texto OCR para Guía de Despacho");
                return StatusCode(500, new { message = "Error interno del servidor al procesar el texto OCR" });
            }
        }

        /// <summary>
        /// Valida si un archivo es un PNG válido
        /// </summary>
        /// <param name="file">Archivo a validar</param>
        /// <returns>Resultado de la validación</returns>
        [HttpPost("validar-png")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<object>> ValidarPng(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { message = "No se ha proporcionado ningún archivo" });
                }

                var tempPath = await GuardarArchivoTemporalAsync(file);
                
                try
                {
                    var esValido = await _guiaDespachoService.ValidarPngAsync(tempPath);
                    
                    return Ok(new { 
                        esValido = esValido,
                        nombreArchivo = file.FileName,
                        tamaño = file.Length,
                        tipoContenido = file.ContentType
                    });
                }
                finally
                {
                    if (System.IO.File.Exists(tempPath))
                    {
                        System.IO.File.Delete(tempPath);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validando archivo PNG: {FileName}", file?.FileName);
                return StatusCode(500, new { message = "Error interno del servidor al validar el archivo" });
            }
        }

        /// <summary>
        /// Obtiene estadísticas de procesamiento
        /// </summary>
        /// <returns>Estadísticas del sistema</returns>
        [HttpGet("estadisticas")]
        [ProducesResponseType(typeof(object), 200)]
        public async Task<ActionResult<object>> ObtenerEstadisticas()
        {
            try
            {
                // Por ahora retornamos estadísticas mock
                var estadisticas = new
                {
                    totalGuiasDespacho = 1,
                    guiasValidas = 1,
                    guiasInvalidas = 0,
                    ultimaProcesada = DateTime.Now.AddHours(-1),
                    tasaExito = 100.0
                };

                return Ok(estadisticas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo estadísticas de Guía de Despacho");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Elimina una guía de despacho por ID
        /// </summary>
        /// <param name="id">ID de la guía de despacho</param>
        /// <returns>Resultado de la eliminación</returns>
        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Eliminar(int id)
        {
            try
            {
                // Por ahora simulamos eliminación exitosa
                _logger.LogInformation("Eliminando guía de despacho con ID: {Id}", id);
                
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error eliminando guía de despacho con ID: {Id}", id);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Solicitud para procesar texto OCR de Guía de Despacho
        /// </summary>
        public class ProcesarTextoOcrGuiaDespachoRequest
        {
            /// <summary>
            /// Texto extraído por OCR
            /// </summary>
            public string TextoOcr { get; set; } = string.Empty;
        }

        /// <summary>
        /// Guarda un archivo temporalmente
        /// </summary>
        private async Task<string> GuardarArchivoTemporalAsync(IFormFile file)
        {
            var tempPath = Path.GetTempFileName();
            using var stream = new FileStream(tempPath, FileMode.Create);
            await file.CopyToAsync(stream);
            return tempPath;
        }
    }
} 