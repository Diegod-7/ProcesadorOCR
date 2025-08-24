namespace CarnetAduaneroProcessor.Core.Services
{
    /// <summary>
    /// Servicio híbrido de OCR que combina Tesseract y Azure Computer Vision
    /// </summary>
    public interface IHybridOcrService
    {
        /// <summary>
        /// Extrae texto de una imagen usando Tesseract como principal y Azure como fallback
        /// </summary>
        /// <param name="imageStream">Stream de la imagen</param>
        /// <returns>Texto extraído</returns>
        Task<string> ExtractTextAsync(Stream imageStream);
    }
}
