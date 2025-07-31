using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarnetAduaneroProcessor.Core.Models
{
    /// <summary>
    /// Modelo que representa un Documento de Recepción (DR) procesado
    /// </summary>
    public class DocumentoRecepcion
    {
        /// <summary>
        /// Identificador único del documento
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Número del documento de recepción (ej: 2025-5742)
        /// </summary>
        [Required(ErrorMessage = "El número del documento es obligatorio")]
        [StringLength(20, ErrorMessage = "El número del documento no puede exceder 20 caracteres")]
        public string NumeroDocumento { get; set; } = string.Empty;

        /// <summary>
        /// Situación del documento (ej: NORMAL)
        /// </summary>
        [StringLength(20, ErrorMessage = "La situación no puede exceder 20 caracteres")]
        public string? SituacionDocumento { get; set; }

        /// <summary>
        /// Número de manifiesto (ej: 253388)
        /// </summary>
        [StringLength(20, ErrorMessage = "El número de manifiesto no puede exceder 20 caracteres")]
        public string? NumeroManifiesto { get; set; }

        /// <summary>
        /// Fecha del manifiesto SNA (ej: 20/03/2025)
        /// </summary>
        public DateTime? FechaManifiestoSna { get; set; }

        /// <summary>
        /// Fecha de inicio de almacenaje (ej: 24/03/2025)
        /// </summary>
        public DateTime? FechaInicioAlmacenaje { get; set; }

        /// <summary>
        /// Fecha de inicio de 90 días (ej: 24/03/2025)
        /// </summary>
        public DateTime? FechaInicio90Dias { get; set; }

        /// <summary>
        /// Fecha de término de 90 días (ej: 21/06/2025)
        /// </summary>
        public DateTime? FechaTermino90Dias { get; set; }

        /// <summary>
        /// Tipo de documento (ej: CONTENEDOR IMPORTACION - POR MANIFIESTO - INDIRECTO)
        /// </summary>
        [StringLength(100, ErrorMessage = "El tipo de documento no puede exceder 100 caracteres")]
        public string? TipoDocumento { get; set; }

        /// <summary>
        /// BL Armador (ej: MEDUOY612089)
        /// </summary>
        [StringLength(50, ErrorMessage = "El BL Armador no puede exceder 50 caracteres")]
        public string? BlArmador { get; set; }

        /// <summary>
        /// Consignatario (ej: JIN & YIN & WANG LIMITADA)
        /// </summary>
        [StringLength(200, ErrorMessage = "El consignatario no puede exceder 200 caracteres")]
        public string? Consignatario { get; set; }

        /// <summary>
        /// RUT del consignatario (ej: 77816676-3)
        /// </summary>
        [StringLength(15, ErrorMessage = "El RUT del consignatario no puede exceder 15 caracteres")]
        public string? RutConsignatario { get; set; }

        /// <summary>
        /// Dirección del consignatario (ej: UNION LATINO AMERICANA 254-3-SANTIAGO)
        /// </summary>
        [StringLength(300, ErrorMessage = "La dirección no puede exceder 300 caracteres")]
        public string? DireccionConsignatario { get; set; }

        /// <summary>
        /// Línea operadora (ej: MEDITERRANEAN SHIPPPING CO.)
        /// </summary>
        [StringLength(200, ErrorMessage = "La línea operadora no puede exceder 200 caracteres")]
        public string? LineaOperadora { get; set; }

        /// <summary>
        /// Servicio de almacenaje (ej: ALMACENAJE DE CONTENEDOR 40 NORMAL (ZP))
        /// </summary>
        [StringLength(200, ErrorMessage = "El servicio de almacenaje no puede exceder 200 caracteres")]
        public string? ServicioAlmacenaje { get; set; }

        /// <summary>
        /// Guarda almacén (ej: MELLA JUAN)
        /// </summary>
        [StringLength(200, ErrorMessage = "El guarda almacén no puede exceder 200 caracteres")]
        public string? GuardaAlmacen { get; set; }

        /// <summary>
        /// RUT del guarda almacén (ej: 13196306-3)
        /// </summary>
        [StringLength(15, ErrorMessage = "El RUT del guarda almacén no puede exceder 15 caracteres")]
        public string? RutGuardaAlmacen { get; set; }

        /// <summary>
        /// Puerto de origen (ej: CNNGB)
        /// </summary>
        [StringLength(20, ErrorMessage = "El puerto de origen no puede exceder 20 caracteres")]
        public string? PuertoOrigen { get; set; }

        /// <summary>
        /// Puerto de embarque (ej: CNNGB)
        /// </summary>
        [StringLength(20, ErrorMessage = "El puerto de embarque no puede exceder 20 caracteres")]
        public string? PuertoEmbarque { get; set; }

        /// <summary>
        /// Puerto de descarga (ej: SAN ANTONIO)
        /// </summary>
        [StringLength(50, ErrorMessage = "El puerto de descarga no puede exceder 50 caracteres")]
        public string? PuertoDescarga { get; set; }

        /// <summary>
        /// Puerto de destino (ej: SAN ANTONIO)
        /// </summary>
        [StringLength(50, ErrorMessage = "El puerto de destino no puede exceder 50 caracteres")]
        public string? PuertoDestino { get; set; }

        /// <summary>
        /// Puerto de transbordo (ej: CALLAO)
        /// </summary>
        [StringLength(20, ErrorMessage = "El puerto de transbordo no puede exceder 20 caracteres")]
        public string? PuertoTransbordo { get; set; }

        /// <summary>
        /// Nave/Viaje (ej: ONE IBIS / 2502)
        /// </summary>
        [StringLength(100, ErrorMessage = "La nave/viaje no puede exceder 100 caracteres")]
        public string? NaveViaje { get; set; }

        /// <summary>
        /// Almacén (ej: PATIO ZONA PRIMARIA)
        /// </summary>
        [StringLength(100, ErrorMessage = "El almacén no puede exceder 100 caracteres")]
        public string? Almacen { get; set; }

        /// <summary>
        /// Destino de carga (ej: IMPORTACION)
        /// </summary>
        [StringLength(50, ErrorMessage = "El destino de carga no puede exceder 50 caracteres")]
        public string? DestinoCarga { get; set; }

        /// <summary>
        /// Zona (ej: PRIMARIA)
        /// </summary>
        [StringLength(50, ErrorMessage = "La zona no puede exceder 50 caracteres")]
        public string? Zona { get; set; }

        /// <summary>
        /// Origen (ej: IMPORTACION)
        /// </summary>
        [StringLength(50, ErrorMessage = "El origen no puede exceder 50 caracteres")]
        public string? Origen { get; set; }

        /// <summary>
        /// Tipo de bulto (ej: H40 40 CONTENEDOR HIGH CUBE STD)
        /// </summary>
        [StringLength(100, ErrorMessage = "El tipo de bulto no puede exceder 100 caracteres")]
        public string? TipoBulto { get; set; }

        /// <summary>
        /// Contenedor (ej: MSBU 827710-2)
        /// </summary>
        [StringLength(50, ErrorMessage = "El contenedor no puede exceder 50 caracteres")]
        public string? Contenedor { get; set; }

        /// <summary>
        /// TATC (ej: 2025391760025982)
        /// </summary>
        [StringLength(50, ErrorMessage = "El TATC no puede exceder 50 caracteres")]
        public string? Tatc { get; set; }

        /// <summary>
        /// Cantidad (ej: 1)
        /// </summary>
        [StringLength(10, ErrorMessage = "La cantidad no puede exceder 10 caracteres")]
        public string? Cantidad { get; set; }

        /// <summary>
        /// Peso (ej: 17.540,00)
        /// </summary>
        [StringLength(20, ErrorMessage = "El peso no puede exceder 20 caracteres")]
        public string? Peso { get; set; }

        /// <summary>
        /// Volumen (ej: 0,00)
        /// </summary>
        [StringLength(20, ErrorMessage = "El volumen no puede exceder 20 caracteres")]
        public string? Volumen { get; set; }

        /// <summary>
        /// Estado (ej: BUENO)
        /// </summary>
        [StringLength(20, ErrorMessage = "El estado no puede exceder 20 caracteres")]
        public string? Estado { get; set; }

        /// <summary>
        /// RUT del emisor (ej: 12452809-7)
        /// </summary>
        [StringLength(15, ErrorMessage = "El RUT del emisor no puede exceder 15 caracteres")]
        public string? RutEmisor { get; set; }

        /// <summary>
        /// Fecha de emisión (ej: 25-06-2025 15:10:48)
        /// </summary>
        [StringLength(50, ErrorMessage = "La fecha de emisión no puede exceder 50 caracteres")]
        public string? FechaEmision { get; set; }

        /// <summary>
        /// Medio de emisión (ej: WEB)
        /// </summary>
        [StringLength(20, ErrorMessage = "El medio de emisión no puede exceder 20 caracteres")]
        public string? MedioEmision { get; set; }

        /// <summary>
        /// Forwarder (ej: NOMBRE FORWARDER)
        /// </summary>
        [StringLength(200, ErrorMessage = "El forwarder no puede exceder 200 caracteres")]
        public string? Forwarder { get; set; }

        /// <summary>
        /// Agencia de aduana
        /// </summary>
        [StringLength(200, ErrorMessage = "La agencia de aduana no puede exceder 200 caracteres")]
        public string? AgenciaAduana { get; set; }

        /// <summary>
        /// Ubicación (ej: 10010101)
        /// </summary>
        [StringLength(20, ErrorMessage = "La ubicación no puede exceder 20 caracteres")]
        public string? Ubicacion { get; set; }

        /// <summary>
        /// Marcas (ej: CONSIGNATARIO: JIN & YIN & WANG LTDA N M (GCI #59111-hum1))
        /// </summary>
        [StringLength(500, ErrorMessage = "Las marcas no pueden exceder 500 caracteres")]
        public string? Marcas { get; set; }

        // Metadatos del archivo
        /// <summary>
        /// Nombre del archivo PNG original
        /// </summary>
        [Required(ErrorMessage = "El nombre del archivo es obligatorio")]
        [StringLength(255, ErrorMessage = "El nombre del archivo no puede exceder 255 caracteres")]
        public string NombreArchivo { get; set; } = string.Empty;

        /// <summary>
        /// Ruta del archivo PNG en el servidor
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
        /// Texto extraído completo del PNG
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
        /// Indica si el documento está activo
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
        /// Indica si el documento es válido (tiene los campos requeridos)
        /// </summary>
        public bool EsValido { get; set; } = false;

        /// <summary>
        /// Indica si el documento tiene errores de procesamiento
        /// </summary>
        [NotMapped]
        public bool TieneErrores => Errores.Any(e => !e.Resuelto);

        /// <summary>
        /// Obtiene el último error de procesamiento
        /// </summary>
        [NotMapped]
        public ErrorProcesamiento? UltimoError => Errores
            .Where(e => !e.Resuelto)
            .OrderByDescending(e => e.FechaError)
            .FirstOrDefault();

        /// <summary>
        /// Obtiene el último procesamiento exitoso
        /// </summary>
        [NotMapped]
        public Procesamiento? UltimoProcesamiento => Procesamientos
            .Where(p => p.Estado == "Exitoso")
            .OrderByDescending(p => p.FechaProcesamiento)
            .FirstOrDefault();
    }
} 