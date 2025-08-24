namespace CarnetAduaneroProcessor.Core.Services
{
    /// <summary>
    /// Servicio para post-procesamiento de documentos usando IA
    /// </summary>
    public interface IAiPostProcessorService
    {
        /// <summary>
        /// Post-procesa un documento JSON usando IA para completar campos faltantes
        /// </summary>
        /// <param name="documentoJson">Documento JSON con campos faltantes</param>
        /// <param name="textoOcr">Texto extraído por OCR</param>
        /// <returns>Documento JSON completado</returns>
        Task<string> PostProcesarDocumentoAsync(string documentoJson, string textoOcr);
    }
}
