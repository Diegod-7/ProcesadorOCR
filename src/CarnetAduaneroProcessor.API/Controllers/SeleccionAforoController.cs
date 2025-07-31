using Microsoft.AspNetCore.Mvc;
using CarnetAduaneroProcessor.Core.Models;
using CarnetAduaneroProcessor.Core.Services;
using AutoMapper;
using System.Text.Json;

namespace CarnetAduaneroProcessor.API.Controllers
{
    /// <summary>
    /// Controlador para procesar documentos de Selección de Aforo
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class SeleccionAforoController : ControllerBase
    {
        private readonly ISeleccionAforoService _seleccionAforoService;
        private readonly ILogger<SeleccionAforoController> _logger;
        private readonly IMapper _mapper;

        public SeleccionAforoController(
            ISeleccionAforoService seleccionAforoService,
            ILogger<SeleccionAforoController> logger,
            IMapper mapper)
        {
            _seleccionAforoService = seleccionAforoService;
            _logger = logger;
            _mapper = mapper;
        }

        /// <summary>
        /// Sube y procesa un archivo PNG de Selección de Aforo
        /// </summary>
        /// <param name="file">Archivo PNG a procesar</param>
        /// <returns>Datos extraídos de la selección de aforo</returns>
        [HttpPost("procesar")]
        [ProducesResponseType(typeof(SeleccionAforo), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<SeleccionAforo>> ProcesarPng(IFormFile file)
        {
            try
            {
                _logger.LogInformation("Iniciando procesamiento de archivo PNG de Selección de Aforo: {FileName}", file?.FileName);

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
                    var seleccionAforo = await _seleccionAforoService.ExtraerDatosAsync(tempPath);

                    _logger.LogInformation("Selección de Aforo procesada exitosamente: {NumeroDin}", seleccionAforo.NumeroDin);

                    return CreatedAtAction(nameof(ObtenerPorId), new { id = seleccionAforo.Id }, seleccionAforo);
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
        [ProducesResponseType(typeof(List<SeleccionAforo>), 201)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<List<SeleccionAforo>>> ProcesarLote(IFormFileCollection files)
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

                var resultados = new List<SeleccionAforo>();
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
                            var seleccionAforo = await _seleccionAforoService.ExtraerDatosAsync(tempPath);
                            resultados.Add(seleccionAforo);
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
                    seleccionesAforo = resultados,
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
        /// Obtiene una selección de aforo por ID
        /// </summary>
        /// <param name="id">ID de la selección de aforo</param>
        /// <returns>Selección de aforo encontrada</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(SeleccionAforo), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<SeleccionAforo>> ObtenerPorId(int id)
        {
            try
            {
                // Por ahora retornamos un mock, en el futuro se conectará a base de datos
                var seleccionAforo = new SeleccionAforo
                {
                    Id = id,
                    NumeroDin = "2120204867",
                    FechaAceptacion = DateTime.Now.AddDays(-30),
                    TipoRevision = "FISICO",
                    NombreAgente = "ALEXIS MONTENEGRO P"
                };

                if (seleccionAforo == null)
                {
                    return NotFound(new { message = $"No se encontró la selección de aforo con ID {id}" });
                }

                return Ok(seleccionAforo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo selección de aforo con ID: {Id}", id);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Obtiene todas las selecciones de aforo
        /// </summary>
        /// <param name="page">Número de página</param>
        /// <param name="pageSize">Tamaño de página</param>
        /// <param name="search">Término de búsqueda</param>
        /// <returns>Lista de selecciones de aforo</returns>
        [HttpGet]
        [ProducesResponseType(typeof(List<SeleccionAforo>), 200)]
        public async Task<ActionResult<List<SeleccionAforo>>> ObtenerTodos(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null)
        {
            try
            {
                // Por ahora retornamos datos mock, en el futuro se conectará a base de datos
                var seleccionesAforo = new List<SeleccionAforo>
                {
                    new SeleccionAforo
                    {
                        Id = 1,
                        NumeroDin = "2120204867",
                        FechaAceptacion = DateTime.Now.AddDays(-30),
                        TipoRevision = "FISICO",
                        NombreAgente = "ALEXIS MONTENEGRO P",
                        NombreAduana = "SAN ANTONIO"
                    }
                };

                return Ok(seleccionesAforo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo selecciones de aforo");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Procesa texto OCR para extraer datos de Selección de Aforo
        /// </summary>
        /// <param name="request">Solicitud con texto OCR</param>
        /// <returns>Datos extraídos de la selección de aforo</returns>
        [HttpPost("procesar-texto-ocr")]
        [ProducesResponseType(typeof(SeleccionAforo), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<SeleccionAforo>> ProcesarTextoOcr([FromBody] ProcesarTextoOcrSeleccionAforoRequest request)
        {
            try
            {
                _logger.LogInformation("Iniciando procesamiento de texto OCR para Selección de Aforo");

                if (string.IsNullOrWhiteSpace(request.TextoOcr))
                {
                    return BadRequest(new { message = "El texto OCR no puede estar vacío" });
                }

                var seleccionAforo = await _seleccionAforoService.ProcesarTextoOcrAsync(request.TextoOcr);

                _logger.LogInformation("Procesamiento de texto OCR completado para Selección de Aforo: {NumeroDin}", seleccionAforo.NumeroDin);

                return Ok(seleccionAforo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando texto OCR para Selección de Aforo");
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
                    var esValido = await _seleccionAforoService.ValidarPngAsync(tempPath);
                    
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
                    totalSeleccionesAforo = 1,
                    seleccionesValidas = 1,
                    seleccionesInvalidas = 0,
                    ultimaProcesada = DateTime.Now.AddHours(-1),
                    tasaExito = 100.0
                };

                return Ok(estadisticas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo estadísticas de Selección de Aforo");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Elimina una selección de aforo por ID
        /// </summary>
        /// <param name="id">ID de la selección de aforo</param>
        /// <returns>Resultado de la eliminación</returns>
        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Eliminar(int id)
        {
            try
            {
                // Por ahora simulamos eliminación exitosa
                _logger.LogInformation("Eliminando selección de aforo con ID: {Id}", id);
                
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error eliminando selección de aforo con ID: {Id}", id);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Solicitud para procesar texto OCR de Selección de Aforo
        /// </summary>
        public class ProcesarTextoOcrSeleccionAforoRequest
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