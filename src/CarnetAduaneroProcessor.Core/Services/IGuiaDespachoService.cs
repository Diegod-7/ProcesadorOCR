using CarnetAduaneroProcessor.Core.Models;

namespace CarnetAduaneroProcessor.Core.Services
{
    /// <summary>
    /// Interfaz para el servicio de procesamiento de documentos de Guía de Despacho Electrónica
    /// </summary>
    public interface IGuiaDespachoService
    {
        /// <summary>
        /// Extrae datos de un archivo PNG de documento de Guía de Despacho Electrónica
        /// </summary>
        /// <param name="filePath">Ruta del archivo PNG</param>
        /// <returns>Documento de Guía de Despacho procesado</returns>
        Task<GuiaDespacho> ExtraerDatosAsync(string filePath);

        /// <summary>
        /// Extrae datos de un stream de archivo PNG
        /// </summary>
        /// <param name="fileStream">Stream del archivo PNG</param>
        /// <param name="fileName">Nombre del archivo</param>
        /// <returns>Documento de Guía de Despacho procesado</returns>
        Task<GuiaDespacho> ExtraerDatosAsync(Stream fileStream, string fileName);

        /// <summary>
        /// Extrae datos de un array de bytes
        /// </summary>
        /// <param name="fileBytes">Bytes del archivo PNG</param>
        /// <param name="fileName">Nombre del archivo</param>
        /// <returns>Documento de Guía de Despacho procesado</returns>
        Task<GuiaDespacho> ExtraerDatosAsync(byte[] fileBytes, string fileName);

        /// <summary>
        /// Procesa texto OCR para extraer datos de documento de Guía de Despacho Electrónica
        /// </summary>
        /// <param name="textoOcr">Texto extraído por OCR</param>
        /// <returns>Documento de Guía de Despacho procesado</returns>
        Task<GuiaDespacho> ProcesarTextoOcrAsync(string textoOcr);

        /// <summary>
        /// Valida si el archivo es un PNG válido
        /// </summary>
        /// <param name="filePath">Ruta del archivo</param>
        /// <returns>True si es válido</returns>
        Task<bool> ValidarPngAsync(string filePath);

        /// <summary>
        /// Valida si el stream es un PNG válido
        /// </summary>
        /// <param name="fileStream">Stream del archivo</param>
        /// <returns>True si es válido</returns>
        Task<bool> ValidarPngAsync(Stream fileStream);
    }
} 