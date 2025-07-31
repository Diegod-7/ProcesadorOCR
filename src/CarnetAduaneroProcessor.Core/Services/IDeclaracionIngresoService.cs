using CarnetAduaneroProcessor.Core.Models;

namespace CarnetAduaneroProcessor.Core.Services
{
    /// <summary>
    /// Interfaz para el servicio de procesamiento de Declaraciones de Ingreso
    /// </summary>
    public interface IDeclaracionIngresoService
    {
        /// <summary>
        /// Extrae datos de un archivo PDF de Declaración de Ingreso
        /// </summary>
        /// <param name="filePath">Ruta del archivo PDF</param>
        /// <returns>Modelo DeclaracionIngreso con los datos extraídos</returns>
        Task<DeclaracionIngreso> ExtraerDatosAsync(string filePath);

        /// <summary>
        /// Extrae datos de un stream de archivo PDF
        /// </summary>
        /// <param name="fileStream">Stream del archivo PDF</param>
        /// <param name="fileName">Nombre del archivo</param>
        /// <returns>Modelo DeclaracionIngreso con los datos extraídos</returns>
        Task<DeclaracionIngreso> ExtraerDatosAsync(Stream fileStream, string fileName);

        /// <summary>
        /// Extrae datos de un array de bytes
        /// </summary>
        /// <param name="fileBytes">Bytes del archivo PDF</param>
        /// <param name="fileName">Nombre del archivo</param>
        /// <returns>Modelo DeclaracionIngreso con los datos extraídos</returns>
        Task<DeclaracionIngreso> ExtraerDatosAsync(byte[] fileBytes, string fileName);

        /// <summary>
        /// Procesa texto OCR para extraer datos de declaración de ingreso
        /// </summary>
        /// <param name="textoOcr">Texto extraído por OCR</param>
        /// <returns>Datos procesados de la declaración</returns>
        Task<DeclaracionIngreso> ProcesarTextoOcrAsync(string textoOcr);

        /// <summary>
        /// Valida si el archivo es un PDF válido
        /// </summary>
        /// <param name="filePath">Ruta del archivo</param>
        /// <returns>True si es un PDF válido</returns>
        Task<bool> ValidarPdfAsync(string filePath);

        /// <summary>
        /// Valida si el stream es un PDF válido
        /// </summary>
        /// <param name="fileStream">Stream del archivo</param>
        /// <returns>True si es un PDF válido</returns>
        Task<bool> ValidarPdfAsync(Stream fileStream);
    }
} 