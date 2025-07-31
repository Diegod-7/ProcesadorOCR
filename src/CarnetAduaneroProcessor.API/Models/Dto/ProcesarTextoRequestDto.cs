namespace CarnetAduaneroProcessor.API.Models.Dto
{
    /// <summary>
    /// Modelo para solicitud de procesamiento de texto OCR (compartido)
    /// </summary>
    public class ProcesarTextoRequestDto
    {
        /// <summary>
        /// Texto extraído por OCR
        /// </summary>
        public string TextoOcr { get; set; } = string.Empty;
    }
} 