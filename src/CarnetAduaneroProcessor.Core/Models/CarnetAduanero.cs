using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarnetAduaneroProcessor.Core.Models
{
    /// <summary>
    /// Modelo principal que representa un Carné Aduanero procesado
    /// </summary>
    public class CarnetAduanero
    {
        /// <summary>
        /// Identificador único del carné
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Número del carné aduanero (ej: N8687)
        /// </summary>
        [Required(ErrorMessage = "El número de carné es obligatorio")]
        [StringLength(20, ErrorMessage = "El número de carné no puede exceder 20 caracteres")]
        public string NumeroCarnet { get; set; } = string.Empty;

        /// <summary>
        /// Nombre completo del titular
        /// </summary>
        [Required(ErrorMessage = "El nombre del titular es obligatorio")]
        [StringLength(200, ErrorMessage = "El nombre no puede exceder 200 caracteres")]
        public string NombreTitular { get; set; } = string.Empty;

        /// <summary>
        /// Apellidos del titular
        /// </summary>
        [StringLength(200, ErrorMessage = "Los apellidos no pueden exceder 200 caracteres")]
        public string? ApellidosTitular { get; set; }

        /// <summary>
        /// RUT del titular (ej: 15.970.128-K)
        /// </summary>
        [Required(ErrorMessage = "El RUT es obligatorio")]
        [StringLength(15, ErrorMessage = "El RUT no puede exceder 15 caracteres")]
        [RegularExpression(@"^\d{1,2}\.\d{3}\.\d{3}-[A-Z]$", ErrorMessage = "El formato del RUT no es válido")]
        public string Rut { get; set; } = string.Empty;

        /// <summary>
        /// Fecha de emisión del carné
        /// </summary>
        [Required(ErrorMessage = "La fecha de emisión es obligatoria")]
        public DateTime FechaEmision { get; set; }

        /// <summary>
        /// Fecha de vencimiento del carné
        /// </summary>
        public DateTime? FechaVencimiento { get; set; }

        /// <summary>
        /// Número de resolución
        /// </summary>
        [StringLength(10, ErrorMessage = "La resolución no puede exceder 10 caracteres")]
        public string? Resolucion { get; set; }

        /// <summary>
        /// Fecha de resolución
        /// </summary>
        public DateTime? FechaResolucion { get; set; }

        /// <summary>
        /// Código AGAD
        /// </summary>
        [StringLength(10, ErrorMessage = "El código AGAD no puede exceder 10 caracteres")]
        public string? AgadCod { get; set; }

        /// <summary>
        /// Entidad emisora del carné
        /// </summary>
        [StringLength(200, ErrorMessage = "La entidad emisora no puede exceder 200 caracteres")]
        public string? EntidadEmisora { get; set; }

        /// <summary>
        /// Estado del carné (Vigente, Vencido, etc.)
        /// </summary>
        [StringLength(20, ErrorMessage = "El estado no puede exceder 20 caracteres")]
        public string? Estado { get; set; }

        /// <summary>
        /// Nombre del archivo PDF original
        /// </summary>
        [Required(ErrorMessage = "El nombre del archivo es obligatorio")]
        [StringLength(255, ErrorMessage = "El nombre del archivo no puede exceder 255 caracteres")]
        public string NombreArchivo { get; set; } = string.Empty;

        /// <summary>
        /// Ruta del archivo PDF en el servidor
        /// </summary>
        [StringLength(500, ErrorMessage = "La ruta del archivo no puede exceder 500 caracteres")]
        public string? RutaArchivo { get; set; }

        /// <summary>
        /// Tamaño del archivo en bytes
        /// </summary>
        public long TamanioArchivo { get; set; }

        /// <summary>
        /// Hash del archivo para verificar integridad
        /// </summary>
        [StringLength(64, ErrorMessage = "El hash no puede exceder 64 caracteres")]
        public string? HashArchivo { get; set; }

        /// <summary>
        /// Nivel de confianza de la extracción (0-1)
        /// </summary>
        [Range(0, 1, ErrorMessage = "La confianza debe estar entre 0 y 1")]
        public decimal ConfianzaExtraccion { get; set; }

        /// <summary>
        /// Método de extracción utilizado
        /// </summary>
        [StringLength(50, ErrorMessage = "El método de extracción no puede exceder 50 caracteres")]
        public string MetodoExtraccion { get; set; } = "Azure Form Recognizer";

        /// <summary>
        /// Texto extraído completo del PDF
        /// </summary>
        [Column(TypeName = "TEXT")]
        public string? TextoExtraido { get; set; }

        /// <summary>
        /// Datos JSON de la extracción original
        /// </summary>
        [Column(TypeName = "TEXT")]
        public string? DatosExtraccionJson { get; set; }

        /// <summary>
        /// Comentarios o notas adicionales
        /// </summary>
        [StringLength(1000, ErrorMessage = "Los comentarios no pueden exceder 1000 caracteres")]
        public string? Comentarios { get; set; }

        /// <summary>
        /// Indica si el carné está activo
        /// </summary>
        public bool EstaActivo { get; set; } = true;

        /// <summary>
        /// Fecha de creación del registro
        /// </summary>
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Fecha de última modificación
        /// </summary>
        public DateTime FechaModificacion { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Usuario que creó el registro
        /// </summary>
        [StringLength(100, ErrorMessage = "El usuario no puede exceder 100 caracteres")]
        public string? UsuarioCreacion { get; set; }

        /// <summary>
        /// Usuario que modificó por última vez
        /// </summary>
        [StringLength(100, ErrorMessage = "El usuario no puede exceder 100 caracteres")]
        public string? UsuarioModificacion { get; set; }

        // Propiedades de navegación
        public virtual ICollection<Procesamiento> Procesamientos { get; set; } = new List<Procesamiento>();
        public virtual ICollection<ErrorProcesamiento> Errores { get; set; } = new List<ErrorProcesamiento>();

        // Propiedades calculadas
        /// <summary>
        /// Nombre completo del titular
        /// </summary>
        [NotMapped]
        public string NombreCompleto => $"{NombreTitular} {ApellidosTitular}".Trim();

        /// <summary>
        /// Indica si el carné está vencido
        /// </summary>
        [NotMapped]
        public bool EstaVencido => FechaVencimiento.HasValue && FechaVencimiento.Value < DateTime.Today;

        /// <summary>
        /// Días restantes hasta el vencimiento
        /// </summary>
        [NotMapped]
        public int? DiasHastaVencimiento => FechaVencimiento?.Subtract(DateTime.Today).Days;

        /// <summary>
        /// Tamaño del archivo formateado
        /// </summary>
        [NotMapped]
        public string TamanioArchivoFormateado
        {
            get
            {
                string[] sizes = { "B", "KB", "MB", "GB" };
                double len = TamanioArchivo;
                int order = 0;
                while (len >= 1024 && order < sizes.Length - 1)
                {
                    order++;
                    len = len / 1024;
                }
                return $"{len:0.##} {sizes[order]}";
            }
        }
    }
} 