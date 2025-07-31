namespace CarnetAduaneroProcessor.API.Models
{
    /// <summary>
    /// Modelo para solicitud de procesamiento de texto
    /// </summary>
    public class ProcesamientoTextoRequest
    {
        /// <summary>
        /// Texto OCR a procesar
        /// </summary>
        public string Texto { get; set; } = string.Empty;
    }
} 