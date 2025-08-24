namespace CarnetAduaneroProcessor.Core.Services
{
    /// <summary>
    /// Servicio de OCR que usa Tesseract
    /// </summary>
    public interface IHybridOcrService
    {
        /// <summary>
        /// Extrae texto de una imagen usando Tesseract
        /// </summary>
        /// <param name="imageStream">Stream de la imagen</param>
        /// <returns>Texto extraído</returns>
        Task<string> ExtractTextAsync(Stream imageStream);
    }
}
