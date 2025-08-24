using CarnetAduaneroProcessor.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace CarnetAduaneroProcessor.Infrastructure.Services
{
    /// <summary>
    /// Servicio de post-procesamiento con IA usando Ollama con Gemma 3: 1B
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
            
            // Modelo de Ollama (por defecto Gemma 3: 1B)
            _modeloOllama = _configuration["Ollama:Model"] ?? "gemma3:1b";
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
  * Titulo: "CARNÉ ADUANERO" (siempre este valor exacto)
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

IMPORTANTE: ConfianzaExtraccion debe ser un número decimal (ej: 0.85), NO un string.";

        }

        /// <summary>
        /// Llama a Ollama usando su API REST con DeepSeek R1: 8B
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

                // Actualizar solo los campos que estén vacíos o sean null con la respuesta de Ollama
                var camposActualizados = 0;
                foreach (var property in respuesta.RootElement.EnumerateObject())
                {
                    if (documentoCombinado.ContainsKey(property.Name))
                    {
                        var valorOriginal = documentoCombinado[property.Name];
                        var valorRespuesta = ConvertirValorSegunTipo(property.Value);

                        // Solo actualizar si el campo original está realmente vacío
                        if (EsCampoVacio(valorOriginal))
                        {
                            // Verificar que el valor de respuesta sea válido
                            if (EsValorValido(valorRespuesta))
                            {
                                documentoCombinado[property.Name] = valorRespuesta;
                                camposActualizados++;
                                _logger.LogDebug("Campo '{Campo}' actualizado: '{ValorOriginal}' -> '{ValorNuevo}'", 
                                    property.Name, valorOriginal, valorRespuesta);
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
        /// Clase para deserializar la respuesta de Ollama
        /// </summary>
        private class OllamaResponse
        {
            public string response { get; set; } = string.Empty;
        }
    }
}
