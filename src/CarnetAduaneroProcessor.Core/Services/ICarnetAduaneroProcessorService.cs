using CarnetAduaneroProcessor.Core.Models;

namespace CarnetAduaneroProcessor.Core.Services
{
    public interface ICarnetAduaneroProcessorService
    {
        /// <summary>
        /// Procesa el texto OCR extraído y retorna los datos estructurados del carné aduanero
        /// </summary>
        /// <param name="textoOcr">Texto extraído por OCR</param>
        /// <returns>Datos estructurados del carné aduanero</returns>
        Task<CarnetAduaneroData> ProcesarTextoOcrAsync(string textoOcr);

        /// <summary>
        /// Extrae datos de un archivo PNG de carné aduanero
        /// </summary>
        /// <param name="filePath">Ruta del archivo PNG</param>
        /// <returns>Datos extraídos del documento</returns>
        Task<CarnetAduaneroData> ExtraerDatosAsync(string filePath);

        /// <summary>
        /// Extrae datos de un stream de archivo PNG
        /// </summary>
        /// <param name="fileStream">Stream del archivo</param>
        /// <param name="fileName">Nombre del archivo</param>
        /// <returns>Datos extraídos del documento</returns>
        Task<CarnetAduaneroData> ExtraerDatosAsync(Stream fileStream, string fileName);

        /// <summary>
        /// Extrae datos de un array de bytes
        /// </summary>
        /// <param name="fileBytes">Bytes del archivo</param>
        /// <param name="fileName">Nombre del archivo</param>
        /// <returns>Datos extraídos del documento</returns>
        Task<CarnetAduaneroData> ExtraerDatosAsync(byte[] fileBytes, string fileName);

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