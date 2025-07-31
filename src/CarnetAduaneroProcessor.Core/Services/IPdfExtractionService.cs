using CarnetAduaneroProcessor.Core.Models;
using System.Drawing;

namespace CarnetAduaneroProcessor.Core.Services
{
    /// <summary>
    /// Interfaz para el servicio de extracción de datos de PDFs
    /// </summary>
    public interface IPdfExtractionService
    {
        /// <summary>
        /// Extrae datos de un archivo PDF de Carné Aduanero
        /// </summary>
        /// <param name="filePath">Ruta del archivo PDF</param>
        /// <returns>Modelo CarnetAduanero con los datos extraídos</returns>
        Task<CarnetAduanero> ExtraerDatosAsync(string filePath);

        /// <summary>
        /// Extrae datos de un stream de archivo PDF
        /// </summary>
        /// <param name="fileStream">Stream del archivo PDF</param>
        /// <param name="fileName">Nombre del archivo</param>
        /// <returns>Modelo CarnetAduanero con los datos extraídos</returns>
        Task<CarnetAduanero> ExtraerDatosAsync(Stream fileStream, string fileName);

        /// <summary>
        /// Extrae datos de un array de bytes
        /// </summary>
        /// <param name="fileBytes">Bytes del archivo PDF</param>
        /// <param name="fileName">Nombre del archivo</param>
        /// <returns>Modelo CarnetAduanero con los datos extraídos</returns>
        Task<CarnetAduanero> ExtraerDatosAsync(byte[] fileBytes, string fileName);

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

        /// <summary>
        /// Obtiene información básica del archivo PDF
        /// </summary>
        /// <param name="filePath">Ruta del archivo</param>
        /// <returns>Información del archivo</returns>
        Task<FileInfo> ObtenerInformacionArchivoAsync(string filePath);

        /// <summary>
        /// Calcula el hash SHA256 del archivo
        /// </summary>
        /// <param name="filePath">Ruta del archivo</param>
        /// <returns>Hash SHA256 del archivo</returns>
        Task<string> CalcularHashArchivoAsync(string filePath);

        /// <summary>
        /// Calcula el hash SHA256 del stream
        /// </summary>
        /// <param name="fileStream">Stream del archivo</param>
        /// <returns>Hash SHA256 en formato hexadecimal</returns>
        Task<string> CalcularHashArchivoAsync(Stream fileStream);

        /// <summary>
        /// Guarda las imágenes extraídas del PDF en una carpeta específica
        /// </summary>
        /// <param name="filePath>Ruta del archivo PDF</param>
        /// <param name="outputFolder">Carpeta donde guardar las imágenes (opcional)</param>
        /// <returns>Lista de rutas de las imágenes guardadas</returns>
        Task<List<string>> GuardarImagenesExtraidasAsync(string filePath, string outputFolder = null);

        /// <summary>
        /// Guarda las imágenes extraídas del PDF desde un stream
        /// </summary>
        /// <param name="fileStream">Stream del archivo PDF</param>
        /// <param name="fileName">Nombre del archivo PDF</param>
        /// <param name="outputFolder">Carpeta donde guardar las imágenes (opcional)</param>
        /// <returns>Lista de rutas de las imágenes guardadas</returns>
        Task<List<string>> GuardarImagenesExtraidasAsync(Stream fileStream, string fileName, string outputFolder = null);

        /// <summary>
        /// Extrae texto de una imagen usando Azure Computer Vision
        /// </summary>
        /// <param name="image">Imagen como Bitmap</param>
        /// <returns>Texto extraído de la imagen</returns>
        Task<string> ExtraerTextoConAzureVisionAsync(Bitmap image);

        /// <summary>
        /// Extrae texto de una imagen usando Tesseract OCR
        /// </summary>
        /// <param name="image">Imagen como Bitmap</param>
        /// <returns>Texto extraído de la imagen</returns>
        string ExtraerTextoConTesseract(Bitmap image);
    }
} 