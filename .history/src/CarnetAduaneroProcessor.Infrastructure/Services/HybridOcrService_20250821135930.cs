using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Drawing;
using Tesseract;
using CarnetAduaneroProcessor.Core.Services;

namespace CarnetAduaneroProcessor.Infrastructure.Services
{
    /// <summary>
    /// Servicio híbrido de OCR que usa Tesseract como principal y Azure Computer Vision como fallback
    /// </summary>
    public class HybridOcrService : IHybridOcrService
    {
        private readonly ILogger<HybridOcrService> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _tessdataPath;
        private TesseractEngine _tesseractEngine;

        public HybridOcrService(ILogger<HybridOcrService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            
            // Configuración de Tesseract
            _tessdataPath = configuration["Tesseract:TessdataPath"] ?? "/usr/share/tessdata";
            
            InitializeTesseract();
        }

        /// <summary>
        /// Inicializa el motor de Tesseract
        /// </summary>
        private void InitializeTesseract()
        {
            try
            {
                _logger.LogInformation("Inicializando motor de Tesseract desde: {TessdataPath}", _tessdataPath);
                
                // Verificar que la ruta existe
                if (!Directory.Exists(_tessdataPath))
                {
                    _logger.LogError("La ruta de tessdata no existe: {TessdataPath}", _tessdataPath);
                    _tesseractEngine = null;
                    return;
                }
                
                // Verificar archivos de idioma
                var spanishPath = Path.Combine(_tessdataPath, "spa.traineddata");
                var englishPath = Path.Combine(_tessdataPath, "eng.traineddata");
                
                _logger.LogInformation("Verificando archivo español: {SpanishPath} - Existe: {Exists}", spanishPath, File.Exists(spanishPath));
                _logger.LogInformation("Verificando archivo inglés: {EnglishPath} - Existe: {Exists}", englishPath, File.Exists(englishPath));
                
                // Intentar usar español primero, luego inglés como fallback
                if (File.Exists(spanishPath))
                {
                    _logger.LogInformation("Intentando inicializar Tesseract con idioma español...");
                    _tesseractEngine = new TesseractEngine(_tessdataPath, "spa", EngineMode.Default);
                    _logger.LogInformation("Tesseract inicializado exitosamente con idioma español");
                }
                else if (File.Exists(englishPath))
                {
                    _logger.LogInformation("Intentando inicializar Tesseract con idioma inglés...");
                    _tesseractEngine = new TesseractEngine(_tessdataPath, "eng", EngineMode.Default);
                    _logger.LogInformation("Tesseract inicializado exitosamente con idioma inglés");
                }
                else
                {
                    _logger.LogWarning("No se encontraron archivos de idioma de Tesseract. Usando Azure Computer Vision como fallback.");
                    _tesseractEngine = null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inicializando Tesseract. Usando Azure Computer Vision como fallback. Error: {Error}", ex.ToString());
                _tesseractEngine = null;
            }
        }

        /// <summary>
        /// Extrae texto de una imagen usando Tesseract
        /// </summary>
        public async Task<string> ExtractTextAsync(Stream imageStream)
        {
            try
            {
                // Convertir Stream a Bitmap para Tesseract
                using var bitmap = new Bitmap(imageStream);
                
                // Usar Tesseract para extraer texto
                if (_tesseractEngine != null)
                {
                    var tesseractResult = await ExtractTextWithTesseractAsync(bitmap);
                    if (!string.IsNullOrWhiteSpace(tesseractResult))
                    {
                        _logger.LogInformation("Texto extraído exitosamente con Tesseract");
                        return tesseractResult;
                    }
                }

                // Si Tesseract falla
                _logger.LogWarning("Tesseract falló al extraer texto de la imagen");
                return "Error: No se pudo extraer texto de la imagen";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante la extracción de texto");
                return $"Error: {ex.Message}";
            }
        }

        /// <summary>
        /// Extrae texto usando Tesseract (gratis)
        /// </summary>
        private async Task<string> ExtractTextWithTesseractAsync(Bitmap image)
        {
            try
            {
                return await Task.Run(() =>
                {
                    using var pix = ConvertBitmapToPix(image);
                    using var page = _tesseractEngine.Process(pix);
                    return page.GetText();
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extrayendo texto con Tesseract");
                return string.Empty;
            }
        }



        /// <summary>
        /// Convierte Bitmap a Pix (formato de Tesseract)
        /// </summary>
        private Pix ConvertBitmapToPix(Bitmap bitmap)
        {
            // Convertir Bitmap a formato compatible con Tesseract
            using var ms = new MemoryStream();
            bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            var imageBytes = ms.ToArray();
            
            return Pix.LoadFromMemory(imageBytes);
        }

        /// <summary>
        /// Libera recursos
        /// </summary>
        public void Dispose()
        {
            _tesseractEngine?.Dispose();
        }
    }

}
