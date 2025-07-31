using CarnetAduaneroProcessor.Core.Models;
using CarnetAduaneroProcessor.Core.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace CarnetAduaneroProcessor.API.Controllers
{
    /// <summary>
    /// Controlador para procesar Declaraciones de Ingreso (DI)
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class DeclaracionIngresoController : ControllerBase
    {
        private readonly IDeclaracionIngresoService _declaracionIngresoService;
        private readonly ILogger<DeclaracionIngresoController> _logger;

        public DeclaracionIngresoController(
            IDeclaracionIngresoService declaracionIngresoService,
            ILogger<DeclaracionIngresoController> logger)
        {
            _declaracionIngresoService = declaracionIngresoService;
            _logger = logger;
        }
         /// <summary>
        /// Procesa 1 o 2 archivos PNG de Declaración de Ingreso
        /// </summary>
        /// <param name="files">1 o 2 archivos PNG de la declaración</param>
        /// <returns>Datos extraídos combinados de los archivos</returns>
        [HttpPost("procesar")]
        public async Task<ActionResult<object>> ProcesarDeclaracionIngreso(IFormFileCollection files)
        {
            try
            {
                _logger.LogInformation("Procesando Declaración de Ingreso con {Count} archivos PNG", files?.Count ?? 0);

                if (files == null || files.Count == 0)
                {
                    return BadRequest(new { Status = false, Message = "Se requiere al menos 1 archivo PNG" });
                }

                if (files.Count > 2)
                {
                    return BadRequest(new { Status = false, Message = "Se permiten máximo 2 archivos PNG" });
                }

                var declaraciones = new List<DeclaracionIngreso>();
                var archivosProcesados = new List<string>();
                var errores = new List<string>();

                // Procesar cada archivo
                foreach (var file in files)
                {
                    if (file.Length == 0)
                    {
                        errores.Add($"Archivo {file.FileName}: Archivo vacío");
                        continue;
                    }

                    // Validar que sea PNG
                    var contentType = file.ContentType.ToLower();
                    var extension = Path.GetExtension(file.FileName).ToLower();
                    
                    bool isPngFile = contentType.Contains("png") || extension == ".png";

                    if (!isPngFile)
                    {
                        errores.Add($"Archivo {file.FileName}: Solo se permiten archivos PNG");
                        continue;
                    }

                    try
                    {
                        // Convertir el archivo a bytes
                        using var memoryStream = new MemoryStream();
                        await file.CopyToAsync(memoryStream);
                        var fileBytes = memoryStream.ToArray();

                        // Procesar el archivo
                        var declaracion = await _declaracionIngresoService.ExtraerDatosAsync(fileBytes, file.FileName);
                        declaraciones.Add(declaracion);
                        archivosProcesados.Add(file.FileName);

                        _logger.LogInformation("Archivo PNG procesado: {FileName}", file.FileName);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error procesando archivo: {FileName}", file.FileName);
                        errores.Add($"Archivo {file.FileName}: {ex.Message}");
                    }
                }

                if (declaraciones.Count == 0)
                {
                    return BadRequest(new { 
                        Status = false, 
                        Message = "No se pudo procesar ningún archivo válido",
                        Errores = errores
                    });
                }

                // Combinar datos de todas las declaraciones
                var declaracionCombinada = new DeclaracionIngreso
                {
                    NumeroIdentificacion = declaraciones.FirstOrDefault(d => !string.IsNullOrEmpty(d.NumeroIdentificacion))?.NumeroIdentificacion ?? string.Empty,
                    FechaVencimiento = declaraciones.FirstOrDefault(d => d.FechaVencimiento.HasValue)?.FechaVencimiento,
                    TipoOperacion = declaraciones.FirstOrDefault(d => !string.IsNullOrEmpty(d.TipoOperacion))?.TipoOperacion,
                    CodigoTipoOperacion = declaraciones.FirstOrDefault(d => !string.IsNullOrEmpty(d.CodigoTipoOperacion))?.CodigoTipoOperacion,
                    TipoBulto = declaraciones.FirstOrDefault(d => !string.IsNullOrEmpty(d.TipoBulto))?.TipoBulto,
                    PesoBruto = declaraciones.FirstOrDefault(d => !string.IsNullOrEmpty(d.PesoBruto))?.PesoBruto,
                    SelloContenedor = declaraciones.FirstOrDefault(d => !string.IsNullOrEmpty(d.SelloContenedor))?.SelloContenedor,
                    FechaAceptacion = declaraciones.FirstOrDefault(d => d.FechaAceptacion.HasValue)?.FechaAceptacion,
                    TotalPagar = declaraciones.FirstOrDefault(d => !string.IsNullOrEmpty(d.TotalPagar))?.TotalPagar,
                    Aduana = declaraciones.FirstOrDefault(d => !string.IsNullOrEmpty(d.Aduana))?.Aduana,
                    Consignatario = declaraciones.FirstOrDefault(d => !string.IsNullOrEmpty(d.Consignatario))?.Consignatario,
                    Consignante = declaraciones.FirstOrDefault(d => !string.IsNullOrEmpty(d.Consignante))?.Consignante,
                    DocumentoTransporte = declaraciones.FirstOrDefault(d => !string.IsNullOrEmpty(d.DocumentoTransporte))?.DocumentoTransporte,
                    ValorCif = declaraciones.FirstOrDefault(d => !string.IsNullOrEmpty(d.ValorCif))?.ValorCif,
                    Manifiesto = declaraciones.FirstOrDefault(d => !string.IsNullOrEmpty(d.Manifiesto))?.Manifiesto,
                    PuertoEmbarque = declaraciones.FirstOrDefault(d => !string.IsNullOrEmpty(d.PuertoEmbarque))?.PuertoEmbarque,
                    PuertoDesembarque = declaraciones.FirstOrDefault(d => !string.IsNullOrEmpty(d.PuertoDesembarque))?.PuertoDesembarque,
                    CompaniaTransportista = declaraciones.FirstOrDefault(d => !string.IsNullOrEmpty(d.CompaniaTransportista))?.CompaniaTransportista,
                    NombreArchivo = $"DI_Combinado_{archivosProcesados.Count}_partes",
                    MetodoExtraccion = "Azure Vision OCR + Regex",
                    ConfianzaExtraccion = declaraciones.Average(d => d.ConfianzaExtraccion),
                    TextoExtraido = string.Join("\n\n--- ARCHIVO SEPARADOR ---\n\n", 
                        declaraciones.Select(d => d.TextoExtraido ?? "")),
                    FechaCreacion = DateTime.UtcNow,
                    FechaModificacion = DateTime.UtcNow
                };

                // Verificar si se extrajeron campos críticos
                var camposExtraidos = !string.IsNullOrEmpty(declaracionCombinada.NumeroIdentificacion) ||
                                    !string.IsNullOrEmpty(declaracionCombinada.TipoOperacion) ||
                                    !string.IsNullOrEmpty(declaracionCombinada.PesoBruto);

                _logger.LogInformation("Declaración de Ingreso procesada exitosamente: {NumeroIdentificacion}", declaracionCombinada.NumeroIdentificacion);

                return Ok(new
                {
                    Status = true,
                    Message = $"Declaración de Ingreso procesada correctamente desde {archivosProcesados.Count} archivo(s) PNG",
                    Data = declaracionCombinada,
                    CamposExtraidos = camposExtraidos,
                    ArchivosProcesados = archivosProcesados.Count,
                    ArchivosNombres = archivosProcesados,
                    TipoProcesamiento = archivosProcesados.Count == 1 ? "Archivo único" : "Archivos múltiples",
                    Errores = errores.Count > 0 ? errores : null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando Declaración de Ingreso con archivos PNG");
                return StatusCode(500, new { Status = false, Message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Procesa un solo archivo PNG de Declaración de Ingreso
        /// </summary>
        /// <param name="file">Archivo PNG de la declaración de ingreso</param>
        /// <returns>Datos extraídos de la declaración</returns>
        [HttpPost("procesar-single")]
        public async Task<ActionResult<object>> ProcesarDeclaracionIngresoSingle(IFormFile file)
        {
            try
            {
                _logger.LogInformation("Procesando Declaración de Ingreso con 1 archivo PNG: {FileName}", file?.FileName);

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

                try
                {
                    // Convertir el archivo a bytes
                    using var memoryStream = new MemoryStream();
                    await file.CopyToAsync(memoryStream);
                    var fileBytes = memoryStream.ToArray();

                    // Procesar el archivo
                    var declaracion = await _declaracionIngresoService.ExtraerDatosAsync(fileBytes, file.FileName);

                    // Verificar si se extrajeron campos críticos
                    var camposExtraidos = !string.IsNullOrEmpty(declaracion.NumeroIdentificacion) ||
                                        !string.IsNullOrEmpty(declaracion.TipoOperacion) ||
                                        !string.IsNullOrEmpty(declaracion.PesoBruto);

                    _logger.LogInformation("Declaración de Ingreso procesada exitosamente: {NumeroIdentificacion}", declaracion.NumeroIdentificacion);

                    return Ok(new
                    {
                        Status = true,
                        Message = "Declaración de Ingreso procesada correctamente desde 1 archivo PNG",
                        Data = declaracion,
                        CamposExtraidos = camposExtraidos,
                        ArchivosProcesados = 1,
                        ArchivosNombres = new List<string> { file.FileName },
                        TipoProcesamiento = "Archivo único"
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error procesando archivo: {FileName}", file.FileName);
                    return BadRequest(new { 
                        Status = false, 
                        Message = $"Error procesando archivo: {ex.Message}",
                        Archivo = file.FileName
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando Declaración de Ingreso con archivo PNG único");
                return StatusCode(500, new { Status = false, Message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Extrae datos de una Declaración de Ingreso desde un archivo (PDF, PNG, JPG)
        /// </summary>
        /// <param name="file">Archivo de la declaración de ingreso (PDF, PNG, JPG)</param>
        /// <returns>Datos extraídos de la declaración</returns>
        [HttpPost("extraer-archivo")]
        public async Task<ActionResult<DeclaracionIngreso>> ExtraerDatosArchivo(IFormFile file)
        {
            try
            {
                _logger.LogInformation("Iniciando extracción de datos de DI desde archivo: {FileName}", file?.FileName);

                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { Status = false, Message = "No se proporcionó ningún archivo" });
                }

                // Validar tipo de archivo
                var contentType = file.ContentType.ToLower();
                var extension = Path.GetExtension(file.FileName).ToLower();
                
                bool isValidFile = contentType.Contains("pdf") || contentType.Contains("png") || contentType.Contains("jpeg") || contentType.Contains("jpg") ||
                                 extension == ".pdf" || extension == ".png" || extension == ".jpg" || extension == ".jpeg";

                if (!isValidFile)
                {
                    return BadRequest(new { Status = false, Message = "El archivo debe ser PDF, PNG o JPG" });
                }

                // Convertir el archivo a bytes
                using var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream);
                var fileBytes = memoryStream.ToArray();

                // Procesar el archivo
                var declaracion = await _declaracionIngresoService.ExtraerDatosAsync(fileBytes, file.FileName);

                _logger.LogInformation("Extracción completada para DI: {NumeroIdentificacion}", declaracion.NumeroIdentificacion);

                return Ok(new
                {
                    Status = true,
                    Message = "Datos extraídos correctamente",
                    Data = declaracion
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Error de validación: {Message}", ex.Message);
                return BadRequest(new { Status = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando archivo de DI: {FileName}", file?.FileName);
                return StatusCode(500, new { Status = false, Message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Procesa texto OCR para extraer datos de declaración de ingreso
        /// </summary>
        /// <param name="request">Solicitud con el texto OCR</param>
        /// <returns>Datos extraídos de la declaración</returns>
        [HttpPost("procesar-texto")]
        public async Task<ActionResult<DeclaracionIngreso>> ProcesarTexto([FromBody] ProcesarTextoRequest request)
        {
            try
            {
                _logger.LogInformation("Iniciando procesamiento de texto OCR para DI");

                if (string.IsNullOrWhiteSpace(request.TextoOcr))
                {
                    return BadRequest(new { Status = false, Message = "El texto OCR es requerido" });
                }

                // Procesar el texto OCR
                var declaracion = await _declaracionIngresoService.ProcesarTextoOcrAsync(request.TextoOcr);

                _logger.LogInformation("Procesamiento completado para DI: {NumeroIdentificacion}", declaracion.NumeroIdentificacion);

                return Ok(new
                {
                    Status = true,
                    Message = "Texto procesado correctamente",
                    Data = declaracion
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando texto OCR para DI");
                return StatusCode(500, new { Status = false, Message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Obtiene los campos críticos extraídos de una declaración de ingreso (PDF, PNG, JPG)
        /// </summary>
        /// <param name="file">Archivo de la declaración (PDF, PNG, JPG)</param>
        /// <returns>Campos críticos extraídos (los marcados en rojo)</returns>
        [HttpPost("campos-criticos")]
        public async Task<ActionResult<object>> ExtraerCamposCriticos(IFormFile file)
        {
            try
            {
                _logger.LogInformation("Extrayendo campos críticos de DI desde archivo: {FileName}", file?.FileName);

                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { Status = false, Message = "No se proporcionó ningún archivo" });
                }

                // Validar tipo de archivo
                var contentType = file.ContentType.ToLower();
                var extension = Path.GetExtension(file.FileName).ToLower();
                
                bool isValidFile = contentType.Contains("pdf") || contentType.Contains("png") || contentType.Contains("jpeg") || contentType.Contains("jpg") ||
                                 extension == ".pdf" || extension == ".png" || extension == ".jpg" || extension == ".jpeg";

                if (!isValidFile)
                {
                    return BadRequest(new { Status = false, Message = "El archivo debe ser PDF, PNG o JPG" });
                }

                // Convertir el archivo a bytes
                using var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream);
                var fileBytes = memoryStream.ToArray();

                // Procesar el archivo
                var declaracion = await _declaracionIngresoService.ExtraerDatosAsync(fileBytes, file.FileName);

                // Extraer solo los campos críticos (marcados en rojo)
                var camposCriticos = new
                {
                    NumeroIdentificacion = declaracion.NumeroIdentificacion,
                    FechaVencimiento = declaracion.FechaVencimiento?.ToString("dd/MM/yyyy"),
                    TipoOperacion = declaracion.TipoOperacion,
                    CodigoTipoOperacion = declaracion.CodigoTipoOperacion,
                    TipoBulto = declaracion.TipoBulto,
                    PesoBruto = declaracion.PesoBruto,
                    SelloContenedor = declaracion.SelloContenedor,
                    FechaAceptacion = declaracion.FechaAceptacion?.ToString("dd/MM/yyyy"),
                    TotalPagar = declaracion.TotalPagar
                };

                _logger.LogInformation("Campos críticos extraídos para DI: {NumeroIdentificacion}", declaracion.NumeroIdentificacion);

                return Ok(new
                {
                    Status = true,
                    Message = "Campos críticos extraídos correctamente",
                    Data = camposCriticos,
                    CamposExtraidos = declaracion.EsValida
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extrayendo campos críticos de DI: {FileName}", file?.FileName);
                return StatusCode(500, new { Status = false, Message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Obtiene los campos críticos extraídos de 1 o más archivos de declaración de ingreso (PDF, PNG, JPG)
        /// </summary>
        /// <param name="files">1 o más archivos de la declaración (PDF, PNG, JPG)</param>
        /// <returns>Campos críticos extraídos combinados de todos los archivos</returns>
        [HttpPost("campos-criticos-multiple")]
        public async Task<ActionResult<object>> ExtraerCamposCriticosMultiple(IFormFileCollection files)
        {
            try
            {
                _logger.LogInformation("Extrayendo campos críticos de DI desde {Count} archivos", files?.Count ?? 0);

                if (files == null || files.Count == 0)
                {
                    return BadRequest(new { Status = false, Message = "No se proporcionaron archivos" });
                }

                var declaraciones = new List<DeclaracionIngreso>();
                var archivosProcesados = new List<string>();

                // Procesar cada archivo
                foreach (var file in files)
                {
                    if (file.Length == 0) continue;

                    // Validar tipo de archivo
                    var contentType = file.ContentType.ToLower();
                    var extension = Path.GetExtension(file.FileName).ToLower();
                    
                    bool isValidFile = contentType.Contains("pdf") || contentType.Contains("png") || contentType.Contains("jpeg") || contentType.Contains("jpg") ||
                                     extension == ".pdf" || extension == ".png" || extension == ".jpg" || extension == ".jpeg";

                    if (!isValidFile)
                    {
                        _logger.LogWarning("Tipo de archivo no soportado: {FileName} ({ContentType})", file.FileName, file.ContentType);
                        continue;
                    }

                    try
                    {
                        // Convertir el archivo a bytes
                        using var memoryStream = new MemoryStream();
                        await file.CopyToAsync(memoryStream);
                        var fileBytes = memoryStream.ToArray();

                        // Procesar el archivo
                        var declaracion = await _declaracionIngresoService.ExtraerDatosAsync(fileBytes, file.FileName);
                        declaraciones.Add(declaracion);
                        archivosProcesados.Add(file.FileName);

                        _logger.LogInformation("Archivo procesado: {FileName} ({ContentType})", file.FileName, file.ContentType);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error procesando archivo: {FileName}", file.FileName);
                    }
                }

                if (declaraciones.Count == 0)
                {
                    return BadRequest(new { Status = false, Message = "No se pudo procesar ningún archivo válido" });
                }

                // Combinar campos críticos de todas las declaraciones
                var camposCriticosCombinados = new
                {
                    NumeroIdentificacion = declaraciones.FirstOrDefault(d => !string.IsNullOrEmpty(d.NumeroIdentificacion))?.NumeroIdentificacion,
                    FechaVencimiento = declaraciones.FirstOrDefault(d => d.FechaVencimiento.HasValue)?.FechaVencimiento?.ToString("dd/MM/yyyy"),
                    TipoOperacion = declaraciones.FirstOrDefault(d => !string.IsNullOrEmpty(d.TipoOperacion))?.TipoOperacion,
                    CodigoTipoOperacion = declaraciones.FirstOrDefault(d => !string.IsNullOrEmpty(d.CodigoTipoOperacion))?.CodigoTipoOperacion,
                    TipoBulto = declaraciones.FirstOrDefault(d => !string.IsNullOrEmpty(d.TipoBulto))?.TipoBulto,
                    PesoBruto = declaraciones.FirstOrDefault(d => !string.IsNullOrEmpty(d.PesoBruto))?.PesoBruto,
                    SelloContenedor = declaraciones.FirstOrDefault(d => !string.IsNullOrEmpty(d.SelloContenedor))?.SelloContenedor,
                    FechaAceptacion = declaraciones.FirstOrDefault(d => d.FechaAceptacion.HasValue)?.FechaAceptacion?.ToString("dd/MM/yyyy"),
                    TotalPagar = declaraciones.FirstOrDefault(d => !string.IsNullOrEmpty(d.TotalPagar))?.TotalPagar
                };

                // Verificar si se extrajeron campos críticos
                var camposExtraidos = !string.IsNullOrEmpty(camposCriticosCombinados.NumeroIdentificacion) ||
                                    !string.IsNullOrEmpty(camposCriticosCombinados.TipoOperacion) ||
                                    !string.IsNullOrEmpty(camposCriticosCombinados.PesoBruto);

                _logger.LogInformation("Campos críticos combinados extraídos de {Count} archivos", archivosProcesados.Count);

                return Ok(new
                {
                    Status = true,
                    Message = $"Campos críticos extraídos correctamente de {archivosProcesados.Count} archivo(s)",
                    Data = camposCriticosCombinados,
                    CamposExtraidos = camposExtraidos,
                    ArchivosProcesados = archivosProcesados.Count,
                    ArchivosNombres = archivosProcesados
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extrayendo campos críticos de múltiples archivos DI");
                return StatusCode(500, new { Status = false, Message = "Error interno del servidor" });
            }
        }

       
        /// <summary>
        /// Valida si un archivo es una declaración de ingreso válida (PDF, PNG, JPG)
        /// </summary>
        /// <param name="file">Archivo a validar (PDF, PNG, JPG)</param>
        /// <returns>Resultado de la validación</returns>
        [HttpPost("validar")]
        public async Task<ActionResult<object>> ValidarDeclaracion(IFormFile file)
        {
            try
            {
                _logger.LogInformation("Validando declaración de ingreso: {FileName}", file?.FileName);

                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { Status = false, Message = "No se proporcionó ningún archivo" });
                }

                // Validar tipo de archivo
                var contentType = file.ContentType.ToLower();
                var extension = Path.GetExtension(file.FileName).ToLower();
                
                bool isValidFile = contentType.Contains("pdf") || contentType.Contains("png") || contentType.Contains("jpeg") || contentType.Contains("jpg") ||
                                 extension == ".pdf" || extension == ".png" || extension == ".jpg" || extension == ".jpeg";

                if (!isValidFile)
                {
                    return BadRequest(new { Status = false, Message = "El archivo debe ser PDF, PNG o JPG" });
                }

                // Convertir el archivo a bytes
                using var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream);
                var fileBytes = memoryStream.ToArray();

                // Procesar el archivo
                var declaracion = await _declaracionIngresoService.ExtraerDatosAsync(fileBytes, file.FileName);

                var resultado = new
                {
                    EsValida = declaracion.EsValida,
                    NumeroIdentificacion = declaracion.NumeroIdentificacion,
                    ConfianzaExtraccion = declaracion.ConfianzaExtraccion,
                    Comentarios = declaracion.Comentarios,
                    CamposCriticosExtraidos = !string.IsNullOrEmpty(declaracion.NumeroIdentificacion) &&
                                             !string.IsNullOrEmpty(declaracion.TipoOperacion) &&
                                             !string.IsNullOrEmpty(declaracion.PesoBruto)
                };

                return Ok(new
                {
                    Status = true,
                    Message = "Validación completada",
                    Data = resultado
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validando declaración de ingreso: {FileName}", file?.FileName);
                return StatusCode(500, new { Status = false, Message = "Error interno del servidor" });
            }
        }
    }

    /// <summary>
    /// Modelo para solicitud de procesamiento de texto
    /// </summary>
    public class ProcesarTextoRequest
    {
        /// <summary>
        /// Texto OCR a procesar
        /// </summary>
        public string TextoOcr { get; set; } = string.Empty;
    }

    /// <summary>
    /// Modelo para solicitud de procesamiento de Declaración de Ingreso con 1 o 2 PNG
    /// </summary>
    public class ProcesarDeclaracionIngresoRequest
    {
        /// <summary>
        /// Primer archivo PNG de la declaración (requerido)
        /// </summary>
        public IFormFile Archivo1 { get; set; } = null!;

        /// <summary>
        /// Segundo archivo PNG de la declaración (opcional)
        /// </summary>
        public IFormFile? Archivo2 { get; set; }
    }
} 