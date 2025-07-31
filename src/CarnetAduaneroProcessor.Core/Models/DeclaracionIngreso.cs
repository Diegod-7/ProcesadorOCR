using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarnetAduaneroProcessor.Core.Models
{
    /// <summary>
    /// Modelo que representa una Declaración de Ingreso (DI) procesada
    /// </summary>
    public class DeclaracionIngreso
    {
        /// <summary>
        /// Identificador único de la declaración
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Número de identificación principal (ej: 4700045635-3)
        /// </summary>
        [Required(ErrorMessage = "El número de identificación es obligatorio")]
        [StringLength(20, ErrorMessage = "El número de identificación no puede exceder 20 caracteres")]
        public string NumeroIdentificacion { get; set; } = string.Empty;

        /// <summary>
        /// Fecha de vencimiento (ej: 02/04/2025)
        /// </summary>
        public DateTime? FechaVencimiento { get; set; }

        /// <summary>
        /// Tipo de operación (ej: IMPORT.CTDO ANTIC.)
        /// </summary>
        [StringLength(50, ErrorMessage = "El tipo de operación no puede exceder 50 caracteres")]
        public string? TipoOperacion { get; set; }

        /// <summary>
        /// Código de tipo de operación (ej: 151)
        /// </summary>
        [StringLength(10, ErrorMessage = "El código de tipo de operación no puede exceder 10 caracteres")]
        public string? CodigoTipoOperacion { get; set; }

        /// <summary>
        /// Tipo de bulto (ej: CONT40 074 1)
        /// </summary>
        [StringLength(50, ErrorMessage = "El tipo de bulto no puede exceder 50 caracteres")]
        public string? TipoBulto { get; set; }

        /// <summary>
        /// Peso bruto (ej: 17.540,00)
        /// </summary>
        [StringLength(20, ErrorMessage = "El peso bruto no puede exceder 20 caracteres")]
        public string? PesoBruto { get; set; }

        /// <summary>
        /// Sello del contenedor (ej: MSBU 827710-2 SELLO FX39286687)
        /// </summary>
        [StringLength(100, ErrorMessage = "El sello del contenedor no puede exceder 100 caracteres")]
        public string? SelloContenedor { get; set; }

        /// <summary>
        /// Fecha de aceptación (ej: 21 18/03/2025)
        /// </summary>
        public DateTime? FechaAceptacion { get; set; }

        /// <summary>
        /// Total a pagar (ej: 5.632.525)
        /// </summary>
        [StringLength(20, ErrorMessage = "El total a pagar no puede exceder 20 caracteres")]
        public string? TotalPagar { get; set; }

        // Campos adicionales del documento
        /// <summary>
        /// Aduana (ej: SAN ANTONIO)
        /// </summary>
        [StringLength(100, ErrorMessage = "La aduana no puede exceder 100 caracteres")]
        public string? Aduana { get; set; }

        /// <summary>
        /// Despachante (ej: WALTER PEREZ SALAS)
        /// </summary>
        [StringLength(200, ErrorMessage = "El despachante no puede exceder 200 caracteres")]
        public string? Despachante { get; set; }

        /// <summary>
        /// Nombre del importador (ej: WALTER PEREZ SALAS)
        /// </summary>
        [StringLength(200, ErrorMessage = "El nombre del importador no puede exceder 200 caracteres")]
        public string? NombreImportador { get; set; }

        /// <summary>
        /// RUT del importador (ej: 77.816.676-3)
        /// </summary>
        [StringLength(15, ErrorMessage = "El RUT del importador no puede exceder 15 caracteres")]
        public string? RutImportador { get; set; }

        /// <summary>
        /// Descripción de mercancías
        /// </summary>
        [StringLength(500, ErrorMessage = "La descripción de mercancías no puede exceder 500 caracteres")]
        public string? DescripcionMercancias { get; set; }

        /// <summary>
        /// Consignatario o Importador (ej: JIN & YIN & WANG LIMITADA)
        /// </summary>
        [StringLength(200, ErrorMessage = "El consignatario no puede exceder 200 caracteres")]
        public string? Consignatario { get; set; }

        /// <summary>
        /// RUT del consignatario (ej: 77.816.676-3)
        /// </summary>
        [StringLength(15, ErrorMessage = "El RUT del consignatario no puede exceder 15 caracteres")]
        public string? RutConsignatario { get; set; }

        /// <summary>
        /// Consignante (ej: BOHUA TRADE CO., LIMITED)
        /// </summary>
        [StringLength(200, ErrorMessage = "El consignante no puede exceder 200 caracteres")]
        public string? Consignante { get; set; }

        /// <summary>
        /// País de origen (ej: CHINA)
        /// </summary>
        [StringLength(50, ErrorMessage = "El país de origen no puede exceder 50 caracteres")]
        public string? PaisOrigen { get; set; }

        /// <summary>
        /// Puerto de embarque (ej: NWGBO)
        /// </summary>
        [StringLength(50, ErrorMessage = "El puerto de embarque no puede exceder 50 caracteres")]
        public string? PuertoEmbarque { get; set; }

        /// <summary>
        /// Puerto de desembarque (ej: SAN ANTONIO)
        /// </summary>
        [StringLength(50, ErrorMessage = "El puerto de desembarque no puede exceder 50 caracteres")]
        public string? PuertoDesembarque { get; set; }

        /// <summary>
        /// Compañía transportista (ej: MEDITERRANEAN SHIPPING CO SA)
        /// </summary>
        [StringLength(200, ErrorMessage = "La compañía transportista no puede exceder 200 caracteres")]
        public string? CompaniaTransportista { get; set; }

        /// <summary>
        /// Manifiesto (ej: 253388)
        /// </summary>
        [StringLength(20, ErrorMessage = "El manifiesto no puede exceder 20 caracteres")]
        public string? Manifiesto { get; set; }

        /// <summary>
        /// Documento de transporte (ej: MEDUOY612089)
        /// </summary>
        [StringLength(50, ErrorMessage = "El documento de transporte no puede exceder 50 caracteres")]
        public string? DocumentoTransporte { get; set; }

        /// <summary>
        /// Valor CIF (ej: 30.077,16)
        /// </summary>
        [StringLength(20, ErrorMessage = "El valor CIF no puede exceder 20 caracteres")]
        public string? ValorCif { get; set; }

        /// <summary>
        /// Valor FOB (ej: 27.819,95)
        /// </summary>
        [StringLength(20, ErrorMessage = "El valor FOB no puede exceder 20 caracteres")]
        public string? ValorFob { get; set; }

        /// <summary>
        /// Flete (ej: 1.700,81)
        /// </summary>
        [StringLength(20, ErrorMessage = "El flete no puede exceder 20 caracteres")]
        public string? Flete { get; set; }

        /// <summary>
        /// Seguro (ej: 556,40)
        /// </summary>
        [StringLength(20, ErrorMessage = "El seguro no puede exceder 20 caracteres")]
        public string? Seguro { get; set; }

        /// <summary>
        /// Moneda (ej: DOLAR USA)
        /// </summary>
        [StringLength(20, ErrorMessage = "La moneda no puede exceder 20 caracteres")]
        public string? Moneda { get; set; }

        /// <summary>
        /// Forma de pago (ej: ANTICIPO)
        /// </summary>
        [StringLength(50, ErrorMessage = "La forma de pago no puede exceder 50 caracteres")]
        public string? FormaPago { get; set; }

        /// <summary>
        /// Cláusula de compra (ej: CFR)
        /// </summary>
        [StringLength(10, ErrorMessage = "La cláusula de compra no puede exceder 10 caracteres")]
        public string? ClausulaCompra { get; set; }

        /// <summary>
        /// Certificado de origen (ej: F25MASGLUSX00002)
        /// </summary>
        [StringLength(50, ErrorMessage = "El certificado de origen no puede exceder 50 caracteres")]
        public string? CertificadoOrigen { get; set; }

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
        public string MetodoExtraccion { get; set; } = "Extracción Manual";

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
        /// Indica si la declaración está activa
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
        /// Indica si la declaración está vencida
        /// </summary>
        [NotMapped]
        public bool EstaVencida => FechaVencimiento.HasValue && FechaVencimiento.Value < DateTime.Today;

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

        /// <summary>
        /// Indica si todos los campos requeridos están completos
        /// </summary>
        [NotMapped]
        public bool EsValida => !string.IsNullOrWhiteSpace(NumeroIdentificacion);
    }
} 