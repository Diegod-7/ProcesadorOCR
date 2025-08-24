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

                // Detectar el tipo de documento basado en el nombre del archivo o contenido
                var tipoDocumento = DetectarTipoDocumento(nombreArchivo);

                // Crear el prompt específico para el tipo de documento
                var prompt = CrearPromptParaImagen(tipoDocumento);

                // Llamar a Ollama con la imagen
                var respuesta = await LlamarOllamaConImagenAsync(prompt, imagenBytes, nombreArchivo);

                // Procesar la respuesta de la imagen
                var documentoExtraido = ProcesarRespuestaImagen(respuesta, nombreArchivo);

                _logger.LogInformation("Procesamiento de imagen completado exitosamente con Gemma 3: 4B para tipo: {Tipo}", tipoDocumento);
                return documentoExtraido;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en procesamiento directo de imagen con Gemma 3: 4B: {Archivo}", nombreArchivo);
                return CrearJsonVacio(); // Devolver JSON vacío si hay error
            }
        }

        /// <summary>
        /// Detecta el tipo de documento basado en el nombre del archivo o contenido
        /// </summary>
        private string DetectarTipoDocumento(string nombreArchivo)
        {
            var nombreLower = nombreArchivo.ToLower();
            
            if (nombreLower.Contains("carne") || nombreLower.Contains("aduanero"))
                return "CarnetAduanero";
            else if (nombreLower.Contains("comprobante") || nombreLower.Contains("transaccion") || nombreLower.Contains("tesoreria"))
                return "ComprobanteTransaccion";
            else if (nombreLower.Contains("recepcion") || nombreLower.Contains("dr"))
                return "DocumentoRecepcion";
            else if (nombreLower.Contains("declaracion") || nombreLower.Contains("ingreso") || nombreLower.Contains("di"))
                return "DeclaracionIngreso";
            else if (nombreLower.Contains("guia") || nombreLower.Contains("despacho"))
                return "GuiaDespacho";
            else if (nombreLower.Contains("tact") || nombreLower.Contains("adc"))
                return "TactAdc";
            else if (nombreLower.Contains("aforo") || nombreLower.Contains("seleccion"))
                return "SeleccionAforo";
            else
                return "Generico"; // Tipo genérico por defecto
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
4. Para fechas, usa el formato exacto como aparece en el documento
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
  ""FechaVencimiento"": ""09-07-2025"",
  ""MonedaPago"": ""CLP"",
  ""FechaPago"": ""24-06-2025 17:44:12"",
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
4. Para fechas, usa el formato DD.MM.YYYY como aparece en el documento
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
  ""FechaEmision"": ""17.01.2024"",
  ""Resolucion"": ""01.42"",
  ""ConfianzaExtraccion"": 0.95
}";
        }

        /// <summary>
        /// Crea prompt específico para Documento de Recepción
        /// </summary>
        private string CrearPromptDocumentoRecepcion()
        {
            return @"Eres un asistente experto en procesamiento de documentos chilenos usando Gemma 3: 4B. Tu tarea es analizar una imagen de DOCUMENTO DE RECEPCIÓN y extraer TODA la información disponible en formato JSON.

INSTRUCCIONES DETALLADAS:
1. Analiza cuidadosamente la imagen del DOCUMENTO DE RECEPCIÓN
2. Identifica TODOS los campos disponibles según el modelo DocumentoRecepcion
3. Extrae la información en formato JSON estructurado con los nombres exactos de campos
4. Para fechas, usa el formato exacto como aparece en el documento
5. Para números, usa el formato exacto como aparece en el documento
6. IMPORTANTE: ConfianzaExtraccion debe ser un número decimal entre 0.0 y 1.0 (ej: 0.95)

CAMPOS ESPECÍFICOS DE DOCUMENTO DE RECEPCIÓN:
- NumeroIdentificacion: Número de identificación del documento
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
        /// Crea prompt específico para Declaración de Ingreso
        /// </summary>
        private string CrearPromptDeclaracionIngreso()
        {
            return @"Eres un asistente experto en procesamiento de documentos chilenos usando Gemma 3: 4B. Tu tarea es analizar una imagen de DECLARACIÓN DE INGRESO y extraer TODA la información disponible en formato JSON.

INSTRUCCIONES DETALLADAS:
1. Analiza cuidadosamente la imagen de la DECLARACIÓN DE INGRESO
2. Identifica TODOS los campos disponibles según el modelo DeclaracionIngreso
3. Extrae la información en formato JSON estructurado con los nombres exactos de campos
4. Para fechas, usa el formato exacto como aparece en el documento
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
4. Para fechas, usa el formato exacto como aparece en el documento
5. Para números, usa el formato exacto como aparece en el documento
6. IMPORTANTE: ConfianzaExtraccion debe ser un número decimal entre 0.0 y 1.0 (ej: 0.95)

CAMPOS ESPECÍFICOS DE GUÍA DE DESPACHO:
- NumeroGuia: Número de la guía de despacho
- FechaEmision: Fecha de emisión
- RazonSocialEmisor: Razón social del emisor
- RutEmisor: RUT del emisor
- DireccionEmisor: Dirección del emisor
- ComunaEmisor: Comuna del emisor
- RazonSocialReceptor: Razón social del receptor
- RutReceptor: RUT del receptor
- DireccionReceptor: Dirección del receptor
- ComunaReceptor: Comuna del receptor
- CondicionesVenta: Condiciones de venta
- FormaPago: Forma de pago
- FechaVencimiento: Fecha de vencimiento
- TotalNeto: Total neto
- TotalIva: Total IVA
- TotalDocumento: Total del documento
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
        /// Crea prompt específico para TACT/ADC
        /// </summary>
        private string CrearPromptTactAdc()
        {
            return @"Eres un asistente experto en procesamiento de documentos chilenos usando Gemma 3: 4B. Tu tarea es analizar una imagen de documento TACT/ADC y extraer TODA la información disponible en formato JSON.

INSTRUCCIONES DETALLADAS:
1. Analiza cuidadosamente la imagen del documento TACT/ADC
2. Identifica TODOS los campos disponibles según el modelo TactAdc
3. Extrae la información en formato JSON estructurado con los nombres exactos de campos
4. Para fechas, usa el formato exacto como aparece en el documento
5. Para números, usa el formato exacto como aparece en el documento
6. IMPORTANTE: ConfianzaExtraccion debe ser un número decimal entre 0.0 y 1.0 (ej: 0.95)

CAMPOS ESPECÍFICOS DE TACT/ADC:
- NumeroTact: Número TACT
- FechaEmision: Fecha de emisión
- Compania: Compañía (MAERSK, MSC, IANTAYLOR, etc.)
- Origen: Puerto de origen
- Destino: Puerto de destino
- Consignatario: Nombre del consignatario
- Consignante: Nombre del consignante
- DescripcionMercancia: Descripción de la mercancía
- Peso: Peso de la mercancía
- Volumen: Volumen de la mercancía
- CantidadBultos: Cantidad de bultos
- TipoBulto: Tipo de bulto
- ValorMercancia: Valor de la mercancía
- Moneda: Moneda del valor
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
        /// Crea prompt específico para Selección de Aforo
        /// </summary>
        private string CrearPromptSeleccionAforo()
        {
            return @"Eres un asistente experto en procesamiento de documentos chilenos usando Gemma 3: 4B. Tu tarea es analizar una imagen de SELECCIÓN DE AFORO y extraer TODA la información disponible en formato JSON.

INSTRUCCIONES DETALLADAS:
1. Analiza cuidadosamente la imagen de la SELECCIÓN DE AFORO
2. Identifica TODOS los campos disponibles según el modelo SeleccionAforo
3. Extrae la información en formato JSON estructurado con los nombres exactos de campos
4. Para fechas, usa el formato exacto como aparece en el documento
5. Para números, usa el formato exacto como aparece en el documento
6. IMPORTANTE: ConfianzaExtraccion debe ser un número decimal entre 0.0 y 1.0 (ej: 0.95)

CAMPOS ESPECÍFICOS DE SELECCIÓN DE AFORO:
- NumeroSeleccion: Número de selección
- FechaSeleccion: Fecha de selección
- TipoSeleccion: Tipo de selección
- Aduana: Nombre de la aduana
- Consignatario: Nombre del consignatario
- Consignante: Nombre del consignante
- DocumentoTransporte: Documento de transporte
- Manifiesto: Número de manifiesto
- DescripcionMercancia: Descripción de la mercancía
- Peso: Peso de la mercancía
- ValorMercancia: Valor de la mercancía
- Moneda: Moneda del valor
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
        /// Crea prompt genérico para documentos no identificados
        /// </summary>
        private string CrearPromptGenerico()
        {
            return @"Eres un asistente experto en procesamiento de documentos chilenos usando Gemma 3: 4B. Tu tarea es analizar una imagen de documento y extraer TODA la información disponible en formato JSON.

INSTRUCCIONES DETALLADAS:
1. Analiza cuidadosamente la imagen del documento
2. Identifica TODOS los campos disponibles (nombres, números, fechas, montos, etc.)
3. Extrae la información en formato JSON estructurado
4. Para fechas, usa el formato exacto como aparece en el documento
5. Para números, usa el formato exacto como aparece en el documento
6. Para monedas, usa el formato numérico sin símbolos de moneda
7. IMPORTANTE: ConfianzaExtraccion debe ser un número decimal entre 0.0 y 1.0 (ej: 0.95)

CAMPOS COMUNES A BUSCAR EN LA IMAGEN:
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
Responde ÚNICAMENTE con el JSON extraído de la imagen, sin explicaciones adicionales, sin markdown, sin texto extra.
El JSON debe ser válido y parseable inmediatamente.

IMPORTANTE: 
- ConfianzaExtraccion debe ser un número decimal (ej: 0.95), NO un string
- Usa nombres de campos descriptivos en PascalCase
- Si un campo no se puede extraer, déjalo como null
- Extrae TODA la información visible en la imagen";
        }

        // ... resto de métodos existentes ...
    }
}
