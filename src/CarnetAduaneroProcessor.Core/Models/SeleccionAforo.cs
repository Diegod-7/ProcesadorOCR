using System.ComponentModel.DataAnnotations;

namespace CarnetAduaneroProcessor.Core.Models
{
    /// <summary>
    /// Modelo para documentos de Selección de Aforo
    /// </summary>
    public class SeleccionAforo
    {
        /// <summary>
        /// ID único del documento
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Número de Declaración de Ingreso (DIN)
        /// </summary>
        [Required]
        public string NumeroDin { get; set; } = string.Empty;

        /// <summary>
        /// Fecha de aceptación de la declaración
        /// </summary>
        public DateTime? FechaAceptacion { get; set; }

        /// <summary>
        /// Número encriptado
        /// </summary>
        public string NumeroEncriptado { get; set; } = string.Empty;

        /// <summary>
        /// Código del agente aduanero
        /// </summary>
        public string CodigoAgente { get; set; } = string.Empty;

        /// <summary>
        /// Nombre del agente aduanero
        /// </summary>
        public string NombreAgente { get; set; } = string.Empty;

        /// <summary>
        /// Código de la aduana de tramitación
        /// </summary>
        public string CodigoAduana { get; set; } = string.Empty;

        /// <summary>
        /// Nombre de la aduana de tramitación
        /// </summary>
        public string NombreAduana { get; set; } = string.Empty;

        /// <summary>
        /// Tipo de revisión (FISICO, SIN INSPECCION, etc.)
        /// </summary>
        public string TipoRevision { get; set; } = string.Empty;

        /// <summary>
        /// Nombre del firmante
        /// </summary>
        public string NombreFirmante { get; set; } = string.Empty;

        /// <summary>
        /// RUT del firmante
        /// </summary>
        public string RutFirmante { get; set; } = string.Empty;

        /// <summary>
        /// Número de agencia del firmante
        /// </summary>
        public string NumeroAgencia { get; set; } = string.Empty;

        /// <summary>
        /// Nombre de la agencia
        /// </summary>
        public string NombreAgencia { get; set; } = string.Empty;

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

        /// <summary>
        /// Comentarios o errores del procesamiento
        /// </summary>
        public string Comentarios { get; set; } = string.Empty;

        /// <summary>
        /// Indica si el documento es válido
        /// </summary>
        public bool EsValido { get; set; }

        /// <summary>
        /// Valida que el documento tenga los campos requeridos
        /// </summary>
        public void ValidarDocumento()
        {
            EsValido = !string.IsNullOrWhiteSpace(NumeroDin) && 
                      !string.IsNullOrWhiteSpace(TipoRevision) &&
                      !string.IsNullOrWhiteSpace(NombreAgente);
        }
    }
} 