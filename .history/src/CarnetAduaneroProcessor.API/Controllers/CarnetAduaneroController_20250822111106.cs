using Microsoft.AspNetCore.Mvc;
using CarnetAduaneroProcessor.Core.Models;
using CarnetAduaneroProcessor.Core.Services;
using AutoMapper;
using System.Text.Json;
// using System.IO; // Eliminado para evitar conflicto con File()

namespace CarnetAduaneroProcessor.API.Controllers
{
    /// <summary>
    /// Controlador para procesar PDFs de Carnés Aduaneros
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class CarnetAduaneroController : ControllerBase
    {
        private readonly IPdfExtractionService _pdfExtractionService;
        private readonly ICarnetAduaneroRepository _repository;
        private readonly ICarnetAduaneroProcessorService _processorService;
        private readonly ILogger<CarnetAduaneroController> _logger;
        private readonly IMapper _mapper;

        public CarnetAduaneroController(
            IPdfExtractionService pdfExtractionService,
            ICarnetAduaneroRepository repository,
            ICarnetAduaneroProcessorService processorService,
            ILogger<CarnetAduaneroController> logger,
            IMapper mapper)
        {
            _pdfExtractionService = pdfExtractionService;
            _repository = repository;
            _processorService = processorService;
            _logger = logger;
            _mapper = mapper;
        }

        /// <summary>
        /// Sube y procesa un archivo PNG de carné aduanero usando IA directamente
        /// </summary>
        /// <param name="file">Archivo PNG a procesar</param>
        /// <returns>Datos extraídos del carné usando IA</returns>
        [HttpPost("procesar")]
        [ProducesResponseType(typeof(CarnetAduaneroData), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<CarnetAduaneroData>> ProcesarPng(IFormFile file)
        {
            try
            {
                _logger.LogInformation("Iniciando procesamiento de archivo PNG con IA: {FileName}", file?.FileName);

                // Validar archivo
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { message = "No se ha proporcionado ningún archivo" });
                }

                if (!file.ContentType.Equals("image/png", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(new { message = "El archivo debe ser un PNG" });
                }

                if (file.Length > 20 * 1024 * 1024) // 20MB máximo para IA
                {
                    return BadRequest(new { message = "El archivo no puede exceder 20MB" });
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
                    var tempPath = await GuardarArchivoTemporalAsync(file);
                    try
                    {
                        var carnetData = await _processorService.ExtraerDatosAsync(tempPath);
                        _logger.LogInformation("Carné procesado exitosamente con método tradicional: {NumeroCarne}", carnetData.NumeroCarne);
                        return Ok(carnetData);
                    }
                    finally
                    {
                        if (System.IO.File.Exists(tempPath))
                        {
                            System.IO.File.Delete(tempPath);
                        }
                    }
                }

                // Procesar imagen directamente con IA
                var jsonExtraido = await aiService.ProcesarImagenDirectamenteAsync(imagenBytes, file.FileName);

                // Convertir JSON a CarnetAduaneroData
                try
                {
                    var carnetData = JsonSerializer.Deserialize<CarnetAduaneroData>(jsonExtraido);
                    if (carnetData != null)
                    {
                        _logger.LogInformation("Carné procesado exitosamente con IA: {NumeroCarne}", carnetData.NumeroCarne);
                        return Ok(carnetData);
                    }
                    else
                    {
                        _logger.LogWarning("IA devolvió JSON inválido, usando método tradicional");
                        // Fallback al método tradicional
                        var tempPath = await GuardarArchivoTemporalAsync(file);
                        try
                        {
                            var carnetDataFallback = await _processorService.ExtraerDatosAsync(tempPath);
                            return Ok(carnetDataFallback);
                        }
                        finally
                        {
                            if (System.IO.File.Exists(tempPath))
                            {
                                System.IO.File.Delete(tempPath);
                            }
                        }
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Error parseando JSON de IA, usando método tradicional");
                    // Fallback al método tradicional
                    var tempPath = await GuardarArchivoTemporalAsync(file);
                    try
                    {
                        var carnetDataFallback = await _processorService.ExtraerDatosAsync(tempPath);
                        return Ok(carnetDataFallback);
                    }
                    finally
                    {
                        if (System.IO.File.Exists(tempPath))
                        {
                            System.IO.File.Delete(tempPath);
                        }
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
                _logger.LogError(ex, "Error al procesar archivo con IA: {FileName}", file?.FileName);
                return StatusCode(500, new { message = "Error interno del servidor al procesar el archivo con IA" });
            }
        }

        /// <summary>
        /// Procesa múltiples PDFs en lote
        /// </summary>
        /// <param name="files">Lista de archivos PDF</param>
        /// <returns>Resultados del procesamiento</returns>
        [HttpPost("procesar-lote")]
        [ProducesResponseType(typeof(List<CarnetAduanero>), 201)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<List<CarnetAduanero>>> ProcesarLote(IFormFileCollection files)
        {
            try
            {
                _logger.LogInformation("Iniciando procesamiento de lote con {Count} archivos", files.Count);

                if (files == null || !files.Any())
                {
                    return BadRequest(new { message = "No se han proporcionado archivos" });
                }

                if (files.Count > 10) // Máximo 10 archivos por lote
                {
                    return BadRequest(new { message = "No se pueden procesar más de 10 archivos a la vez" });
                }

                var resultados = new List<CarnetAduanero>();
                var errores = new List<string>();

                foreach (var file in files)
                {
                    try
                    {
                        if (!file.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
                        {
                            errores.Add($"Archivo {file.FileName}: No es un PDF válido");
                            continue;
                        }

                        var tempPath = await GuardarArchivoTemporalAsync(file);
                        
                        try
                        {
                            var carnet = await _pdfExtractionService.ExtraerDatosAsync(tempPath);
                            
                            // Verificar duplicados
                            var existe = await _repository.ExistePorNumeroAsync(carnet.NumeroCarnet);

                            if (!existe)
                            {
                                carnet = await _repository.AgregarAsync(carnet);
                                resultados.Add(carnet);
                            }
                            else
                            {
                                errores.Add($"Archivo {file.FileName}: Carné {carnet.NumeroCarnet} ya existe");
                            }
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

                // Los resultados ya se guardaron individualmente

                var response = new
                {
                    procesados = resultados.Count,
                    errores = errores.Count,
                    carnets = resultados,
                    mensajesError = errores
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en procesamiento de lote");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Obtiene un carné por su ID
        /// </summary>
        /// <param name="id">ID del carné</param>
        /// <returns>Carné encontrado</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(CarnetAduanero), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<CarnetAduanero>> ObtenerPorId(int id)
        {
            var carnet = await _repository.ObtenerPorIdAsync(id);

            if (carnet == null)
            {
                return NotFound(new { message = $"No se encontró el carné con ID {id}" });
            }

            return Ok(carnet);
        }

        /// <summary>
        /// Obtiene todos los carnés con paginación
        /// </summary>
        /// <param name="page">Número de página</param>
        /// <param name="pageSize">Tamaño de página</param>
        /// <param name="search">Término de búsqueda</param>
        /// <returns>Lista de carnés</returns>
        [HttpGet]
        [ProducesResponseType(typeof(List<CarnetAduanero>), 200)]
        public async Task<ActionResult<List<CarnetAduanero>>> ObtenerTodos(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null)
        {
            var carnets = await _repository.ObtenerTodosAsync(page, pageSize, search);
            var total = await _repository.ObtenerTotalAsync();

            Response.Headers.Add("X-Total-Count", total.ToString());
            Response.Headers.Add("X-Page", page.ToString());
            Response.Headers.Add("X-PageSize", pageSize.ToString());

            return Ok(carnets.ToList());
        }

        /// <summary>
        /// Procesa una imagen directamente y extrae texto usando OCR
        /// </summary>
        /// <param name="file">Archivo de imagen (PNG, JPG, etc.)</param>
        /// <returns>Texto extraído de la imagen</returns>
        [HttpPost("procesar-imagen")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<object>> ProcesarImagen(IFormFile file)
        {
            try
            {
                _logger.LogInformation("Iniciando procesamiento de imagen: {FileName}", file?.FileName);

                // Validar archivo
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { message = "No se ha proporcionado ningún archivo" });
                }

                // Validar tipo de archivo (imágenes)
                var allowedTypes = new string[] { "image/jpeg", "image/jpg", "image/png", "image/bmp", "image/tiff" };
                if (!allowedTypes.Contains(file.ContentType.ToLower()))
                {
                    return BadRequest(new { message = "El archivo debe ser una imagen (JPG, PNG, BMP, TIFF)" });
                }

                if (file.Length > 10 * 1024 * 1024) // 10MB máximo
                {
                    return BadRequest(new { message = "El archivo no puede exceder 10MB" });
                }

                // Guardar archivo temporalmente
                var tempPath = await GuardarArchivoTemporalAsync(file);
                
                try
                {
                    _logger.LogInformation("Iniciando procesamiento de imagen con SkiaSharp");
                    await using var stream = new FileStream(tempPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                    
                    // Usar el método correcto que evita System.Drawing
                    var datosExtraidos = await _processorService.ExtraerDatosAsync(stream, file.FileName);
                    var metodoUsado = "SkiaSharp + Azure Computer Vision";
                    
                    _logger.LogInformation("Procesamiento completado con {Metodo}: {DatosCarnet}", 
                        metodoUsado, datosExtraidos?.NumeroCarne ?? "Sin datos");

                    var response = new
                    {
                        mensaje = "Datos extraídos exitosamente",
                        datos = datosExtraidos,
                        metodo = metodoUsado,
                        nombreArchivo = file.FileName,
                        tamanioArchivo = file.Length,
                        tipoArchivo = file.ContentType
                    };

                    return Ok(response);
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
                _logger.LogWarning(ex, "Error de validación en imagen: {FileName}", file?.FileName);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al procesar imagen: {FileName}", file?.FileName);
                return StatusCode(500, new { message = "Error interno del servidor al procesar la imagen" });
            }
        }

        /// <summary>
        /// Extrae y guarda las imágenes de un PDF
        /// </summary>
        /// <param name="file">Archivo PDF</param>
        /// <param name="outputFolder">Carpeta de destino (opcional)</param>
        /// <returns>Lista de rutas de las imágenes extraídas</returns>
        [HttpPost("extraer-imagenes")]
        [ProducesResponseType(typeof(List<string>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<List<string>>> ExtraerImagenes(IFormFile file, [FromQuery] string? outputFolder = null)
        {
            try
            {
                _logger.LogInformation("Iniciando extracción de imágenes del archivo: {FileName}", file?.FileName);

                // Validar archivo
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { message = "No se ha proporcionado ningún archivo" });
                }

                if (!file.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(new { message = "El archivo debe ser un PDF" });
                }

                if (file.Length > 10 * 1024 * 1024) // 10MB máximo
                {
                    return BadRequest(new { message = "El archivo no puede exceder 10MB" });
                }

                // Guardar archivo temporalmente
                var tempPath = await GuardarArchivoTemporalAsync(file);
                
                try
                {
                    // Extraer imágenes del PDF
                    var imagenesGuardadas = await _pdfExtractionService.GuardarImagenesExtraidasAsync(tempPath, outputFolder);

                    _logger.LogInformation("Extracción completada. {Count} imágenes guardadas", imagenesGuardadas.Count);

                    var response = new
                    {
                        mensaje = $"Se extrajeron {imagenesGuardadas.Count} imágenes del PDF",
                        imagenes = imagenesGuardadas,
                        carpetaDestino = outputFolder ?? Path.Combine(Path.GetDirectoryName(tempPath), $"{Path.GetFileNameWithoutExtension(file.FileName)}_imagenes")
                    };

                    return Ok(response);
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
                _logger.LogError(ex, "Error al extraer imágenes del archivo: {FileName}", file?.FileName);
                return StatusCode(500, new { message = "Error interno del servidor al extraer las imágenes" });
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
            var estadisticas = await _repository.ObtenerEstadisticasAsync();
            return Ok(estadisticas);
        }

        /// <summary>
        /// Exporta carnés a JSON
        /// </summary>
        /// <param name="fechaDesde">Fecha desde</param>
        /// <param name="fechaHasta">Fecha hasta</param>
        /// <returns>Archivo JSON con los datos</returns>
        [HttpGet("exportar")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> ExportarJson(
            [FromQuery] DateTime? fechaDesde = null,
            [FromQuery] DateTime? fechaHasta = null)
        {
            var carnets = await _repository.ObtenerTodosAsync(1, 1000); // Obtener todos para exportar

            // Filtrar por fecha si se especifica
            var carnetsFiltrados = carnets.AsEnumerable();
            
            if (fechaDesde.HasValue)
            {
                carnetsFiltrados = carnetsFiltrados.Where(c => c.FechaCreacion >= fechaDesde.Value);
            }

            if (fechaHasta.HasValue)
            {
                carnetsFiltrados = carnetsFiltrados.Where(c => c.FechaCreacion <= fechaHasta.Value);
            }

            var carnetsOrdenados = carnetsFiltrados
                .OrderByDescending(c => c.FechaCreacion)
                .ToList();

            var json = JsonSerializer.Serialize(carnetsOrdenados, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var fileName = $"carnets_aduaneros_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            
            return ((Microsoft.AspNetCore.Mvc.ControllerBase)this).File(
                System.Text.Encoding.UTF8.GetBytes(json),
                "application/json",
                fileName);
        }

        /// <summary>
        /// Elimina un carné por su ID
        /// </summary>
        /// <param name="id">ID del carné</param>
        /// <returns>Resultado de la eliminación</returns>
        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Eliminar(int id)
        {
            var carnet = await _repository.ObtenerPorIdAsync(id);

            if (carnet == null)
            {
                return NotFound(new { message = $"No se encontró el carné con ID {id}" });
            }

            var eliminado = await _repository.EliminarAsync(id);

            if (eliminado)
            {
                _logger.LogInformation("Carné eliminado: {NumeroCarnet}", carnet.NumeroCarnet);
                return NoContent();
            }

            return BadRequest(new { message = "Error al eliminar el carné" });
        }

        /// <summary>
        /// Procesa texto OCR de un carné aduanero y extrae los datos principales
        /// </summary>
        /// <param name="request">Objeto con la propiedad 'textoOcr'</param>
        /// <returns>Datos estructurados del carné aduanero</returns>
        [HttpPost("procesar-texto-ocr")]
        [ProducesResponseType(typeof(CarnetAduaneroData), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<CarnetAduaneroData>> ProcesarTextoOcr([FromBody] ProcesarTextoOcrRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.TextoOcr))
            {
                return BadRequest(new { mensaje = "El texto OCR es requerido" });
            }

            try
            {
                var resultado = await _processorService.ProcesarTextoOcrAsync(request.TextoOcr);
                if (!resultado.EsValido)
                {
                    return BadRequest(new { mensaje = resultado.MensajeError, datos = resultado });
                }
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al procesar texto OCR");
                return StatusCode(500, new { mensaje = "Error interno del servidor al procesar el texto OCR" });
            }
        }

        public class ProcesarTextoOcrRequest
        {
            public string TextoOcr { get; set; } = string.Empty;
        }

        /// <summary>
        /// Procesa una imagen directamente con IA (Gemma 3: 4B) para extraer el JSON completo
        /// </summary>
        /// <param name="file">Archivo de imagen a procesar (PNG, JPG, JPEG, etc.)</param>
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