using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarnetAduaneroProcessor.Core.Models
{
    /// <summary>
    /// Modelo que representa el historial de procesamiento de un carné
    /// </summary>
    public class Procesamiento
    {
        /// <summary>
        /// Identificador único del procesamiento
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// ID del carné aduanero relacionado
        /// </summary>
        [Required(ErrorMessage = "El ID del carné es obligatorio")]
        public int CarnetAduaneroId { get; set; }

        /// <summary>
        /// Fecha y hora del procesamiento
        /// </summary>
        [Required(ErrorMessage = "La fecha de procesamiento es obligatoria")]
        public DateTime FechaProcesamiento { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Método de procesamiento utilizado
        /// </summary>
        [Required(ErrorMessage = "El método de procesamiento es obligatorio")]
        [StringLength(50, ErrorMessage = "El método no puede exceder 50 caracteres")]
        public string MetodoUtilizado { get; set; } = string.Empty;

        /// <summary>
        /// Nivel de confianza del procesamiento (0-1)
        /// </summary>
        [Range(0, 1, ErrorMessage = "La confianza debe estar entre 0 y 1")]
        public decimal? Confianza { get; set; }

        /// <summary>
        /// Duración del procesamiento en milisegundos
        /// </summary>
        public long? DuracionMs { get; set; }

        /// <summary>
        /// Estado del procesamiento (Exitoso, Fallido, En Proceso)
        /// </summary>
        [Required(ErrorMessage = "El estado es obligatorio")]
        [StringLength(20, ErrorMessage = "El estado no puede exceder 20 caracteres")]
        public string Estado { get; set; } = "Exitoso";

        /// <summary>
        /// Detalles adicionales del procesamiento
        /// </summary>
        [Column(TypeName = "TEXT")]
        public string? Detalles { get; set; }

        /// <summary>
        /// Usuario que realizó el procesamiento
        /// </summary>
        [StringLength(100, ErrorMessage = "El usuario no puede exceder 100 caracteres")]
        public string? Usuario { get; set; }

        /// <summary>
        /// IP del cliente que realizó el procesamiento
        /// </summary>
        [StringLength(45, ErrorMessage = "La IP no puede exceder 45 caracteres")]
        public string? IpCliente { get; set; }

        /// <summary>
        /// User Agent del cliente
        /// </summary>
        [StringLength(500, ErrorMessage = "El User Agent no puede exceder 500 caracteres")]
        public string? UserAgent { get; set; }

        // Propiedades de navegación
        public virtual CarnetAduanero CarnetAduanero { get; set; } = null!;

        // Propiedades calculadas
        /// <summary>
        /// Duración formateada del procesamiento
        /// </summary>
        [NotMapped]
        public string DuracionFormateada
        {
            get
            {
                if (!DuracionMs.HasValue) return "N/A";
                
                if (DuracionMs.Value < 1000)
                    return $"{DuracionMs.Value}ms";
                else if (DuracionMs.Value < 60000)
                    return $"{DuracionMs.Value / 1000.0:F1}s";
                else
                    return $"{DuracionMs.Value / 60000.0:F1}min";
            }
        }

        /// <summary>
        /// Confianza formateada como porcentaje
        /// </summary>
        [NotMapped]
        public string ConfianzaFormateada
        {
            get
            {
                if (!Confianza.HasValue) return "N/A";
                return $"{Confianza.Value * 100:F1}%";
            }
        }
    }
} 