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

                // Crear el prompt para análisis de imagen
                var prompt = CrearPromptParaImagen();

                // Llamar a Ollama con la imagen
                var respuesta = await LlamarOllamaConImagenAsync(prompt, imagenBytes, nombreArchivo);

                // Procesar la respuesta de la imagen
                var documentoExtraido = ProcesarRespuestaImagen(respuesta, nombreArchivo);

                _logger.LogInformation("Procesamiento de imagen completado exitosamente con Gemma 3: 4B");
                return documentoExtraido;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en procesamiento directo de imagen con Gemma 3: 4B: {Archivo}", nombreArchivo);
                return CrearJsonVacio(); // Devolver JSON vacío si hay error
            }
        }

        /// <summary>
        /// Crea un prompt optimizado para análisis de imágenes con Gemma 3: 4B
        /// </summary>
        private string CrearPromptParaImagen()
        {
            return @"Eres un asistente experto en procesamiento de documentos chilenos usando Gemma 3: 4B. Tu tarea es analizar una imagen de documento y extraer TODA la información disponible en formato JSON.

INSTRUCCIONES DETALLADAS:
1. Analiza cuidadosamente la imagen del documento
2. Identifica TODOS los campos disponibles (nombres, números, fechas, montos, etc.)
3. Extrae la información en formato JSON estructurado
4. Para fechas, usa el formato DD.MM.YYYY como aparece en el documento
5. Para números, usa el formato exacto como aparece en el documento
6. Para monedas, usa el formato numérico sin símbolos de moneda
7. IMPORTANTE: ConfianzaExtraccion debe ser un número decimal entre 0.0 y 1.0 (ej: 0.95)
8. IMPORTANTE: NO uses comillas para campos numéricos, solo para strings
9. Si un campo no se puede extraer de la imagen, déjalo como null
10. CRÍTICO: Extrae el NOMBRE COMPLETO completo, no solo apellidos
11. CRÍTICO: Extrae el RUT completo con formato XX.XXX.XXX-X
12. CRÍTICO: Extrae TODOS los números de carné, resolución, códigos AGAD, etc.

TIPOS DE DOCUMENTOS CHILENOS QUE PUEDES PROCESAR:
- CARNÉ ADUANERO: 
  * Titulo: ""CARNÉ ADUANERO"" (siempre este valor exacto)
  * NombreCompleto: NOMBRE COMPLETO de la persona (nombre + apellidos)
  * Rut: Formato completo XX.XXX.XXX-X (NO enmascarar)
  * NumeroCarne: Número completo del carné (ej: N8687)
  * FechaEmision: Fecha en formato DD.MM.YYYY como aparece
  * Resolucion: Número de resolución completo (ej: 01.42)
  * Otros campos: Cualquier otro campo visible en la imagen

CAMPOS COMUNES A BUSCAR EN LA IMAGEN:
- Números de documento, folio, formulario, identificación
- Fechas (emisión, vencimiento, pago, recepción, etc.)
- Montos y valores monetarios (totales, pagos, cargos)
- Nombres de instituciones, empresas, personas, destinatarios
- Números de identificación (RUT, pasaporte, carné, etc.)
- Direcciones y ubicaciones (origen, destino, puerto)
- Códigos y referencias (barras, QR, internos, AGAD)
- Cantidades, pesos, volúmenes, unidades
- Estados y situaciones (normal, pendiente, aprobado)
- Comentarios y observaciones adicionales

FORMATO DE RESPUESTA:
Responde ÚNICAMENTE con el JSON extraído de la imagen, sin explicaciones adicionales, sin markdown, sin texto extra.
El JSON debe ser válido y parseable inmediatamente.

IMPORTANTE: ConfianzaExtraccion debe ser un número decimal (ej: 0.95), NO un string.

REGLAS DE EXTRACCIÓN CRÍTICAS:
- Si encuentras ""GONZALEZ"" en la imagen, busca el NOMBRE COMPLETO completo
- Si encuentras ""15.970.128-K"" en la imagen, úsalo exactamente como está
- Si encuentras ""N8687"" en la imagen, úsalo exactamente como está
- Si encuentras ""17.01.2024"" en la imagen, úsalo exactamente como está
- Si encuentras ""01.42"" en la imagen, úsalo exactamente como está
- Si encuentras ""E.1.2"" en la imagen, úsalo exactamente como está
- El título debe ser siempre ""CARNÉ ADUANERO""
- Busca TODOS los códigos AGAD y números de resolución en la imagen
- NO enmascares información, extrae TODO lo que veas
- Para nombres, extrae NOMBRE + APELLIDOS completos
- Para RUT, extrae el formato completo XX.XXX.XXX-X

NOMBRES EXACTOS DE CAMPOS A USAR:
- Titulo (NO titulo)
- NombreCompleto (NO nombre)
- Rut (NO rut)
- NumeroCarne (NO numeroCarne)
- FechaEmision (NO fechaEmision)
- Resolucion (NO resolucion)
- ConfianzaExtraccion (NO confianzaExtraccion)";
        }

        /// <summary>
        /// Llama a Ollama con una imagen para análisis directo
        /// </summary>
        private async Task<string> LlamarOllamaConImagenAsync(string prompt, byte[] imagenBytes, string nombreArchivo)
        {
            try
            {
                _logger.LogInformation("Llamando a Ollama con imagen usando modelo {Modelo} para procesar: {Archivo}", _modeloOllama, nombreArchivo);

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
                        max_tokens = 3000, // Más tokens para análisis de imagen
                        repeat_penalty = 1.05,
                        num_predict = 3000
                    }
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogDebug("Payload enviado a Ollama con imagen: {Payload}", json);

                // Llamar a la API de Ollama
                var response = await _httpClient.PostAsync($"{_ollamaBaseUrl}/api/generate", content);
                
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
                    return string.Empty;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error llamando a Ollama con imagen usando modelo {Modelo}", _modeloOllama);
                return string.Empty;
            }
        }

        /// <summary>
        /// Obtiene el MIME type de una extensión de archivo
        /// </summary>
        private string ObtenerMimeType(string extension)
        {
            return extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                ".tiff" or ".tif" => "image/tiff",
                ".webp" => "image/webp",
                _ => "image/jpeg" // Por defecto
            };
        }

        /// <summary>
        /// Procesa la respuesta de la imagen y extrae el JSON
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

                _logger.LogDebug("JSON limpio extraído de imagen: {JsonLimpio}", jsonLimpio);

                // Intentar parsear la respuesta como JSON
                try
                {
                    using var jsonDoc = JsonDocument.Parse(jsonLimpio);
                    _logger.LogInformation("JSON extraído exitosamente de imagen: {Archivo}", nombreArchivo);
                    return jsonLimpio;
                }
                catch (JsonException)
                {
                    _logger.LogWarning("La respuesta de Ollama no es un JSON válido para imagen: {Archivo}, Respuesta: {Respuesta}", nombreArchivo, respuestaOllama);
                    return CrearJsonVacio();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando respuesta de imagen: {Archivo}", nombreArchivo);
                return CrearJsonVacio();
            }
        }

        /// <summary>
        /// Crea un JSON vacío con estructura básica
        /// </summary>
        private string CrearJsonVacio()
        {
            var jsonVacio = new
            {
                Titulo = "",
                NombreCompleto = "",
                Rut = "",
                NumeroCarne = "",
                FechaEmision = "",
                Resolucion = "",
                ConfianzaExtraccion = 0.0,
                error = "No se pudo extraer información de la imagen"
            };

            return JsonSerializer.Serialize(jsonVacio, new JsonSerializerOptions { WriteIndented = true });
        }

        /// <summary>
        /// Crea un prompt optimizado para Ollama con Gemma 3: 1B
        /// </summary>
        private string CrearPromptMejorado(string documentoJson, string textoOcr)
        {
            return $@"Eres un asistente experto en procesamiento de documentos chilenos usando Gemma 3: 1B. Tu tarea es analizar un JSON y completar todos los campos faltantes basándote en el texto extraído por OCR.

DOCUMENTO JSON ACTUAL:
{documentoJson}

TEXTO EXTRAÍDO POR OCR:
{textoOcr}

INSTRUCCIONES DETALLADAS:
1. Analiza cuidadosamente el texto OCR para identificar TODOS los campos disponibles
2. Completa campos vacíos y CORRIGE campos con formato incorrecto
3. Mantén el formato JSON exacto y la estructura original
4. Para fechas, usa el formato ISO 8601 (YYYY-MM-DDTHH:mm:ss) o (YYYY-MM-DD) según el contexto
5. Para números, usa el formato decimal sin comas ni puntos de miles (ej: 0.85, 1.0, 0.0)
6. Para monedas, usa el formato numérico sin símbolos de moneda
7. REVISA y CORRIGE campos que tengan formato incorrecto comparándolos con el texto OCR
8. Si un campo no se puede extraer del texto, déjalo como null
9. Usa tu capacidad de razonamiento para inferir campos relacionados
10. IMPORTANTE: ConfianzaExtraccion debe ser un número decimal entre 0.0 y 1.0 (ej: 0.85)
11. IMPORTANTE: NO uses comillas para campos numéricos, solo para strings
12. CRÍTICO: Si encuentras información en el texto OCR que mejora un campo existente, CORRÍGELO
13. CRÍTICO: Compara cada campo con el texto OCR para asegurar precisión

TIPOS DE DOCUMENTOS CHILENOS QUE PUEDES PROCESAR:
- CARNÉ ADUANERO: 
  * Titulo: ""CARNÉ ADUANERO"" (siempre este valor exacto)
  * Nombre: Nombre completo de la persona
  * RUT: Formato XX.XXX.XXX-X
  * NumeroCarne: Número del carné (ej: N868)
  * FechaEmision: Fecha en formato DD.MM.YYYY
  * Resolucion: Número de resolución si está disponible
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
El JSON debe ser válido y parseable inmediatamente.

IMPORTANTE: ConfianzaExtraccion debe ser un número decimal (ej: 0.85), NO un string.

REGLAS DE CORRECCIÓN:
- Si encuentras ""GONZALEZ"" en el texto OCR, busca el nombre completo ""ALEX GONZALEZ GONZALEZ""
- Si encuentras ""15.970178K"" en el texto OCR, úsalo exactamente como está
- Si encuentras ""N868"" en el texto OCR, úsalo exactamente como está
- Si encuentras ""17.01.2024"" en el texto OCR, úsalo exactamente como está
- El título debe ser siempre ""CARNÉ ADUANERO""
- Busca números de resolución en el texto OCR si están disponibles";
        }

        /// <summary>
        /// Llama a Ollama usando su API REST con Gemma 3: 1B
        /// </summary>
        private async Task<string> LlamarOllamaAsync(string prompt)
        {
            try
            {
                _logger.LogInformation("Llamando a Ollama con modelo {Modelo} para procesar documento", _modeloOllama);

                // Crear el payload para Ollama optimizado para Gemma 3: 1B
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
                        max_tokens = 2000, // Reducir tokens para modelo más pequeño
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
        /// Limpia la respuesta de Ollama de manera más robusta
        /// </summary>
        private string LimpiarRespuestaOllama(string respuesta)
        {
            try
            {
                // Eliminar markdown si existe
                var limpio = respuesta
                    .Replace("```json", "")
                    .Replace("```", "")
                    .Replace("```JSON", "")
                    .Trim();

                // Buscar el JSON dentro de la respuesta
                var inicio = limpio.IndexOf('{');
                var fin = limpio.LastIndexOf('}');
                
                if (inicio >= 0 && fin > inicio)
                {
                    var jsonExtraido = limpio.Substring(inicio, fin - inicio + 1);
                    _logger.LogDebug("JSON extraído de la respuesta: {JsonExtraido}", jsonExtraido);
                    return jsonExtraido;
                }

                // Si no se encuentra JSON válido, intentar limpiar más
                _logger.LogWarning("No se pudo extraer JSON válido de la respuesta: {Respuesta}", respuesta);
                return limpio;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error limpiando respuesta de Ollama");
                return respuesta;
            }
        }

        /// <summary>
        /// Combina el documento original con la respuesta de Ollama de manera inteligente
        /// </summary>
        private string CombinarDocumentos(string documentoOriginal, string respuestaOllama)
        {
            try
            {
                var original = JsonDocument.Parse(documentoOriginal);
                var respuesta = JsonDocument.Parse(respuestaOllama);

                // Crear un nuevo documento combinando ambos
                var documentoCombinado = new Dictionary<string, object>();

                // Copiar todos los campos del documento original
                foreach (var property in original.RootElement.EnumerateObject())
                {
                    documentoCombinado[property.Name] = property.Value.ValueKind == JsonValueKind.Null ? null : property.Value.GetRawText();
                }

                // Actualizar campos vacíos y corregir campos con formato incorrecto
                var camposActualizados = 0;
                foreach (var property in respuesta.RootElement.EnumerateObject())
                {
                    if (documentoCombinado.ContainsKey(property.Name))
                    {
                        var valorOriginal = documentoCombinado[property.Name];
                        var valorRespuesta = ConvertirValorSegunTipo(property.Value);

                        // Verificar si el campo original está vacío o tiene formato incorrecto
                        if (EsCampoVacio(valorOriginal) || TieneFormatoIncorrecto(valorOriginal, property.Name))
                        {
                            // Verificar que el valor de respuesta sea válido
                            if (EsValorValido(valorRespuesta))
                            {
                                var valorAnterior = documentoCombinado[property.Name];
                                documentoCombinado[property.Name] = valorRespuesta;
                                camposActualizados++;
                                
                                if (EsCampoVacio(valorOriginal))
                                {
                                    _logger.LogDebug("Campo '{Campo}' completado: '{ValorOriginal}' -> '{ValorNuevo}'", 
                                        property.Name, valorOriginal, valorRespuesta);
                                }
                                else
                                {
                                    _logger.LogDebug("Campo '{Campo}' corregido: '{ValorOriginal}' -> '{ValorNuevo}'", 
                                        property.Name, valorOriginal, valorRespuesta);
                                }
                            }
                            else
                            {
                                _logger.LogDebug("Campo '{Campo}' no actualizado: valor de respuesta no válido '{ValorRespuesta}'", 
                                    property.Name, valorRespuesta);
                            }
                        }
                        else
                        {
                            _logger.LogDebug("Campo '{Campo}' no actualizado: ya tiene valor válido '{ValorOriginal}'", 
                                property.Name, valorOriginal);
                        }
                    }
                }

                _logger.LogInformation("Se actualizaron {CamposActualizados} campos del documento", camposActualizados);

                // Convertir de vuelta a JSON
                return JsonSerializer.Serialize(documentoCombinado, new JsonSerializerOptions { WriteIndented = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error combinando documentos");
                return documentoOriginal;
            }
        }

        /// <summary>
        /// Convierte el valor de Ollama al tipo correcto según el campo
        /// </summary>
        private object ConvertirValorSegunTipo(JsonElement element)
        {
            try
            {
                switch (element.ValueKind)
                {
                    case JsonValueKind.Number:
                        if (element.TryGetDecimal(out var decimalValue))
                            return decimalValue;
                        if (element.TryGetInt32(out var intValue))
                            return intValue;
                        return element.GetRawText();
                    
                    case JsonValueKind.String:
                        var stringValue = element.GetString();
                        // Intentar convertir strings que deberían ser números
                        if (decimal.TryParse(stringValue, out var parsedDecimal))
                            return parsedDecimal;
                        if (DateTime.TryParse(stringValue, out var parsedDateTime))
                            return parsedDateTime;
                        return stringValue;
                    
                    case JsonValueKind.True:
                        return true;
                    
                    case JsonValueKind.False:
                        return false;
                    
                    case JsonValueKind.Null:
                        return null;
                    
                    default:
                        return element.GetRawText();
                }
            }
            catch
            {
                return element.GetRawText();
            }
        }

        /// <summary>
        /// Determina si un campo está vacío o tiene valor por defecto
        /// </summary>
        private bool EsCampoVacio(object valor)
        {
            if (valor == null) return true;
            
            var valorString = valor.ToString();
            if (string.IsNullOrWhiteSpace(valorString)) return true;
            
            // Valores por defecto comunes
            var valoresPorDefecto = new[] { "0", "null", "undefined", "N/A", "-", "" };
            
            // Solo considerar vacíos los campos que realmente no tienen información útil
            return valoresPorDefecto.Contains(valorString) || 
                   valorString.Length <= 1 || // Campos de un solo carácter
                   valorString.All(c => c == '.' || c == '-' || c == ' '); // Solo puntos, guiones o espacios
        }

        /// <summary>
        /// Verifica si un valor de respuesta es válido para ser usado
        /// </summary>
        private bool EsValorValido(object valor)
        {
            if (valor == null) return false;
            
            var valorString = valor.ToString();
            if (string.IsNullOrWhiteSpace(valorString)) return false;
            
            // Valores inválidos
            var valoresInvalidos = new[] { "null", "undefined", "N/A", "-", "", "0", "0.0" };
            if (valoresInvalidos.Contains(valorString)) return false;
            
            // No permitir valores que solo contengan caracteres especiales
            if (valorString.All(c => c == '.' || c == '-' || c == ' ' || c == '_')) return false;
            
            // Para strings, debe tener al menos 2 caracteres útiles
            if (valorString.Length < 2) return false;
            
            return true;
        }

        /// <summary>
        /// Detecta si un campo tiene formato incorrecto que necesita corrección
        /// </summary>
        private bool TieneFormatoIncorrecto(object valor, string nombreCampo)
        {
            if (valor == null) return false;
            
            var valorString = valor.ToString();
            if (string.IsNullOrWhiteSpace(valorString)) return false;
            
            // Detectar patrones de formato incorrecto según el tipo de campo
            switch (nombreCampo.ToLower())
            {
                case "titulo":
                    // El título debe ser exactamente "CARNÉ ADUANERO"
                    return valorString != "CARNÉ ADUANERO" && valorString.Length > 0;
                
                case "nombrecompleto":
                    // El nombre no debe ser solo un apellido o tener caracteres extraños
                    if (valorString.Length < 3) return true;
                    if (valorString.All(c => c == '.' || c == '-' || c == ' ' || c == '_')) return true;
                    // Si solo tiene un apellido, probablemente está incompleto
                    if (!valorString.Contains(' ') && valorString.Length < 5) return true;
                    return false;
                
                case "rut":
                    // El RUT debe tener formato XX.XXX.XXX-X
                    if (valorString.Length < 8) return true;
                    if (!valorString.Contains('.') || !valorString.Contains('-')) return true;
                    return false;
                
                case "numeroCarne":
                    // El número de carné debe tener formato válido
                    if (valorString.Length < 2) return true;
                    if (valorString.All(c => c == '.' || c == '-' || c == ' ')) return true;
                    return false;
                
                case "fechaEmision":
                    // La fecha debe tener formato válido
                    if (valorString.Length < 8) return true;
                    if (valorString.All(c => c == '.' || c == '-' || c == ' ')) return true;
                    return false;
                
                case "resolucion":
                    // La resolución debe tener formato válido
                    if (valorString.Length < 2) return true;
                    if (valorString.All(c => c == '.' || c == '-' || c == ' ')) return true;
                    return false;
                
                default:
                    // Para otros campos, verificar si tienen caracteres extraños
                    if (valorString.Length < 2) return true;
                    if (valorString.All(c => c == '.' || c == '-' || c == ' ' || c == '_')) return true;
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
