namespace CarnetAduaneroProcessor.Core.Models
{
    /// <summary>
    /// Modelo de datos extraídos del carné aduanero
    /// </summary>
    public class CarnetAduaneroData
    {
        /// <summary>
        /// Título del documento (CARNÉ ADUANERO)
        /// </summary>
        public string Titulo { get; set; } = string.Empty;

        /// <summary>
        /// Nombre completo del agente aduanero
        /// </summary>
        public string NombreCompleto { get; set; } = string.Empty;

        /// <summary>
        /// RUT (Rol Único Tributario) del agente
        /// </summary>
        public string Rut { get; set; } = string.Empty;

        /// <summary>
        /// Número de carné
        /// </summary>
        public string NumeroCarne { get; set; } = string.Empty;

        /// <summary>
        /// Fecha de emisión
        /// </summary>
        public string FechaEmision { get; set; } = string.Empty;

        /// <summary>
        /// Resolución
        /// </summary>
        public string Resolucion { get; set; } = string.Empty;

        /// <summary>
        /// Indica si todos los campos requeridos fueron extraídos correctamente
        /// </summary>
        public bool EsValido => !string.IsNullOrWhiteSpace(Titulo) &&
                               !string.IsNullOrWhiteSpace(NombreCompleto) &&
                               !string.IsNullOrWhiteSpace(Rut);

        /// <summary>
        /// Mensaje de error si la extracción no fue exitosa
        /// </summary>
        public string MensajeError { get; set; } = string.Empty;

        /// <summary>
        /// Nombre del archivo procesado
        /// </summary>
        public string NombreArchivo { get; set; } = string.Empty;

        /// <summary>
        /// Hash del archivo para verificación de integridad
        /// </summary>
        public string HashArchivo { get; set; } = string.Empty;

        /// <summary>
        /// Método utilizado para la extracción de datos
        /// </summary>
        public string MetodoExtraccion { get; set; } = string.Empty;

        /// <summary>
        /// Texto extraído del documento
        /// </summary>
        public string TextoExtraido { get; set; } = string.Empty;

        /// <summary>
        /// Nivel de confianza en la extracción (0.0 - 1.0)
        /// </summary>
        public decimal ConfianzaExtraccion { get; set; }

        /// <summary>
        /// Fecha y hora del procesamiento
        /// </summary>
        public DateTime FechaProcesamiento { get; set; } = DateTime.UtcNow;
    }
} 