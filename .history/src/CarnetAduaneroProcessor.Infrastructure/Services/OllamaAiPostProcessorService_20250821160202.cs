using CarnetAduaneroProcessor.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace CarnetAduaneroProcessor.Infrastructure.Services
{
    /// <summary>
    /// Servicio de post-procesamiento con IA usando Ollama
    /// </summary>
    public class OllamaAiPostProcessorService : IAiPostProcessorService
    {
        private readonly ILogger<OllamaAiPostProcessorService> _logger;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly string _ollamaBaseUrl;

        public OllamaAiPostProcessorService(ILogger<OllamaAiPostProcessorService> logger, IConfiguration configuration, HttpClient httpClient)
        {
            _logger = logger;
            _configuration = configuration;
            _httpClient = httpClient;
            
            // URL base de Ollama (por defecto localhost:11434)
            _ollamaBaseUrl = _configuration["Ollama:BaseUrl"] ?? "http://localhost:11434";
        }

        /// <summary>
        /// Post-procesa un documento JSON usando Ollama para completar campos faltantes
        /// </summary>
        public async Task<string> PostProcesarDocumentoAsync(string documentoJson, string textoOcr)
        {
            try
            {
                _logger.LogInformation("Iniciando post-procesamiento con IA usando Ollama");

                // Crear el prompt para Ollama
                var prompt = CrearPrompt(documentoJson, textoOcr);

                // Llamar a Ollama
                var respuesta = await LlamarOllamaAsync(prompt);

                // Procesar la respuesta
                var documentoCompletado = ProcesarRespuestaOllama(respuesta, documentoJson);

                _logger.LogInformation("Post-procesamiento con IA completado exitosamente");
                return documentoCompletado;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en post-procesamiento con IA usando Ollama");
                return documentoJson; // Devolver el documento original si hay error
            }
        }

        /// <summary>
        /// Crea el prompt para Ollama
        /// </summary>
        private string CrearPrompt(string documentoJson, string textoOcr)
        {
            return $@"Eres un asistente experto en procesamiento de documentos chilenos. Tu tarea es completar un JSON de Comprobante de Transacción basándote en el texto extraído por OCR.

DOCUMENTO JSON ACTUAL:
{documentoJson}

TEXTO EXTRAÍDO POR OCR:
{textoOcr}

INSTRUCCIONES:
1. Analiza el texto OCR y extrae los campos faltantes del JSON
2. Completa solo los campos que estén vacíos o sean null
3. Mantén el formato JSON exacto
4. Para fechas, usa el formato ISO 8601 (YYYY-MM-DDTHH:mm:ss)
5. Para números, usa el formato decimal sin comas ni puntos de miles
6. NO modifiques campos que ya tengan valor

CAMPOS A COMPLETAR:
- numeroFolio: Número de folio (formato: 4560010758)
- totalPagado: Monto total pagado (formato: 8153962)
- formulario: Número de formulario (formato: 15)
- fechaVencimiento: Fecha de vencimiento (formato: 2025-07-09T00:00:00)
- monedaPago: Moneda de pago (formato: CLP)
- fechaPago: Fecha de pago (formato: 2025-06-24T17:44:12)
- institucionRecaudadora: Nombre de la institución (formato: BANCO ITAU)
- identificadorTransaccion: Identificador de transacción (formato: 02847341-57208059)

RESPONDE SOLO CON EL JSON COMPLETADO, sin explicaciones adicionales.";
        }

        /// <summary>
        /// Llama a Ollama usando su API REST
        /// </summary>
        private async Task<string> LlamarOllamaAsync(string prompt)
        {
            try
            {
                // Crear el payload para Ollama
                var payload = new
                {
                    model = "llama3.2:3b", // Modelo gratuito y ligero
                    prompt = prompt,
                    stream = false,
                    options = new
                    {
                        temperature = 0.1, // Baja temperatura para respuestas más consistentes
                        top_p = 0.9,
                        max_tokens = 2000
                    }
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Llamar a la API de Ollama
                var response = await _httpClient.PostAsync($"{_ollamaBaseUrl}/api/generate", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var ollamaResponse = JsonSerializer.Deserialize<OllamaResponse>(responseContent);
                    return ollamaResponse?.response ?? string.Empty;
                }
                else
                {
                    _logger.LogWarning("Ollama respondió con código de error: {StatusCode}", response.StatusCode);
                    return string.Empty;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error llamando a Ollama");
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
                    return documentoOriginal;

                // Limpiar la respuesta de Ollama (eliminar markdown si existe)
                var jsonLimpio = LimpiarRespuestaOllama(respuestaOllama);

                // Intentar parsear la respuesta como JSON
                if (JsonDocument.TryParse(jsonLimpio, out var jsonDoc))
                {
                    // Combinar el documento original con la respuesta de Ollama
                    return CombinarDocumentos(documentoOriginal, jsonLimpio);
                }
                else
                {
                    _logger.LogWarning("La respuesta de Ollama no es un JSON válido");
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
        /// Limpia la respuesta de Ollama
        /// </summary>
        private string LimpiarRespuestaOllama(string respuesta)
        {
            // Eliminar markdown si existe
            var limpio = respuesta
                .Replace("```json", "")
                .Replace("```", "")
                .Trim();

            // Buscar el JSON dentro de la respuesta
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
                foreach (var property in respuesta.RootElement.EnumerateObject())
                {
                    if (documentoCombinado.ContainsKey(property.Name))
                    {
                        var valorOriginal = documentoCombinado[property.Name];
                        var valorRespuesta = property.Value.ValueKind == JsonValueKind.Null ? null : property.Value.GetRawText();

                        // Solo actualizar si el campo original está vacío o es null
                        if (valorOriginal == null || 
                            valorOriginal.ToString() == "" || 
                            valorOriginal.ToString() == "0" ||
                            valorOriginal.ToString() == "null")
                        {
                            documentoCombinado[property.Name] = valorRespuesta;
                        }
                    }
                }

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
        /// Clase para deserializar la respuesta de Ollama
        /// </summary>
        private class OllamaResponse
        {
            public string response { get; set; } = string.Empty;
        }
    }
}
