using CarnetAduaneroProcessor.Core.Models;

namespace CarnetAduaneroProcessor.Core.Services
{
    /// <summary>
    /// Servicio para procesar documentos de Selección de Aforo
    /// </summary>
    public interface ISeleccionAforoService
    {
        /// <summary>
        /// Extrae datos de un archivo PNG de Selección de Aforo
        /// </summary>
        /// <param name="filePath">Ruta del archivo PNG</param>
        /// <returns>Datos extraídos del documento</returns>
        Task<SeleccionAforo> ExtraerDatosAsync(string filePath);

        /// <summary>
        /// Extrae datos de un stream de archivo PNG
        /// </summary>
        /// <param name="fileStream">Stream del archivo</param>
        /// <param name="fileName">Nombre del archivo</param>
        /// <returns>Datos extraídos del documento</returns>
        Task<SeleccionAforo> ExtraerDatosAsync(Stream fileStream, string fileName);

        /// <summary>
        /// Extrae datos de un array de bytes
        /// </summary>
        /// <param name="fileBytes">Bytes del archivo</param>
        /// <param name="fileName">Nombre del archivo</param>
        /// <returns>Datos extraídos del documento</returns>
        Task<SeleccionAforo> ExtraerDatosAsync(byte[] fileBytes, string fileName);

        /// <summary>
        /// Procesa texto OCR para extraer datos de Selección de Aforo
        /// </summary>
        /// <param name="textoOcr">Texto extraído por OCR</param>
        /// <returns>Datos estructurados del documento</returns>
        Task<SeleccionAforo> ProcesarTextoOcrAsync(string textoOcr);

        /// <summary>
        /// Valida si un archivo es un PNG válido
        /// </summary>
        /// <param name="filePath">Ruta del archivo</param>
        /// <returns>True si es un PNG válido</returns>
        Task<bool> ValidarPngAsync(string filePath);

        /// <summary>
        /// Valida si un stream es un PNG válido
        /// </summary>
        /// <param name="fileStream">Stream del archivo</param>
        /// <returns>True si es un PNG válido</returns>
        Task<bool> ValidarPngAsync(Stream fileStream);
    }
} 