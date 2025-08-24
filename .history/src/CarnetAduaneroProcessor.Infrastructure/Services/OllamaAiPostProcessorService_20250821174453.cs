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
            return $@"Eres un asistente experto en procesamiento de documentos chilenos usando DeepSeek R1: 8B. Tu tarea es analizar un JSON y completar todos los campos faltantes basándote en el texto extraído por OCR.

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

CAMPOS COMUNES A BUSCAR EN EL TEXTO OCR:
- Números de documento, folio, formulario
- Fechas (emisión, vencimiento, pago, etc.)
- Montos y valores monetarios
- Nombres de instituciones, empresas, personas
- Números de identificación (RUT, pasaporte, etc.)
- Direcciones y ubicaciones
- Códigos y referencias
- Cantidades, pesos, volúmenes
- Estados y situaciones
- Comentarios y observaciones

FORMATO DE RESPUESTA:
Responde ÚNICAMENTE con el JSON completado, sin explicaciones adicionales, sin markdown, sin texto extra.
El JSON debe ser válido y parseable inmediatamente.";

        }

        /// <summary>
        /// Llama a Ollama usando su API REST con DeepSeek R1: 8B
        /// </summary>
        private async Task<string> LlamarOllamaAsync(string prompt)
        {
            try
            {
                _logger.LogInformation("Llamando a Ollama con modelo {Modelo} para procesar documento", _modeloOllama);

                // Crear el payload para Ollama optimizado para DeepSeek R1: 8B
                var payload = new
                {
                    model = _modeloOllama,
                    prompt = prompt,
                    stream = false,
                    options = new
                    {
                        temperature = 0.05, // Temperatura muy baja para respuestas más consistentes
                        top_p = 0.95,
                        top_k = 40,
                        max_tokens = 4000, // Aumentar tokens para respuestas más completas
                        repeat_penalty = 1.1,
                        num_predict = 4000
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
                if (JsonDocument.TryParse(jsonLimpio, out var jsonDoc))
                {
                    // Combinar el documento original con la respuesta de Ollama
                    var resultado = CombinarDocumentos(documentoOriginal, jsonLimpio);
                    _logger.LogInformation("Documento combinado exitosamente con respuesta de Ollama");
                    return resultado;
                }
                else
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
                        var valorRespuesta = property.Value.ValueKind == JsonValueKind.Null ? null : property.Value.GetRawText();

                        // Solo actualizar si el campo original está vacío o es null
                        if (EsCampoVacio(valorOriginal))
                        {
                            documentoCombinado[property.Name] = valorRespuesta;
                            camposActualizados++;
                            _logger.LogDebug("Campo '{Campo}' actualizado: '{ValorOriginal}' -> '{ValorNuevo}'", 
                                property.Name, valorOriginal, valorRespuesta);
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
        /// Determina si un campo está vacío o tiene valor por defecto
        /// </summary>
        private bool EsCampoVacio(object valor)
        {
            if (valor == null) return true;
            
            var valorString = valor.ToString();
            if (string.IsNullOrWhiteSpace(valorString)) return true;
            
            // Valores por defecto comunes
            var valoresPorDefecto = new[] { "0", "null", "undefined", "N/A", "-", "" };
            return valoresPorDefecto.Contains(valorString);
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
