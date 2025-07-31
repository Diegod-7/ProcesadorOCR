using System.ComponentModel.DataAnnotations;

namespace CarnetAduaneroProcessor.Core.Models
{
    /// <summary>
    /// Modelo para documentos TACT/ADC (Transport Air Cargo Tariff / Airway Bill)
    /// </summary>
    public class TactAdc
    {
        [Key]
        public int Id { get; set; }

        // Campos críticos (marcados en rojo en el documento)
        [Required(ErrorMessage = "El número TATC es requerido")]
        public string NumeroTatc { get; set; } = string.Empty;

        [Required(ErrorMessage = "El número de contenedor es requerido")]
        public string NumeroContenedor { get; set; } = string.Empty;

        [Required(ErrorMessage = "El número de sellos es requerido")]
        public string NumeroSellos { get; set; } = string.Empty;

        // Campos adicionales
        public string EmpresaEmisora { get; set; } = string.Empty;
        public string DireccionEmpresa { get; set; } = string.Empty;
        public string RutEmisor { get; set; } = string.Empty;
        public DateTime? FechaEmision { get; set; }
        public string TipoDocumento { get; set; } = string.Empty;
        public string BlArmador { get; set; } = string.Empty;
        public string Consignatario { get; set; } = string.Empty;
        public string RutConsignatario { get; set; } = string.Empty;
        public string DireccionConsignatario { get; set; } = string.Empty;
        public string Forwarder { get; set; } = string.Empty;
        public string LineaOperadora { get; set; } = string.Empty;
        public string ServicioAlmacenaje { get; set; } = string.Empty;
        public string GuardaAlmacen { get; set; } = string.Empty;
        public string RutGuardaAlmacen { get; set; } = string.Empty;
        public string PuertoOrigen { get; set; } = string.Empty;
        public string PuertoDescarga { get; set; } = string.Empty;
        public string PuertoEmbarque { get; set; } = string.Empty;
        public string PuertoDestino { get; set; } = string.Empty;
        public string PuertoTransbordo { get; set; } = string.Empty;
        public string TipoBulto { get; set; } = string.Empty;
        public int? Cantidad { get; set; }
        public decimal? Peso { get; set; }
        public decimal? Volumen { get; set; }
        public string Estado { get; set; } = string.Empty;

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
            EsValido = !string.IsNullOrWhiteSpace(NumeroTatc) &&
                      !string.IsNullOrWhiteSpace(NumeroContenedor) &&
                      !string.IsNullOrWhiteSpace(NumeroSellos);
            
            return EsValido;
        }

        /// <summary>
        /// Obtiene un resumen de los campos críticos
        /// </summary>
        public string ObtenerResumen()
        {
            return $"TATC: {NumeroTatc}, Contenedor: {NumeroContenedor}, Sellos: {NumeroSellos}";
        }
    }
} 