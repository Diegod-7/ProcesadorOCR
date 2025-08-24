using CarnetAduaneroProcessor.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace CarnetAduaneroProcessor.Infrastructure.Services
{
    /// <summary>
    /// Servicio de post-procesamiento usando Ollama (gratuito y local)
    /// </summary>
    public class OllamaPostProcessorService : IAiPostProcessorService
    {
        private readonly ILogger<OllamaPostProcessorService> _logger;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly string _ollamaUrl;

        public OllamaPostProcessorService(ILogger<OllamaPostProcessorService> logger, IConfiguration configuration, HttpClient httpClient)
        {
            _logger = logger;
            _configuration = configuration;
            _httpClient = httpClient;
            _ollamaUrl = _configuration["Ollama:Url"] ?? "http://localhost:11434";
        }

        public async Task<string> PostProcesarDocumentoAsync(string documentoJson, string textoExtraido)
        {
            try
            {
                _logger.LogInformation("Iniciando post-procesamiento con Ollama para documento");

                var prompt = CrearPrompt(documentoJson, textoExtraido);
                var response = await LlamarOllamaAsync(prompt);

                if (!string.IsNullOrEmpty(response))
                {
                    _logger.LogInformation("Post-procesamiento con Ollama completado exitosamente");
                    return response;
                }

                _logger.LogWarning("No se pudo obtener respuesta de Ollama, devolviendo documento original");
                return documentoJson;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en post-procesamiento con Ollama");
                return documentoJson;
            }
        }

        private string CrearPrompt(string documentoJson, string textoExtraido)
        {
            return $@"Eres un experto en procesamiento de documentos chilenos. Analiza el siguiente JSON de un Comprobante de Transacción y el texto extraído por OCR, y completa los campos faltantes.

JSON del documento:
{documentoJson}

Texto extraído por OCR:
{textoExtraido}

Instrucciones:
1. Analiza el texto OCR para identificar los valores de los campos vacíos
2. Completa solo los campos que estén vacíos o sean null
3. Mantén el formato JSON exacto
4. Para fechas, usa el formato ISO 8601 (YYYY-MM-DD o YYYY-MM-DDTHH:mm:ss)
5. Para números, mantén el formato original
6. No inventes información, solo extrae lo que está en el texto

Responde SOLO con el JSON completado, sin explicaciones adicionales.";
        }

        private async Task<string> LlamarOllamaAsync(string prompt)
        {
            try
            {
                var requestBody = new
                {
                    model = "llama3.1:8b", // Puedes cambiar por mistral:7b, codellama:7b, etc.
                    prompt = prompt,
                    stream = false,
                    options = new
                    {
                        temperature = 0.1,
                        top_p = 0.9,
                        max_tokens = 1000
                    }
                };

                var jsonContent = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_ollamaUrl}/api/generate", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var ollamaResponse = JsonSerializer.Deserialize<OllamaResponse>(responseContent);
                    
                    if (!string.IsNullOrEmpty(ollamaResponse?.Response))
                    {
                        return ollamaResponse.Response.Trim();
                    }
                }

                _logger.LogWarning("Respuesta no exitosa de Ollama: {StatusCode}", response.StatusCode);
                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error llamando a Ollama");
                return string.Empty;
            }
        }

        // Clases para deserializar la respuesta de Ollama
        private class OllamaResponse
        {
            public string Response { get; set; } = string.Empty;
        }
    }
}
