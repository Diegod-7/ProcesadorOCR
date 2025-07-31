using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarnetAduaneroProcessor.Core.Models
{
    /// <summary>
    /// Modelo que representa un error de procesamiento
    /// </summary>
    public class ErrorProcesamiento
    {
        /// <summary>
        /// Identificador único del error
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// ID del carné aduanero relacionado
        /// </summary>
        [Required(ErrorMessage = "El ID del carné es obligatorio")]
        public int CarnetAduaneroId { get; set; }

        /// <summary>
        /// Fecha y hora del error
        /// </summary>
        [Required(ErrorMessage = "La fecha del error es obligatoria")]
        public DateTime FechaError { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Tipo de error ocurrido
        /// </summary>
        [Required(ErrorMessage = "El tipo de error es obligatorio")]
        [StringLength(50, ErrorMessage = "El tipo de error no puede exceder 50 caracteres")]
        public string TipoError { get; set; } = string.Empty;

        /// <summary>
        /// Mensaje de error
        /// </summary>
        [Required(ErrorMessage = "El mensaje de error es obligatorio")]
        [StringLength(500, ErrorMessage = "El mensaje no puede exceder 500 caracteres")]
        public string Mensaje { get; set; } = string.Empty;

        /// <summary>
        /// Detalles completos del error
        /// </summary>
        [Column(TypeName = "TEXT")]
        public string? Detalles { get; set; }

        /// <summary>
        /// Stack trace del error
        /// </summary>
        [Column(TypeName = "TEXT")]
        public string? StackTrace { get; set; }

        /// <summary>
        /// Indica si el error ha sido resuelto
        /// </summary>
        public bool Resuelto { get; set; } = false;

        /// <summary>
        /// Fecha de resolución del error
        /// </summary>
        public DateTime? FechaResolucion { get; set; }

        /// <summary>
        /// Usuario que resolvió el error
        /// </summary>
        [StringLength(100, ErrorMessage = "El usuario no puede exceder 100 caracteres")]
        public string? UsuarioResolucion { get; set; }

        /// <summary>
        /// Comentarios sobre la resolución
        /// </summary>
        [StringLength(1000, ErrorMessage = "Los comentarios no pueden exceder 1000 caracteres")]
        public string? ComentariosResolucion { get; set; }

        /// <summary>
        /// Nivel de severidad del error (Bajo, Medio, Alto, Crítico)
        /// </summary>
        [StringLength(20, ErrorMessage = "La severidad no puede exceder 20 caracteres")]
        public string? Severidad { get; set; }

        /// <summary>
        /// Usuario que reportó el error
        /// </summary>
        [StringLength(100, ErrorMessage = "El usuario no puede exceder 100 caracteres")]
        public string? UsuarioReporte { get; set; }

        /// <summary>
        /// IP del cliente donde ocurrió el error
        /// </summary>
        [StringLength(45, ErrorMessage = "La IP no puede exceder 45 caracteres")]
        public string? IpCliente { get; set; }

        /// <summary>
        /// User Agent del cliente
        /// </summary>
        [StringLength(500, ErrorMessage = "El User Agent no puede exceder 500 caracteres")]
        public string? UserAgent { get; set; }

        /// <summary>
        /// Datos adicionales del error en formato JSON
        /// </summary>
        [Column(TypeName = "TEXT")]
        public string? DatosAdicionales { get; set; }

        // Propiedades de navegación
        public virtual CarnetAduanero CarnetAduanero { get; set; } = null!;

        // Propiedades calculadas
        /// <summary>
        /// Tiempo transcurrido desde el error
        /// </summary>
        [NotMapped]
        public TimeSpan TiempoTranscurrido => DateTime.UtcNow - FechaError;

        /// <summary>
        /// Tiempo transcurrido formateado
        /// </summary>
        [NotMapped]
        public string TiempoTranscurridoFormateado
        {
            get
            {
                var tiempo = TiempoTranscurrido;
                
                if (tiempo.TotalDays >= 1)
                    return $"{(int)tiempo.TotalDays}d {tiempo.Hours}h {tiempo.Minutes}m";
                else if (tiempo.TotalHours >= 1)
                    return $"{(int)tiempo.TotalHours}h {tiempo.Minutes}m";
                else if (tiempo.TotalMinutes >= 1)
                    return $"{(int)tiempo.TotalMinutes}m {tiempo.Seconds}s";
                else
                    return $"{tiempo.Seconds}s";
            }
        }

        /// <summary>
        /// Indica si el error es reciente (menos de 24 horas)
        /// </summary>
        [NotMapped]
        public bool EsReciente => TiempoTranscurrido.TotalHours < 24;

        /// <summary>
        /// Indica si el error es crítico
        /// </summary>
        [NotMapped]
        public bool EsCritico => Severidad?.Equals("Crítico", StringComparison.OrdinalIgnoreCase) == true;

        /// <summary>
        /// Estado del error formateado
        /// </summary>
        [NotMapped]
        public string EstadoFormateado => Resuelto ? "Resuelto" : "Pendiente";
    }
} 