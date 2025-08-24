using System.Text.Json;

namespace CarnetAduaneroProcessor.Core.Services
{
    /// <summary>
    /// Servicio para post-procesar documentos extraídos con IA
    /// </summary>
    public interface IAiPostProcessorService
    {
        /// <summary>
        /// Post-procesa un documento usando IA para completar campos faltantes
        /// </summary>
        /// <param name="documentoJson">JSON del documento con campos faltantes</param>
        /// <param name="textoExtraido">Texto extraído por OCR</param>
        /// <returns>JSON del documento completado</returns>
        Task<string> PostProcesarDocumentoAsync(string documentoJson, string textoExtraido);
    }
}
