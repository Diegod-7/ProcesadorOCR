using System.ComponentModel.DataAnnotations;

namespace CarnetAduaneroProcessor.Core.Models
{
    /// <summary>
    /// Modelo para documentos de Guía de Despacho Electrónica
    /// </summary>
    public class GuiaDespacho
    {
        [Key]
        public int Id { get; set; }

        // Campos críticos (información principal del documento)
        [Required(ErrorMessage = "El número de guía es requerido")]
        public string NumeroGuia { get; set; } = string.Empty;

        [Required(ErrorMessage = "El RUT del emisor es requerido")]
        public string RutEmisor { get; set; } = string.Empty;

        [Required(ErrorMessage = "La fecha del documento es requerida")]
        public DateTime? FechaDocumento { get; set; }

        // Información del emisor
        public string NombreEmisor { get; set; } = string.Empty;
        public string GiroEmisor { get; set; } = string.Empty;
        public string DireccionEmisor { get; set; } = string.Empty;
        public string CiudadEmisor { get; set; } = string.Empty;

        // Información del receptor
        public string NombreReceptor { get; set; } = string.Empty;
        public string RutReceptor { get; set; } = string.Empty;
        public string GiroReceptor { get; set; } = string.Empty;
        public string DireccionReceptor { get; set; } = string.Empty;
        public string CiudadReceptor { get; set; } = string.Empty;
        public string ComunaReceptor { get; set; } = string.Empty;

        // Información de transporte
        public string Transportista { get; set; } = string.Empty;
        public string PatenteVehiculo { get; set; } = string.Empty;
        public string Chofer { get; set; } = string.Empty;
        public string Ubicacion { get; set; } = string.Empty;
        public string Origen { get; set; } = string.Empty;
        public string Destino { get; set; } = string.Empty;

        // Información aduanera
        public string NumeroDespacho { get; set; } = string.Empty;
        public DateTime? FechaDespacho { get; set; }
        public string Aduana { get; set; } = string.Empty;
        public string Referencia { get; set; } = string.Empty;
        public string TipoOperacion { get; set; } = string.Empty;
        public string NumeroDAU { get; set; } = string.Empty;
        public string ConocimientoEmbarque { get; set; } = string.Empty;
        public string Manifiesto { get; set; } = string.Empty;
        public decimal? Peso { get; set; }
        public decimal? CIFUSD { get; set; }

        // Observaciones
        public string Observaciones { get; set; } = string.Empty;

        // Información de bultos
        public int? CantidadBultos { get; set; }
        public string TipoBulto { get; set; } = string.Empty;

        // Totales
        public decimal? Neto { get; set; }
        public decimal? IVA { get; set; }
        public decimal? Total { get; set; }

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
            EsValido = !string.IsNullOrWhiteSpace(NumeroGuia) && 
                      !string.IsNullOrWhiteSpace(RutEmisor) && 
                      FechaDocumento.HasValue;
            
            return EsValido;
        }

        /// <summary>
        /// Obtiene un resumen de los campos críticos
        /// </summary>
        public string ObtenerResumen()
        {
            return $"Guía: {NumeroGuia}, Emisor: {RutEmisor}, Fecha: {FechaDocumento:dd/MM/yyyy}";
        }
    }
} 