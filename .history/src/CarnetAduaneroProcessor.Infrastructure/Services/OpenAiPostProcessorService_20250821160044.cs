using CarnetAduaneroProcessor.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace CarnetAduaneroProcessor.Infrastructure.Services
{
    /// <summary>
    /// Servicio de post-procesamiento usando OpenAI GPT-3.5-turbo
    /// </summary>
    public class OpenAiPostProcessorService : IAiPostProcessorService
    {
        private readonly ILogger<OpenAiPostProcessorService> _logger;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _apiUrl = "https://api.openai.com/v1/chat/completions";

        public OpenAiPostProcessorService(ILogger<OpenAiPostProcessorService> logger, IConfiguration configuration, HttpClient httpClient)
        {
            _logger = logger;
            _configuration = configuration;
            _httpClient = httpClient;
            _apiKey = _configuration["OpenAI:ApiKey"] ?? throw new InvalidOperationException("OpenAI API Key no configurada");
        }

        public async Task<string> PostProcesarDocumentoAsync(string documentoJson, string textoExtraido)
        {
            try
            {
                _logger.LogInformation("Iniciando post-procesamiento con IA para documento");

                var prompt = CrearPrompt(documentoJson, textoExtraido);
                var response = await LlamarOpenAIAsync(prompt);

                if (!string.IsNullOrEmpty(response))
                {
                    _logger.LogInformation("Post-procesamiento con IA completado exitosamente");
                    return response;
                }

                _logger.LogWarning("No se pudo obtener respuesta de la IA, devolviendo documento original");
                return documentoJson;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en post-procesamiento con IA");
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

        private async Task<string> LlamarOpenAIAsync(string prompt)
        {
            try
            {
                var requestBody = new
                {
                    model = "gpt-3.5-turbo",
                    messages = new[]
                    {
                        new { role = "system", content = "Eres un asistente experto en procesamiento de documentos que responde solo con JSON válido." },
                        new { role = "user", content = prompt }
                    },
                    temperature = 0.1, // Baja temperatura para respuestas más consistentes
                    max_tokens = 1000
                };

                var jsonContent = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);

                var response = await _httpClient.PostAsync(_apiUrl, content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var openAiResponse = JsonSerializer.Deserialize<OpenAiResponse>(responseContent);
                    
                    if (openAiResponse?.Choices?.Length > 0)
                    {
                        return openAiResponse.Choices[0].Message.Content.Trim();
                    }
                }

                _logger.LogWarning("Respuesta no exitosa de OpenAI: {StatusCode}", response.StatusCode);
                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error llamando a OpenAI");
                return string.Empty;
            }
        }

        // Clases para deserializar la respuesta de OpenAI
        private class OpenAiResponse
        {
            public Choice[] Choices { get; set; } = Array.Empty<Choice>();
        }

        private class Choice
        {
            public Message Message { get; set; } = new();
        }

        private class Message
        {
            public string Content { get; set; } = string.Empty;
        }
    }
}
