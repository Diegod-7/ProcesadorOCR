using CarnetAduaneroProcessor.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using System.IO;

namespace CarnetAduaneroProcessor.Infrastructure.Services
{
    /// <summary>
    /// Servicio de post-procesamiento con IA usando Ollama con Gemma 3: 4B (multimodal)
    /// </summary>
    public class OllamaAiPostProcessorService : IAiPostProcessorService
    {
        private readonly ILogger<OllamaAiPostProcessorService> _logger;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly string _ollamaBaseUrl;
        private readonly string _modeloOllama;

        public OllamaAiPostProcessorService(ILogger<OllamaAiPostProcessorService> logger, IConfiguration configuration, HttpClient httpClient)
        {
            _logger = logger;
            _configuration = configuration;
            _httpClient = httpClient;
            
            // URL base de Ollama (por defecto localhost:11434)
            _ollamaBaseUrl = _configuration["Ollama:BaseUrl"] ?? "http://localhost:11434";
            
            // Modelo de Ollama (por defecto Gemma 3: 4B multimodal)
            _modeloOllama = _configuration["Ollama:Model"] ?? "gemma3:4b";
            
            // Configurar timeout personalizado para Ollama (más largo para procesamiento de imágenes)
            _httpClient.Timeout = TimeSpan.FromSeconds(300); // 5 minutos
        }

        /// <summary>
        /// Post-procesa un documento JSON usando Ollama para completar campos faltantes
        /// </summary>
        public async Task<string> PostProcesarDocumentoAsync(string documentoJson, string textoOcr)
        {
            try
            {
                _logger.LogInformation("Iniciando post-procesamiento con IA usando Ollama con modelo: {Modelo}", _modeloOllama);

                // Crear el prompt para Ollama
                var prompt = CrearPromptMejorado(documentoJson, textoOcr);

                // Llamar a Ollama
                var respuesta = await LlamarOllamaAsync(prompt);

                // Procesar la respuesta
                var documentoCompletado = ProcesarRespuestaOllama(respuesta, documentoJson);

                _logger.LogInformation("Post-procesamiento con IA completado exitosamente usando {Modelo}", _modeloOllama);
                return documentoCompletado;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en post-procesamiento con IA usando Ollama con modelo {Modelo}", _modeloOllama);
                return documentoJson; // Devolver el documento original si hay error
            }
        }

        /// <summary>
        /// Procesa una imagen directamente con Gemma 3: 4B para extraer el JSON completo
        /// </summary>
        public async Task<string> ProcesarImagenDirectamenteAsync(byte[] imagenBytes, string nombreArchivo)
        {
            try
            {
                _logger.LogInformation("Iniciando procesamiento directo de imagen con Gemma 3: 4B: {Archivo}", nombreArchivo);

                // Verificar que Ollama esté disponible antes de procesar
                if (!await VerificarOllamaDisponibleAsync())
                {
                    _logger.LogWarning("Ollama no está disponible. Verifica que esté ejecutándose en {Url}", _ollamaBaseUrl);
                    return CrearJsonVacioConError("Ollama no está disponible");
                }

                // Detectar el tipo de documento basado en el nombre del archivo o contenido
                var tipoDocumento = DetectarTipoDocumento(nombreArchivo);
                _logger.LogInformation("Tipo de documento detectado: {Tipo} para {Archivo}", tipoDocumento, nombreArchivo);

                // Crear el prompt específico para el tipo de documento
                var prompt = CrearPromptParaImagen(tipoDocumento);

                // Llamar a Ollama con la imagen
                var respuesta = await LlamarOllamaConImagenAsync(prompt, imagenBytes, nombreArchivo);

                if (string.IsNullOrWhiteSpace(respuesta))
                {
                    _logger.LogWarning("No se obtuvo respuesta de Ollama para {Archivo}. Verifica el modelo {Modelo} y la configuración.", nombreArchivo, _modeloOllama);
                    return CrearJsonVacioConError("No se obtuvo respuesta de Ollama");
                }

                // Procesar la respuesta de la imagen
                var documentoExtraido = ProcesarRespuestaImagen(respuesta, nombreArchivo);

                _logger.LogInformation("Procesamiento de imagen completado exitosamente con Gemma 3: 4B para tipo: {Tipo}", tipoDocumento);
                return documentoExtraido;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Timeout en procesamiento de imagen con Gemma 3: 4B: {Archivo}. Verifica que Ollama esté respondiendo.", nombreArchivo);
                return CrearJsonVacioConError("Timeout en procesamiento");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en procesamiento directo de imagen con Gemma 3: 4B: {Archivo}", nombreArchivo);
                return CrearJsonVacioConError($"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Detecta el tipo de documento basado en el nombre del archivo o contenido
        /// </summary>
        private string DetectarTipoDocumento(string nombreArchivo)
        {
            var nombreLower = nombreArchivo.ToLower();
            
            _logger.LogInformation("Detectando tipo de documento para archivo: {Archivo}", nombreArchivo);
            
            if (nombreLower.Contains("carne") || nombreLower.Contains("aduanero"))
            {
                _logger.LogInformation("Archivo {Archivo} detectado como CarnetAduanero", nombreArchivo);
                return "CarnetAduanero";
            }
            else if (nombreLower.Contains("comprobante") || nombreLower.Contains("transaccion") || nombreLower.Contains("tesoreria"))
            {
                _logger.LogInformation("Archivo {Archivo} detectado como ComprobanteTransaccion", nombreArchivo);
                return "ComprobanteTransaccion";
            }
            else if (nombreLower.Contains("recepcion") || nombreLower.Contains("dr"))
            {
                _logger.LogInformation("Archivo {Archivo} detectado como DocumentoRecepcion", nombreArchivo);
                return "DocumentoRecepcion";
            }
            else if (nombreLower.Contains("declaracion") || nombreLower.Contains("ingreso") || nombreLower.Contains("di"))
            {
                _logger.LogInformation("Archivo {Archivo} detectado como DeclaracionIngreso", nombreArchivo);
                return "DeclaracionIngreso";
            }
            else if (nombreLower.Contains("guia") || nombreLower.Contains("despacho"))
            {
                _logger.LogInformation("Archivo {Archivo} detectado como GuiaDespacho", nombreArchivo);
                return "GuiaDespacho";
            }
            else if (nombreLower.Contains("aforo") || nombreLower.Contains("seleccion"))
            {
                _logger.LogInformation("Archivo {Archivo} detectado como SeleccionAforo", nombreArchivo);
                return "SeleccionAforo";
            }
            else if (nombreLower.Contains("tact") || nombreLower.Contains("tatc") || nombreLower.Contains("adc"))
            {
                // Para archivos TACT, siempre usar el tipo TactAdc
                // La IA analizará el contenido y extraerá la información correcta
                _logger.LogInformation("Archivo {Archivo} detectado como TactAdc", nombreArchivo);
                return "TactAdc";
            }
            else
            {
                _logger.LogInformation("Archivo {Archivo} detectado como Generico", nombreArchivo);
                return "Generico"; // Tipo genérico por defecto para análisis de contenido
            }
        }

        /// <summary>
        /// Crea un prompt optimizado para análisis de imágenes con Gemma 3: 4B según el tipo de documento
        /// </summary>
        private string CrearPromptParaImagen(string tipoDocumento)
        {
            switch (tipoDocumento)
            {
                case "ComprobanteTransaccion":
                    return CrearPromptComprobanteTransaccion();
                case "CarnetAduanero":
                    return CrearPromptCarnetAduanero();
                case "DocumentoRecepcion":
                    return CrearPromptDocumentoRecepcion();
                case "DeclaracionIngreso":
                    return CrearPromptDeclaracionIngreso();
                case "GuiaDespacho":
                    return CrearPromptGuiaDespacho();
                case "TactAdc":
                    return CrearPromptTactAdc();
                case "SeleccionAforo":
                    return CrearPromptSeleccionAforo();
                default:
                    return CrearPromptGenerico();
            }
        }

        /// <summary>
        /// Crea prompt específico para Comprobante de Transacción
        /// </summary>
        private string CrearPromptComprobanteTransaccion()
        {
            return @"Eres un asistente experto en procesamiento de documentos chilenos usando Gemma 3: 4B. Tu tarea es analizar una imagen de COMPROBANTE DE TRANSACCIÓN de la Tesorería General de la República y extraer TODA la información disponible en formato JSON.

INSTRUCCIONES DETALLADAS:
1. Analiza cuidadosamente la imagen del COMPROBANTE DE TRANSACCIÓN
2. Identifica TODOS los campos disponibles según el modelo ComprobanteTransaccion
3. Extrae la información en formato JSON estructurado con los nombres exactos de campos
4. Para fechas, usa el formato ISO 8601 (YYYY-MM-DD) para fechas simples o (YYYY-MM-DDTHH:mm:ss) para fechas con hora
5. Para números, usa el formato exacto como aparece en el documento
6. Para monedas, usa el formato numérico sin símbolos de moneda
7. IMPORTANTE: ConfianzaExtraccion debe ser un número decimal entre 0.0 y 1.0 (ej: 0.95)

CAMPOS ESPECÍFICOS DE COMPROBANTE DE TRANSACCIÓN:
- NumeroFolio: Número de folio del documento (ej: 4560010758)
- TotalPagado: Monto total pagado (ej: 8153962)
- Rut: RUT del contribuyente (ej: 77591058-5)
- Formulario: Tipo de formulario (ej: 15)
- FechaVencimiento: Fecha de vencimiento (ej: 09-07-2025)
- MonedaPago: Moneda del pago (ej: CLP)
- FechaPago: Fecha y hora del pago (ej: 24-06-2025 17:44:12)
- InstitucionRecaudadora: Institución que recauda (ej: BANCO ITAU)
- IdentificadorTransaccion: ID de la transacción (ej: 02847341-57208059)
- CodigoBarras: Código de barras (ej: 06240508201625063001504715)
- NumeroReferencia: Número de referencia si está disponible

FORMATO DE RESPUESTA:
Responde ÚNICAMENTE con el JSON extraído de la imagen, sin explicaciones adicionales, sin markdown, sin texto extra.
El JSON debe ser válido y parseable inmediatamente.

IMPORTANTE: 
- ConfianzaExtraccion debe ser un número decimal (ej: 0.95), NO un string
- Usa los nombres exactos de campos en PascalCase
- Si un campo no se puede extraer, déjalo como null
- Extrae TODA la información visible en la imagen

EJEMPLO DE RESPUESTA ESPERADA:
{
  ""NumeroFolio"": ""4560010758"",
  ""TotalPagado"": 8153962,
  ""Rut"": ""77591058-5"",
  ""Formulario"": ""15"",
  ""FechaVencimiento"": ""2025-07-09"",
  ""MonedaPago"": ""CLP"",
  ""FechaPago"": ""2025-06-24T17:44:12"",
  ""InstitucionRecaudadora"": ""BANCO ITAU"",
  ""IdentificadorTransaccion"": ""02847341-57208059"",
  ""CodigoBarras"": ""06240508201625063001504715"",
  ""NumeroReferencia"": null,
  ""ConfianzaExtraccion"": 0.95
}";
        }

        /// <summary>
        /// Crea prompt específico para Carné Aduanero
        /// </summary>
        private string CrearPromptCarnetAduanero()
        {
            return @"Eres un asistente experto en procesamiento de documentos chilenos usando Gemma 3: 4B. Tu tarea es analizar una imagen de CARNÉ ADUANERO y extraer TODA la información disponible en formato JSON.

INSTRUCCIONES DETALLADAS:
1. Analiza cuidadosamente la imagen del CARNÉ ADUANERO
2. Identifica TODOS los campos disponibles según el modelo CarnetAduaneroData
3. Extrae la información en formato JSON estructurado con los nombres exactos de campos
4. Para fechas, usa el formato ISO 8601 (YYYY-MM-DD) para fechas simples
5. Para números, usa el formato exacto como aparece en el documento
6. IMPORTANTE: ConfianzaExtraccion debe ser un número decimal entre 0.0 y 1.0 (ej: 0.95)

CAMPOS ESPECÍFICOS DE CARNÉ ADUANERO:
- Titulo: ""CARNÉ ADUANERO"" (siempre este valor exacto)
- NombreCompleto: NOMBRE COMPLETO de la persona (nombre + apellidos)
- Rut: Formato completo XX.XXX.XXX-X (NO enmascarar)
- NumeroCarne: Número completo del carné (ej: N8687)
- FechaEmision: Fecha en formato DD.MM.YYYY como aparece
- Resolucion: Número de resolución completo (ej: 01.42)
- ConfianzaExtraccion: Nivel de confianza en la extracción (0.0 a 1.0)

FORMATO DE RESPUESTA:
Responde ÚNICAMENTE con el JSON extraído de la imagen, sin explicaciones adicionales, sin markdown, sin texto extra.
El JSON debe ser válido y parseable inmediatamente.

IMPORTANTE: 
- ConfianzaExtraccion debe ser un número decimal (ej: 0.95), NO un string
- Usa los nombres exactos de campos en PascalCase
- Si un campo no se puede extraer, déjalo como null
- Extrae TODA la información visible en la imagen

EJEMPLO DE RESPUESTA ESPERADA:
{
  ""Titulo"": ""CARNÉ ADUANERO"",
  ""NombreCompleto"": ""GONZALEZ RODRIGUEZ"",
  ""Rut"": ""15.970.128-K"",
  ""NumeroCarne"": ""N8687"",
  ""FechaEmision"": ""2024-01-17"",
  ""Resolucion"": ""01.42"",
  ""ConfianzaExtraccion"": 0.95
}";
        }

        /// <summary>
        /// Crea prompt específico para Documento de Recepción
        /// </summary>
        private string CrearPromptDocumentoRecepcion()
        {
            return @"Eres un asistente experto en procesamiento de documentos chilenos usando Gemma 3: 4B. Tu tarea es analizar una imagen de DOCUMENTO DE RECEPCIÓN (DR) y extraer TODA la información disponible en formato JSON.

INSTRUCCIONES DETALLADAS:
1. Analiza cuidadosamente la imagen del DOCUMENTO DE RECEPCIÓN
2. Identifica TODOS los campos disponibles según el modelo DocumentoRecepcion
3. Extrae la información en formato JSON estructurado con los nombres exactos de campos
4. Para fechas, usa el formato ISO 8601 (YYYY-MM-DD) para fechas simples o (YYYY-MM-DDTHH:mm:ss) para fechas con hora
5. Para números, usa el formato exacto como aparece en el documento
6. IMPORTANTE: ConfianzaExtraccion debe ser un número decimal entre 0.0 y 1.0 (ej: 0.95)

CAMPOS ESPECÍFICOS DE DOCUMENTO DE RECEPCIÓN:
- NumeroDocumento: Número del documento (ej: 2025-10718)
- SituacionDocumento: Situación del documento (ej: NORMAL)
- NumeroManifiesto: Número de manifiesto (ej: 257809)
- FechaManifiestoSna: Fecha del manifiesto SNA (ej: 2025-06-19)
- FechaInicioAlmacenaje: Fecha de inicio de almacenaje (ej: 2025-06-25)
- FechaInicio90Dias: Fecha de inicio de 90 días (ej: 2025-06-20)
- FechaTermino90Dias: Fecha de término de 90 días (ej: 2025-09-17)
- TipoDocumento: Tipo de documento (ej: CONTENEDOR IMPORTACION - POR MANIFIESTO - INDIRECTO)
- BlArmador: BL Armador (ej: BAC0549074/DACA78565)
- Consignatario: Nombre del consignatario (ej: FORUS S A)
- RutConsignatario: RUT del consignatario (ej: 86963200-7)
- DireccionConsignatario: Dirección del consignatario (ej: AV LAS CONDES NRO 11281, BLOCK C - SANTIAGO)
- LineaOperadora: Línea operadora (ej: CMA-CGM CHILE S A)
- ServicioAlmacenaje: Servicio de almacenaje (ej: ALMACENAJE DE CONTENEDOR 40' NORMAL (ZP))
- GuardaAlmacen: Guarda almacén (ej: MELLA JUAN)
- RutGuardaAlmacen: RUT del guarda almacén (ej: 13196396-3)
- PuertoOrigen: Puerto de origen (ej: CHITTAGONG)
- PuertoEmbarque: Puerto de embarque (ej: CHITTAGONG)
- PuertoDescarga: Puerto de descarga (ej: SAN ANTONIO)
- PuertoDestino: Puerto de destino (ej: SAN ANTONIO)
- PuertoTransbordo: Puerto de transbordo (ej: CALLAO)
- NaveViaje: Nave/Viaje (ej: ONE IBIS / 2502)
- Almacen: Almacén (ej: PATIO ZONA PRIMARIA)
- DestinoCarga: Destino de la carga (ej: IMPORTACION)
- Zona: Zona (ej: PRIMARIA)
- Origen: Origen (ej: IMPORTACION)
- TipoBulto: Tipo de bulto (ej: H40 40 CONTENEDOR HIGH CUBE STD)
- Contenedor: Contenedor (ej: MSBU 827710-2)
- Tatc: TATC (ej: 2025391760025982)
- Cantidad: Cantidad (ej: 1)
- Peso: Peso (ej: 17.540,00)
- Volumen: Volumen (ej: 0,00)
- Estado: Estado (ej: BUENO)
- RutEmisor: RUT del emisor (ej: 12452809-7)
- FechaEmision: Fecha de emisión (ej: 2025-06-25T15:10:48)
- MedioEmision: Medio de emisión (ej: WEB)
- Forwarder: Forwarder (ej: NOMBRE FORWARDER)
- AgenciaAduana: Agencia de aduana
- Ubicacion: Ubicación (ej: 10010101)
- Marcas: Marcas (ej: CONSIGNATARIO: JIN & YIN & WANG LTDA N M (GCI #59111-hum1))
- ConfianzaExtraccion: Nivel de confianza en la extracción (0.0 a 1.0)

FORMATO DE RESPUESTA:
Responde ÚNICAMENTE con el JSON extraído de la imagen, sin explicaciones adicionales, sin markdown, sin texto extra.
El JSON debe ser válido y parseable inmediatamente.

IMPORTANTE: 
- ConfianzaExtraccion debe ser un número decimal (ej: 0.95), NO un string
- Usa los nombres exactos de campos en PascalCase
- Si un campo no se puede extraer, déjalo como null
- Extrae TODA la información visible en la imagen

EJEMPLO DE RESPUESTA ESPERADA:
{
  ""NumeroDocumento"": ""2025-10718"",
  ""SituacionDocumento"": ""NORMAL"",
  ""NumeroManifiesto"": ""257809"",
  ""FechaManifiestoSna"": ""2025-06-19"",
  ""FechaInicioAlmacenaje"": ""2025-06-25"",
  ""FechaInicio90Dias"": ""2025-06-20"",
  ""FechaTermino90Dias"": ""2025-09-17"",
  ""TipoDocumento"": ""CONTENEDOR IMPORTACION - POR MANIFIESTO - INDIRECTO"",
  ""BlArmador"": ""BAC0549074/DACA78565"",
  ""Consignatario"": ""FORUS S A"",
  ""RutConsignatario"": ""86963200-7"",
  ""DireccionConsignatario"": ""AV LAS CONDES NRO 11281, BLOCK C - SANTIAGO"",
  ""LineaOperadora"": ""CMA-CGM CHILE S A"",
  ""ServicioAlmacenaje"": ""ALMACENAJE DE CONTENEDOR 40' NORMAL (ZP)"",
  ""GuardaAlmacen"": ""MELLA JUAN"",
  ""RutGuardaAlmacen"": ""13196396-3"",
  ""PuertoOrigen"": ""CHITTAGONG"",
  ""PuertoEmbarque"": ""CHITTAGONG"",
  ""PuertoDescarga"": ""SAN ANTONIO"",
  ""PuertoDestino"": ""SAN ANTONIO"",
  ""PuertoTransbordo"": ""CALLAO"",
  ""NaveViaje"": ""CMA CGM BEIRA / OLISSN1"",
  ""Almacen"": ""PATIO ZONA PRIMARIA"",
  ""DestinoCarga"": ""IMPORTACION"",
  ""Zona"": ""PRIMARIA"",
  ""Origen"": ""IMPORTACION"",
  ""TipoBulto"": ""H40 40 CONTENEDOR HIGH CUBE STD"",
  ""Contenedor"": ""MSBU 827710-2"",
  ""Tatc"": ""2025391760025982"",
  ""Cantidad"": ""1"",
  ""Peso"": ""17.540,00"",
  ""Volumen"": ""0,00"",
  ""Estado"": ""BUENO"",
  ""RutEmisor"": ""12452809-7"",
  ""FechaEmision"": ""2025-06-25T15:10:48"",
  ""MedioEmision"": ""WEB"",
  ""Forwarder"": null,
  ""AgenciaAduana"": null,
  ""Ubicacion"": ""10010101"",
  ""Marcas"": ""CONSIGNATARIO: FORUS S A (GCI #59111-hum1)"",
  ""ConfianzaExtraccion"": 0.95
}";
        }

        /// <summary>
        /// Crea prompt específico para Declaración de Ingreso
        /// </summary>
        private string CrearPromptDeclaracionIngreso()
        {
            return @"Eres un asistente experto en procesamiento de documentos chilenos usando Gemma 3: 4B. Tu tarea es analizar una imagen de DECLARACIÓN DE INGRESO y extraer TODA la información disponible en formato JSON.

INSTRUCCIONES DETALLADAS:
1. Analiza cuidadosamente la imagen de la DECLARACIÓN DE INGRESO
2. Identifica TODOS los campos disponibles según el modelo DeclaracionIngreso
3. Extrae la información en formato JSON estructurado con los nombres exactos de campos
4. Para fechas, usa el formato ISO 8601 (YYYY-MM-DD) para fechas simples o (YYYY-MM-DDTHH:mm:ss) para fechas con hora
5. Para números, usa el formato exacto como aparece en el documento
6. IMPORTANTE: ConfianzaExtraccion debe ser un número decimal entre 0.0 y 1.0 (ej: 0.95)

CAMPOS ESPECÍFICOS DE DECLARACIÓN DE INGRESO:
- NumeroIdentificacion: Número de identificación de la declaración
- FechaVencimiento: Fecha de vencimiento
- TipoOperacion: Tipo de operación
- CodigoTipoOperacion: Código del tipo de operación
- TipoBulto: Tipo de bulto
- PesoBruto: Peso bruto
- SelloContenedor: Sello del contenedor
- FechaAceptacion: Fecha de aceptación
- TotalPagar: Total a pagar
- Aduana: Nombre de la aduana
- Consignatario: Nombre del consignatario
- Consignante: Nombre del consignante
- DocumentoTransporte: Documento de transporte
- ValorCif: Valor CIF
- Manifiesto: Número de manifiesto
- PuertoEmbarque: Puerto de embarque
- PuertoDesembarque: Puerto de desembarque
- CompaniaTransportista: Compañía transportista
- ConfianzaExtraccion: Nivel de confianza en la extracción (0.0 a 1.0)

FORMATO DE RESPUESTA:
Responde ÚNICAMENTE con el JSON extraído de la imagen, sin explicaciones adicionales, sin markdown, sin texto extra.
El JSON debe ser válido y parseable inmediatamente.

IMPORTANTE: 
- ConfianzaExtraccion debe ser un número decimal (ej: 0.95), NO un string
- Usa los nombres exactos de campos en PascalCase
- Si un campo no se puede extraer, déjalo como null
- Extrae TODA la información visible en la imagen";
        }

        /// <summary>
        /// Crea prompt específico para Guía de Despacho
        /// </summary>
        private string CrearPromptGuiaDespacho()
        {
            return @"Eres un asistente experto en procesamiento de documentos chilenos usando Gemma 3: 4B. Tu tarea es analizar una imagen de GUÍA DE DESPACHO y extraer TODA la información disponible en formato JSON.

INSTRUCCIONES DETALLADAS:
1. Analiza cuidadosamente la imagen de la GUÍA DE DESPACHO
2. Identifica TODOS los campos disponibles según el modelo GuiaDespacho
3. Extrae la información en formato JSON estructurado con los nombres exactos de campos
4. Para fechas, usa el formato ISO 8601 (YYYY-MM-DD) para fechas simples o (YYYY-MM-DDTHH:mm:ss) para fechas con hora
5. Para números, usa el formato exacto como aparece en el documento
6. IMPORTANTE: ConfianzaExtraccion debe ser un número decimal entre 0.0 y 1.0 (ej: 0.95)

CAMPOS ESPECÍFICOS DE GUÍA DE DESPACHO (usa estos nombres exactos):
- NumeroGuia: Número de la guía de despacho (ej: 11)
- FechaDocumento: Fecha del documento (ej: 2023-10-26)
- RutEmisor: RUT del emisor (ej: 74.267.890-9)
- NombreEmisor: Nombre o razón social del emisor (ej: GRUPO DE DESPACHO)
- GiroEmisor: Giro del emisor
- DireccionEmisor: Dirección del emisor (ej: AV. SAN FRANCISCO 12345)
- CiudadEmisor: Ciudad del emisor
- RutReceptor: RUT del receptor (ej: 74.267.890-9)
- NombreReceptor: Nombre o razón social del receptor (ej: GRUPO DE DESPACHO)
- GiroReceptor: Giro del receptor
- DireccionReceptor: Dirección del receptor (ej: AV. SAN FRANCISCO 12345)
- CiudadReceptor: Ciudad del receptor
- ComunaReceptor: Comuna del receptor (ej: LASAYUDAS)
- Transportista: Nombre del transportista
- PatenteVehiculo: Patente del vehículo
- Chofer: Nombre del chofer
- Ubicacion: Ubicación
- Origen: Lugar de origen
- Destino: Lugar de destino
- NumeroDespacho: Número de despacho
- FechaDespacho: Fecha de despacho
- Aduana: Nombre de la aduana
- Referencia: Referencia
- TipoOperacion: Tipo de operación
- NumeroDAU: Número DAU
- ConocimientoEmbarque: Conocimiento de embarque
- Manifiesto: Número de manifiesto
- Peso: Peso de la mercancía
- CIFUSD: Valor CIF en USD
- Observaciones: Observaciones adicionales
- CantidadBultos: Cantidad de bultos
- TipoBulto: Tipo de bulto
- Neto: Total neto
- IVA: Total IVA
- Total: Total del documento
- ConfianzaExtraccion: Nivel de confianza en la extracción (0.0 a 1.0)

FORMATO DE RESPUESTA:
Responde ÚNICAMENTE con el JSON extraído de la imagen, sin explicaciones adicionales, sin markdown, sin texto extra.
El JSON debe ser válido y parseable inmediatamente.

IMPORTANTE: 
- ConfianzaExtraccion debe ser un número decimal (ej: 0.95), NO un string
- Usa los nombres exactos de campos en PascalCase como se muestran arriba
- Si un campo no se puede extraer, déjalo como null
- Extrae TODA la información visible en la imagen
- Mapea correctamente: FechaEmision → FechaDocumento, RazonSocialEmisor → NombreEmisor, etc.

EJEMPLO DE RESPUESTA ESPERADA:
{
  ""NumeroGuia"": ""11"",
  ""FechaDocumento"": ""2023-10-26"",
  ""RutEmisor"": ""74.267.890-9"",
  ""NombreEmisor"": ""GRUPO DE DESPACHO"",
  ""GiroEmisor"": """",
  ""DireccionEmisor"": ""AV. SAN FRANCISCO 12345"",
  ""CiudadEmisor"": """",
  ""RutReceptor"": ""74.267.890-9"",
  ""NombreReceptor"": ""GRUPO DE DESPACHO"",
  ""GiroReceptor"": """",
  ""DireccionReceptor"": ""AV. SAN FRANCISCO 12345"",
  ""CiudadReceptor"": """",
  ""ComunaReceptor"": ""LASAYUDAS"",
  ""Transportista"": null,
  ""PatenteVehiculo"": null,
  ""Chofer"": null,
  ""Ubicacion"": null,
  ""Origen"": null,
  ""Destino"": null,
  ""NumeroDespacho"": null,
  ""FechaDespacho"": null,
  ""Aduana"": null,
  ""Referencia"": null,
  ""TipoOperacion"": null,
  ""NumeroDAU"": null,
  ""ConocimientoEmbarque"": null,
  ""Manifiesto"": null,
  ""Peso"": null,
  ""CIFUSD"": null,
  ""Observaciones"": """",
  ""CantidadBultos"": null,
  ""TipoBulto"": """",
  ""Neto"": null,
  ""IVA"": null,
  ""Total"": null,
  ""ConfianzaExtraccion"": 0.95
}";
        }

        /// <summary>
        /// Crea prompt específico para TACT/ADC
        /// </summary>
        private string CrearPromptTactAdc()
        {
            return @"Eres un asistente experto en procesamiento de documentos chilenos usando Gemma 3: 4B. Tu tarea es analizar una imagen de documento y extraer TODA la información disponible en formato JSON.

INSTRUCCIONES DETALLADAS:
1. Analiza cuidadosamente la imagen del documento
2. PRIMERO identifica el tipo de documento basándote en el contenido (no en el nombre del archivo)
3. Luego extrae TODA la información disponible según el tipo de documento identificado
4. Para fechas, usa el formato ISO 8601 (YYYY-MM-DD) para fechas simples o (YYYY-MM-DDTHH:mm:ss) para fechas con hora
5. Para números, usa el formato numérico (NO strings) para campos como peso, volumen, cantidad, etc.
6. IMPORTANTE: ConfianzaExtraccion debe ser un número decimal entre 0.0 y 1.0 (ej: 0.95)
7. ATENCIÓN ESPECIAL: Si encuentras un documento DR que contenga información de TATC, extrae también el NumeroTatc de la sección de sellos o contenedores

TIPOS DE DOCUMENTOS QUE PUEDES IDENTIFICAR:
- **Documento de Recepción (DR)**: Busca campos como N° D.R., Manifiesto, Consignatario, Tipo D.R., BL Armador, etc. 
  *NOTA: Los DR pueden contener información de TATC en la sección de sellos/contenedores*
- **TACT/ADC**: Busca campos como TATC, Compañía, Origen, Destino, etc.
- **Carné Aduanero**: Busca campos como Nombre, RUT, Número de Carné, etc.
- **Guía de Despacho**: Busca campos como Número de Guía, Emisor, Receptor, etc.
- **Declaración de Ingreso**: Busca campos como Número de Identificación, Tipo de Operación, etc.

CAMPOS ESPECÍFICOS PARA DOCUMENTO DE RECEPCIÓN (DR) - USA ESTOS NOMBRES EXACTOS:
- NumeroTatc: Número TATC (ej: 2025391250035445) - IMPORTANTE: Buscar en la sección de sellos o contenedores
- NumeroContenedor: Número del contenedor (ej: CMAU 650120-0)
- NumeroSellos: Número de sellos (ej: M0442444)
- EmpresaEmisora: Empresa emisora del documento (ej: MEDLOG CHILE EXTRAPORTUARIOS LIMITADA)
- DireccionEmpresa: Dirección de la empresa (ej: CAMINO LAS MERCEDES 14851 - SANTIAGO)
- RutEmisor: RUT del emisor (ej: 12452809-7)
- FechaEmision: Fecha de emisión del documento (ej: 2025-06-25) - DEBE SER FECHA ISO 8601
- TipoDocumento: Tipo de documento (ej: CONTENEDOR IMPORTACION - POR MANIFIESTO - INDIRECTO)
- BlArmador: BL Armador (ej: (M)BAC0549074 / (H)DACA78565)
- Consignatario: Nombre del consignatario (ej: FORUS S A)
- RutConsignatario: RUT del consignatario (ej: 86963200-7)
- DireccionConsignatario: Dirección del consignatario (ej: AV LAS CONDES NRO 11281, BLOCK C - SANTIAGO)
- Forwarder: Forwarder (ej: vacío si no hay)
- LineaOperadora: Línea operadora (ej: CMA-CGM CHILE S A)
- ServicioAlmacenaje: Servicio de almacenaje (ej: ALMACENAJE DE CONTENEDOR 40 NORMAL)
- GuardaAlmacen: Guarda almacén (ej: MELLA JUAN)
- RutGuardaAlmacen: RUT del guarda almacén (ej: 13196396-3)
- PuertoOrigen: Puerto de origen (ej: CHITTAGONG)
- PuertoDescarga: Puerto de descarga (ej: SAN ANTONIO)
- PuertoEmbarque: Puerto de embarque (ej: CHITTAGONG)
- PuertoDestino: Puerto de destino (ej: SAN ANTONIO)
- PuertoTransbordo: Puerto de transbordo (ej: CALLAO)
- TipoBulto: Tipo de bulto (ej: (H40) 40' CONTENEDOR HIGH CUBE STD)
- Cantidad: Cantidad de bultos (ej: 1) - DEBE SER NÚMERO
- Peso: Peso en kg (ej: 5384.03) - DEBE SER NÚMERO
- Volumen: Volumen en m³ (ej: 0.00) - DEBE SER NÚMERO
- Estado: Estado del bulto (ej: BUENO)

CAMPOS ESPECÍFICOS PARA TACT/ADC:
- NumeroTact: Número TACT (ej: 123456)
- FechaEmision: Fecha de emisión (ej: 2025-01-15)
- Compania: Compañía (ej: MAERSK, MSC, IANTAYLOR)
- Origen: Puerto de origen (ej: SHANGHAI)
- Destino: Puerto de destino (ej: SAN ANTONIO)
- Consignatario: Nombre del consignatario (ej: IMPORTADORA ABC)
- Consignante: Nombre del consignante (ej: EXPORTADORA XYZ)
- DescripcionMercancia: Descripción de la mercancía (ej: ELECTRONICOS)
- Peso: Peso de la mercancía en kg (ej: 1500.5) - DEBE SER NÚMERO
- Volumen: Volumen de la mercancía en m³ (ej: 25.8) - DEBE SER NÚMERO
- CantidadBultos: Cantidad de bultos (ej: 50) - DEBE SER NÚMERO
- TipoBulto: Tipo de bulto (ej: CAJAS)
- ValorMercancia: Valor de la mercancía (ej: 50000) - DEBE SER NÚMERO
- Moneda: Moneda del valor (ej: USD)

FORMATO DE RESPUESTA:
Responde ÚNICAMENTE con el JSON extraído de la imagen, sin explicaciones adicionales, sin markdown, sin texto extra.
El JSON debe ser válido y parseable inmediatamente.

IMPORTANTE: 
- Los campos numéricos (peso, volumen, cantidad, etc.) DEBEN SER NÚMEROS, NO strings
- **USA ÚNICAMENTE los nombres exactos de campos que se muestran arriba** - NO cambies los nombres
- Si un campo no se puede extraer, déjalo como null
- Extrae TODA la información visible en la imagen
- NO inventes información que no esté en el documento
- **Para documentos DR, mapea la información a los campos del modelo TactAdc usando los nombres exactos**
- **El campo NumeroTatc es CRÍTICO** - búscalo en la sección de sellos/contenedores";
        }

        /// <summary>
        /// Crea prompt específico para Selección de Aforo
        /// </summary>
        private string CrearPromptSeleccionAforo()
        {
            return @"Eres un asistente experto en procesamiento de documentos chilenos usando Gemma 3: 4B. Tu tarea es analizar una imagen de COMPROBANTE DE TIPO DE SELECCIÓN DE AFORO y extraer TODA la información disponible en formato JSON.

INSTRUCCIONES DETALLADAS:
1. Analiza cuidadosamente la imagen del COMPROBANTE DE TIPO DE SELECCIÓN DE AFORO
2. Identifica TODOS los campos disponibles según el modelo SeleccionAforo
3. Extrae la información en formato JSON estructurado con los nombres exactos de campos
4. Para fechas, usa el formato ISO 8601 (YYYY-MM-DD) para fechas simples o (YYYY-MM-DDTHH:mm:ss) para fechas con hora
5. Para números, usa el formato exacto como aparece en el documento
6. IMPORTANTE: ConfianzaExtraccion debe ser un número decimal entre 0.0 y 1.0 (ej: 0.95)

CAMPOS ESPECÍFICOS DE SELECCIÓN DE AFORO (usa estos nombres exactos):
- NumeroDin: Número DIN (ej: 2120204867)
- FechaAceptacion: Fecha de aceptación (ej: 2025-02-28)
- NumeroEncriptado: Número encriptado (ej: 47144496)
- CodigoAgente: Código del agente (ej: B12)
- NombreAgente: Nombre del agente (ej: ALEXIS MONTENEGRO P)
- CodigoAduana: Código de la aduana (ej: 39)
- NombreAduana: Nombre de la aduana (ej: SAN ANTONIO)
- TipoRevision: Tipo de revisión (ej: FISICO)
- NombreFirmante: Nombre del firmante (ej: GONZALO GONZÁLEZ PINO)
- RutFirmante: RUT del firmante (ej: 15.970.128-K)
- NumeroAgencia: Número de agencia (ej: 8687)
- NombreAgencia: Nombre de la agencia (ej: AGENCIA ALEXIS MONTENEGRO)
- ConfianzaExtraccion: Nivel de confianza en la extracción (0.0 a 1.0)

FORMATO DE RESPUESTA:
Responde ÚNICAMENTE con el JSON extraído de la imagen, sin explicaciones adicionales, sin markdown, sin texto extra.
El JSON debe ser válido y parseable inmediatamente.

IMPORTANTE: 
- ConfianzaExtraccion debe ser un número decimal (ej: 0.95), NO un string
- Usa los nombres exactos de campos en PascalCase como se muestran arriba
- Si un campo no se puede extraer, déjalo como null
- Extrae TODA la información visible en la imagen
- Mapea correctamente los campos según el documento: Nro. DIN → NumeroDin, Fecha de Aceptación → FechaAceptacion, etc.

EJEMPLO DE RESPUESTA ESPERADA:
{
  ""NumeroDin"": ""2120204867"",
  ""FechaAceptacion"": ""2025-02-28"",
  ""NumeroEncriptado"": ""47144496"",
  ""CodigoAgente"": ""B12"",
  ""NombreAgente"": ""ALEXIS MONTENEGRO P"",
  ""CodigoAduana"": ""39"",
  ""NombreAduana"": ""SAN ANTONIO"",
  ""TipoRevision"": ""FISICO"",
  ""NombreFirmante"": ""GONZALO GONZÁLEZ PINO"",
  ""RutFirmante"": ""15.970.128-K"",
  ""NumeroAgencia"": ""8687"",
  ""NombreAgencia"": ""AGENCIA ALEXIS MONTENEGRO"",
  ""ConfianzaExtraccion"": 0.95
}";
        }

        /// <summary>
        /// Crea prompt genérico para documentos no identificados
        /// </summary>
        private string CrearPromptGenerico()
        {
            return @"Eres un asistente experto en procesamiento de documentos chilenos usando Gemma 3: 4B. Tu tarea es analizar una imagen de documento y extraer TODA la información disponible en formato JSON.

INSTRUCCIONES DETALLADAS:
1. Analiza cuidadosamente la imagen del documento
2. PRIMERO identifica el tipo de documento basándote en el contenido (no en el nombre del archivo)
3. Luego extrae TODA la información disponible según el tipo de documento identificado
4. Para fechas, usa el formato ISO 8601 (YYYY-MM-DD) para fechas simples o (YYYY-MM-DDTHH:mm:ss) para fechas con hora
5. Para números, usa el formato numérico (NO strings) para campos como peso, volumen, cantidad, etc.
6. Para monedas, usa el formato numérico sin símbolos de moneda
7. IMPORTANTE: ConfianzaExtraccion debe ser un número decimal entre 0.0 y 1.0 (ej: 0.95)

TIPOS DE DOCUMENTOS QUE PUEDES IDENTIFICAR:
- **Documento de Recepción (DR)**: Busca campos como N° D.R., Manifiesto, Consignatario, Tipo D.R., BL Armador, etc.
- **TACT/ADC**: Busca campos como TATC, Compañía, Origen, Destino, etc.
- **Carné Aduanero**: Busca campos como Nombre, RUT, Número de Carné, etc.
- **Guía de Despacho**: Busca campos como Número de Guía, Emisor, Receptor, etc.
- **Declaración de Ingreso**: Busca campos como Número de Identificación, Tipo de Operación, etc.

CAMPOS ESPECÍFICOS PARA DOCUMENTO DE RECEPCIÓN (DR):
- NumeroDocumento: Número del DR (ej: 2025-10718)
- SituacionDocumento: Situación del documento (ej: NORMAL)
- NumeroManifiesto: Número de manifiesto (ej: 257809)
- FechaManifiestoSna: Fecha del manifiesto SNA (ej: 2025-06-19)
- FechaInicioAlmacenaje: Fecha de inicio de almacenaje (ej: 2025-06-25)
- FechaInicio90Dias: Fecha de inicio de 90 días (ej: 2025-06-20)
- FechaTermino90Dias: Fecha de término de 90 días (ej: 2025-09-17)
- TipoDocumento: Tipo de documento (ej: CONTENEDOR IMPORTACION - POR MANIFIESTO - INDIRECTO)
- BlArmador: BL Armador (ej: (M)BAC0549074 / (H)DACA78565)
- Consignatario: Nombre del consignatario (ej: FORUS S A)
- RutConsignatario: RUT del consignatario (ej: 86963200-7)
- DireccionConsignatario: Dirección del consignatario (ej: AV LAS CONDES NRO 11281, BLOCK C - SANTIAGO)
- LineaOperadora: Línea operadora (ej: CMA-CGM CHILE S A)
- PuertoOrigen: Puerto de origen (ej: CHITTAGONG)
- PuertoDestino: Puerto de destino (ej: SAN ANTONIO)
- TipoBulto: Tipo de bulto (ej: (H40) 40' CONTENEDOR HIGH CUBE STD)
- Cantidad: Cantidad de bultos (ej: 1)
- Peso: Peso en kg (ej: 5384.03) - DEBE SER NÚMERO
- Volumen: Volumen en m³ (ej: 0.00) - DEBE SER NÚMERO
- Estado: Estado del bulto (ej: BUENO)
- NumeroContenedor: Número del contenedor (ej: CMAU 650120-0)
- NumeroSellos: Número de sellos (ej: M0442444)
- NumeroTatc: Número TATC (ej: 2025391250035445)

FORMATO DE RESPUESTA:
Responde ÚNICAMENTE con el JSON extraído de la imagen, sin explicaciones adicionales, sin markdown, sin texto extra.
El JSON debe ser válido y parseable inmediatamente.

IMPORTANTE: 
- Los campos numéricos (peso, volumen, cantidad, etc.) DEBEN SER NÚMEROS, NO strings
- Usa nombres de campos descriptivos en PascalCase que coincidan con el tipo de documento
- Si un campo no se puede extraer, déjalo como null
- Extrae TODA la información visible en la imagen
- NO inventes información que no esté en el documento";
        }

        /// <summary>
        /// Crea un prompt optimizado para Ollama con Gemma 3: 4B
        /// </summary>
        private string CrearPromptMejorado(string documentoJson, string textoOcr)
        {
            return $@"Eres un asistente experto en procesamiento de documentos chilenos usando Gemma 3: 4B. Tu tarea es analizar un JSON y completar todos los campos faltantes basándote en el texto extraído por OCR.

DOCUMENTO JSON ACTUAL:
{documentoJson}

TEXTO EXTRAÍDO POR OCR:
{textoOcr}

INSTRUCCIONES DETALLADAS:
1. Analiza cuidadosamente el texto OCR para identificar TODOS los campos disponibles
2. Completa SOLO los campos que estén vacíos, sean null, o contengan valores por defecto
3. Mantén el formato JSON exacto y la estructura original
4. Para fechas, usa el formato ISO 8601 (YYYY-MM-DDTHH:mm:ss) o (YYYY-MM-DD) según el contexto
5. Para números, usa el formato decimal sin comas ni puntos de miles
6. Para monedas, usa el formato numérico sin símbolos de moneda
7. NO modifiques campos que ya tengan valores válidos
8. Si un campo no se puede extraer del texto, déjalo como null
9. Usa tu capacidad de razonamiento para inferir campos relacionados

TIPOS DE DOCUMENTOS CHILENOS QUE PUEDES PROCESAR:
- CARNÉ ADUANERO: Nombre, RUT, número de carné, fecha emisión, resolución
- COMPROBANTE DE TRANSACCIÓN: Folio, monto, fechas, institución, identificador
- DOCUMENTO DE RECEPCIÓN (DR): Número DR, situación, contenedor, TATC, peso, volumen
- DECLARACIÓN DE INGRESO (DI): Número identificación, campos críticos y adicionales
- GUÍA DE DESPACHO: Número guía, destinatario, dirección, mercancía
- TACT/ADC: Número TATC, autorización, contenedores, fechas
- SELECCIÓN DE AFORO: Número selección, tipo, resultado, observaciones

CAMPOS COMUNES A BUSCAR EN EL TEXTO OCR:
- Números de documento, folio, formulario, identificación
- Fechas (emisión, vencimiento, pago, recepción, etc.)
- Montos y valores monetarios (totales, pagos, cargos)
- Nombres de instituciones, empresas, personas, destinatarios
- Números de identificación (RUT, pasaporte, carné, etc.)
- Direcciones y ubicaciones (origen, destino, puerto)
- Códigos y referencias (barras, QR, internos)
- Cantidades, pesos, volúmenes, unidades
- Estados y situaciones (normal, pendiente, aprobado)
- Comentarios y observaciones adicionales

FORMATO DE RESPUESTA:
Responde ÚNICAMENTE con el JSON completado, sin explicaciones adicionales, sin markdown, sin texto extra.
El JSON debe ser válido y parseable inmediatamente.";
        }

        /// <summary>
        /// Llama a Ollama usando su API REST
        /// </summary>
        private async Task<string> LlamarOllamaAsync(string prompt)
        {
            try
            {
                _logger.LogInformation("Llamando a Ollama con modelo {Modelo} para procesar documento", _modeloOllama);

                // Crear el payload para Ollama
                var payload = new
                {
                    model = _modeloOllama,
                    prompt = prompt,
                    stream = false,
                    options = new
                    {
                        temperature = 0.1, // Temperatura baja para respuestas consistentes
                        top_p = 0.9,
                        top_k = 20,
                        max_tokens = 2000,
                        repeat_penalty = 1.05,
                        num_predict = 2000
                    }
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogDebug("Payload enviado a Ollama: {Payload}", json);

                // Llamar a la API de Ollama
                var response = await _httpClient.PostAsync($"{_ollamaBaseUrl}/api/generate", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogDebug("Respuesta de Ollama: {Respuesta}", responseContent);
                    
                    var ollamaResponse = JsonSerializer.Deserialize<OllamaResponse>(responseContent);
                    return ollamaResponse?.response ?? string.Empty;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Ollama respondió con código de error: {StatusCode}, Error: {Error}", response.StatusCode, errorContent);
                    return string.Empty;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error llamando a Ollama con modelo {Modelo}", _modeloOllama);
                return string.Empty;
            }
        }

        /// <summary>
        /// Llama a Ollama con una imagen para análisis directo
        /// </summary>
        private async Task<string> LlamarOllamaConImagenAsync(string prompt, byte[] imagenBytes, string nombreArchivo)
        {
            const int maxReintentos = 2;
            const int timeoutSegundos = 180; // 3 minutos por intento
            
            for (int intento = 1; intento <= maxReintentos; intento++)
            {
                try
                {
                    _logger.LogInformation("Intento {Intento}/{MaxReintentos}: Llamando a Ollama con imagen usando modelo {Modelo} para procesar: {Archivo}", 
                        intento, maxReintentos, _modeloOllama, nombreArchivo);

                    // Verificar que Ollama esté disponible antes de procesar
                    if (!await VerificarOllamaDisponibleAsync())
                    {
                        _logger.LogWarning("Ollama no está disponible en {Url}", _ollamaBaseUrl);
                        return string.Empty;
                    }

                    // Convertir imagen a base64
                    var imagenBase64 = Convert.ToBase64String(imagenBytes);
                    var extension = Path.GetExtension(nombreArchivo).ToLowerInvariant();
                    var mimeType = ObtenerMimeType(extension);

                    // Crear el payload para Ollama con imagen
                    var payload = new
                    {
                        model = _modeloOllama,
                        prompt = prompt,
                        images = new[] { imagenBase64 },
                        stream = false,
                        options = new
                        {
                            temperature = 0.1, // Temperatura baja para respuestas consistentes
                            top_p = 0.9,
                            top_k = 20,
                            max_tokens = 2000, // Reducir tokens para acelerar
                            repeat_penalty = 1.05,
                            num_predict = 2000
                        }
                    };

                    var json = JsonSerializer.Serialize(payload);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    _logger.LogDebug("Payload enviado a Ollama con imagen: {Payload}", json);

                    // Crear un CancellationTokenSource con timeout personalizado
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSegundos));
                    
                    // Llamar a la API de Ollama con timeout personalizado
                    var response = await _httpClient.PostAsync($"{_ollamaBaseUrl}/api/generate", content, cts.Token);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        _logger.LogDebug("Respuesta de Ollama con imagen: {Respuesta}", responseContent);
                        
                        var ollamaResponse = JsonSerializer.Deserialize<OllamaResponse>(responseContent);
                        return ollamaResponse?.response ?? string.Empty;
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        _logger.LogWarning("Ollama respondió con código de error: {StatusCode}, Error: {Error}", response.StatusCode, errorContent);
                        
                        if (intento < maxReintentos)
                        {
                            _logger.LogInformation("Reintentando en 2 segundos...");
                            await Task.Delay(2000, cts.Token);
                            continue;
                        }
                        
                        return string.Empty;
                    }
                }
                catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
                {
                    _logger.LogWarning("Timeout en intento {Intento}/{MaxReintentos} para {Archivo}: {Timeout}s", 
                        intento, maxReintentos, nombreArchivo, timeoutSegundos);
                    
                    if (intento < maxReintentos)
                    {
                        _logger.LogInformation("Reintentando en 5 segundos...");
                        await Task.Delay(5000);
                        continue;
                    }
                    
                    _logger.LogError(ex, "Timeout después de {MaxReintentos} intentos para {Archivo}", maxReintentos, nombreArchivo);
                    return string.Empty;
                }
                catch (TaskCanceledException ex)
                {
                    _logger.LogWarning("Operación cancelada en intento {Intento}/{MaxReintentos} para {Archivo}", 
                        intento, maxReintentos, nombreArchivo);
                    
                    if (intento < maxReintentos)
                    {
                        _logger.LogInformation("Reintentando en 3 segundos...");
                        await Task.Delay(3000);
                        continue;
                    }
                    
                    _logger.LogError(ex, "Operación cancelada después de {MaxReintentos} intentos para {Archivo}", maxReintentos, nombreArchivo);
                    return string.Empty;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error en intento {Intento}/{MaxReintentos} llamando a Ollama con imagen usando modelo {Modelo} para {Archivo}", 
                        intento, maxReintentos, _modeloOllama, nombreArchivo);
                    
                    if (intento < maxReintentos)
                    {
                        _logger.LogInformation("Reintentando en 3 segundos...");
                        await Task.Delay(3000);
                        continue;
                    }
                    
                    return string.Empty;
                }
            }
            
            return string.Empty;
        }

        /// <summary>
        /// Procesa la respuesta de Ollama y actualiza el JSON
        /// </summary>
        private string ProcesarRespuestaOllama(string respuestaOllama, string documentoOriginal)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(respuestaOllama))
                {
                    _logger.LogWarning("Respuesta de Ollama vacía, devolviendo documento original");
                    return documentoOriginal;
                }

                // Limpiar la respuesta de Ollama (eliminar markdown si existe)
                var jsonLimpio = LimpiarRespuestaOllama(respuestaOllama);

                _logger.LogDebug("JSON limpio de Ollama: {JsonLimpio}", jsonLimpio);

                // Intentar parsear la respuesta como JSON
                try
                {
                    using var jsonDoc = JsonDocument.Parse(jsonLimpio);
                    // Combinar el documento original con la respuesta de Ollama
                    var resultado = CombinarDocumentos(documentoOriginal, jsonLimpio);
                    _logger.LogInformation("Documento combinado exitosamente con respuesta de Ollama");
                    return resultado;
                }
                catch (JsonException)
                {
                    _logger.LogWarning("La respuesta de Ollama no es un JSON válido: {Respuesta}", respuestaOllama);
                    return documentoOriginal;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando respuesta de Ollama");
                return documentoOriginal;
            }
        }

        /// <summary>
        /// Procesa la respuesta de la imagen de Ollama
        /// </summary>
        private string ProcesarRespuestaImagen(string respuestaOllama, string nombreArchivo)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(respuestaOllama))
                {
                    _logger.LogWarning("Respuesta de Ollama vacía para imagen: {Archivo}", nombreArchivo);
                    return CrearJsonVacio();
                }

                // Limpiar la respuesta de Ollama (eliminar markdown si existe)
                var jsonLimpio = LimpiarRespuestaOllama(respuestaOllama);

                _logger.LogDebug("JSON limpio de imagen de Ollama: {JsonLimpio}", jsonLimpio);

                // Intentar parsear la respuesta como JSON
                try
                {
                    using var jsonDoc = JsonDocument.Parse(jsonLimpio);
                    _logger.LogInformation("Respuesta de imagen procesada exitosamente por Ollama");
                    return jsonLimpio;
                }
                catch (JsonException)
                {
                    _logger.LogWarning("La respuesta de imagen de Ollama no es un JSON válido: {Respuesta}", respuestaOllama);
                    return CrearJsonVacio();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando respuesta de imagen de Ollama: {Archivo}", nombreArchivo);
                return CrearJsonVacio();
            }
        }

        /// <summary>
        /// Crea un JSON vacío para casos de error
        /// </summary>
        private string CrearJsonVacio()
        {
            return @"{
  ""Titulo"": """",
  ""NombreCompleto"": """",
  ""Rut"": """",
  ""NumeroCarne"": """",
  ""FechaEmision"": """",
  ""Resolucion"": """",
  ""ConfianzaExtraccion"": 0.0
}";
        }

        /// <summary>
        /// Crea un JSON vacío con información de error
        /// </summary>
        private string CrearJsonVacioConError(string mensajeError)
        {
            return $@"{{
  ""Error"": ""{mensajeError}"",
  ""ConfianzaExtraccion"": 0.0,
  ""Timestamp"": ""{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss}Z""
}}";
        }

        /// <summary>
        /// Obtiene el MIME type basado en la extensión del archivo
        /// </summary>
        private string ObtenerMimeType(string extension)
        {
            return extension switch
            {
                ".png" => "image/png",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                ".tiff" => "image/tiff",
                ".webp" => "image/webp",
                _ => "image/png"
            };
        }

        /// <summary>
        /// Limpia la respuesta de Ollama eliminando markdown y texto extra
        /// </summary>
        private string LimpiarRespuestaOllama(string respuesta)
        {
            if (string.IsNullOrWhiteSpace(respuesta))
                return string.Empty;

                // Eliminar markdown si existe
            var limpio = respuesta.Replace("```json", "").Replace("```", "").Trim();
            
            // Buscar el primer { y el último } para extraer solo el JSON
                var inicio = limpio.IndexOf('{');
                var fin = limpio.LastIndexOf('}');
                
                if (inicio >= 0 && fin > inicio)
                {
                return limpio.Substring(inicio, fin - inicio + 1);
                }

                return limpio;
        }

        /// <summary>
        /// Combina el documento original con la respuesta de Ollama
        /// </summary>
        private string CombinarDocumentos(string documentoOriginal, string respuestaOllama)
        {
            try
            {
                // Por ahora, simplemente devolvemos la respuesta de Ollama
                // En el futuro se puede implementar una lógica más sofisticada de combinación
                return respuestaOllama;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error combinando documentos");
                return documentoOriginal;
            }
        }

        /// <summary>
        /// Verifica que Ollama esté disponible y respondiendo
        /// </summary>
        private async Task<bool> VerificarOllamaDisponibleAsync()
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10)); // 10 segundos para verificar
                
                var response = await _httpClient.GetAsync($"{_ollamaBaseUrl}/api/tags", cts.Token);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogDebug("Error verificando disponibilidad de Ollama: {Error}", ex.Message);
                    return false;
            }
        }

        /// <summary>
        /// Clase para deserializar la respuesta de Ollama
        /// </summary>
        private class OllamaResponse
        {
            public string response { get; set; } = string.Empty;
        }
    }
}
