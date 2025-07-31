using System.ComponentModel.DataAnnotations;

namespace CarnetAduaneroProcessor.Core.Models
{
    /// <summary>
    /// Modelo para documentos de Comprobante de Transacción
    /// </summary>
    public class ComprobanteTransaccion
    {
        [Key]
        public int Id { get; set; }

        // Campos críticos (marcados en rojo en el documento)
        [Required(ErrorMessage = "El número de folio es requerido")]
        public string NumeroFolio { get; set; } = string.Empty;

        [Required(ErrorMessage = "El total pagado es requerido")]
        public decimal TotalPagado { get; set; }

        // Campos adicionales
        public string Rut { get; set; } = string.Empty;
        public string Formulario { get; set; } = string.Empty;
        public DateTime? FechaVencimiento { get; set; }
        public string MonedaPago { get; set; } = string.Empty;
        public DateTime? FechaPago { get; set; }
        public string InstitucionRecaudadora { get; set; } = string.Empty;
        public string IdentificadorTransaccion { get; set; } = string.Empty;
        public string CodigoBarras { get; set; } = string.Empty;
        public string NumeroReferencia { get; set; } = string.Empty;

        // Metadatos del procesamiento
        public string NombreArchivo { get; set; } = string.Empty;
        public string HashArchivo { get; set; } = string.Empty;
        public string MetodoExtraccion { get; set; } = string.Empty;
        public string TextoExtraido { get; set; } = string.Empty;
        public decimal ConfianzaExtraccion { get; set; }
        public DateTime FechaProcesamiento { get; set; } = DateTime.UtcNow;
        public string Comentarios { get; set; } = string.Empty;

        // Validación del documento
        public bool EsValido { get; set; }

        /// <summary>
        /// Valida si el documento tiene los campos críticos completos
        /// </summary>
        public bool ValidarDocumento()
        {
            EsValido = !string.IsNullOrWhiteSpace(NumeroFolio) && TotalPagado > 0;
            
            return EsValido;
        }

        /// <summary>
        /// Obtiene un resumen de los campos críticos
        /// </summary>
        public string ObtenerResumen()
        {
            return $"Folio: {NumeroFolio}, Total: {TotalPagado:C}";
        }
    }
} 