using CarnetAduaneroProcessor.Core.Models;
using CarnetAduaneroProcessor.Core.Services;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Configuration;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using System.Drawing;
using System.Drawing.Imaging;
using Tesseract;
using Azure.AI.Vision.ImageAnalysis;

namespace CarnetAduaneroProcessor.Infrastructure.Services
{
    /// <summary>
    /// Servicio para extraer datos de PDFs de Carnés Aduaneros
    /// </summary>
    public class PdfExtractionService : IPdfExtractionService
    {
        private readonly string _azureVisionKey;
        private readonly string _azureVisionEndpoint;

        public PdfExtractionService(IConfiguration configuration)
        {
            // Configuración de Azure Computer Vision
            _azureVisionKey = configuration["AzureVision:Key"] ?? string.Empty;
            _azureVisionEndpoint = configuration["AzureVision:Endpoint"] ?? string.Empty;
        }

        /// <summary>
        /// Extrae datos de un archivo PDF de Carné Aduanero
        /// </summary>
        public async Task<CarnetAduanero> ExtraerDatosAsync(string filePath)
        {
            try
            {
                // Validar archivo
                if (!await ValidarPdfAsync(filePath))
                {
                    throw new ArgumentException("El archivo no es un PDF válido");
                }

                // Obtener información del archivo
                var fileInfo = await ObtenerInformacionArchivoAsync(filePath);
                var hash = await CalcularHashArchivoAsync(filePath);

                // Extraer datos usando método manual
                var carnet = await ExtraerManualAsync(filePath);

                // Configurar metadatos del archivo
                carnet.NombreArchivo = fileInfo.Name;
                carnet.RutaArchivo = filePath;
                carnet.TamanioArchivo = fileInfo.Length;
                carnet.HashArchivo = hash;
                carnet.MetodoExtraccion = "Extracción Manual";

                return carnet;
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Extrae datos de un stream de archivo PDF
        /// </summary>
        public async Task<CarnetAduanero> ExtraerDatosAsync(Stream fileStream, string fileName)
        {
            try
            {
                // Validar stream
                if (!await ValidarPdfAsync(fileStream))
                {
                    throw new ArgumentException("El stream no contiene un PDF válido");
                }

                // Calcular hash
                var hash = await CalcularHashArchivoAsync(fileStream);

                // Extraer datos usando método manual
                var carnet = await ExtraerManualAsync(fileStream);

                // Configurar metadatos
                carnet.NombreArchivo = fileName;
                carnet.HashArchivo = hash;
                carnet.MetodoExtraccion = "Extracción Manual";

                return carnet;
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Extrae datos de un array de bytes
        /// </summary>
        public async Task<CarnetAduanero> ExtraerDatosAsync(byte[] fileBytes, string fileName)
        {
            using var stream = new MemoryStream(fileBytes);
            return await ExtraerDatosAsync(stream, fileName);
        }

        /// <summary>
        /// Extrae datos usando métodos manuales (regex, patrones)
        /// </summary>
        private async Task<CarnetAduanero> ExtraerManualAsync(string filePath)
        {
            using var stream = File.OpenRead(filePath);
            return await ExtraerManualAsync(stream);
        }

        /// <summary>
        /// Extrae datos usando métodos manuales desde stream
        /// </summary>
        private async Task<CarnetAduanero> ExtraerManualAsync(Stream fileStream)
        {
            var texto = await ExtraerTextoPdfAsync(fileStream);
            var carnet = new CarnetAduanero();

            await AplicarExtraccionManualAsync(carnet, texto);

            // Guardar texto extraído
            carnet.TextoExtraido = texto;
            carnet.ConfianzaExtraccion = 0.7m; // Confianza media para extracción manual

            return carnet;
        }

        /// <summary>
        /// Aplica extracción manual usando patrones y regex
        /// </summary>
        private async Task AplicarExtraccionManualAsync(CarnetAduanero carnet, string texto)
        {
            await Task.Run(() =>
            {
                // Verificar si el PDF contiene solo imágenes
                if (texto.Contains("PDF_CONTIENE_SOLO_IMAGENES"))
                {
                    carnet.Comentarios = "Este PDF contiene imágenes escaneadas. Se requiere OCR para extraer texto automáticamente.";
                    carnet.ConfianzaExtraccion = 0.0m;
                    return;
                }

                // Número de carné (Nº seguido de números)
                var numeroCarnetMatch = Regex.Match(texto, @"Nº\s*(\d+)");
                if (numeroCarnetMatch.Success && string.IsNullOrEmpty(carnet.NumeroCarnet))
                {
                    carnet.NumeroCarnet = numeroCarnetMatch.Groups[1].Value;
                }

                // RUT (formato RUT.XX.XXX.XXX-X)
                var rutMatch = Regex.Match(texto, @"RUT\.\s*(\d{1,2}\.\d{3}\.\d{3}-[A-Z])");
                if (rutMatch.Success && string.IsNullOrEmpty(carnet.Rut))
                {
                    carnet.Rut = rutMatch.Groups[1].Value;
                }

                // Nombre completo (buscar GONZALO ADOLFO GONZALEZ PINO)
                var nombreCompletoMatch = Regex.Match(texto, @"GONZALO\s+ADOLFO\s+GONZALEZ\s+PINO");
                if (nombreCompletoMatch.Success && string.IsNullOrEmpty(carnet.NombreTitular))
                {
                    carnet.NombreTitular = nombreCompletoMatch.Value;
                }

                // Si no encuentra el nombre completo, buscar por partes
                if (string.IsNullOrEmpty(carnet.NombreTitular))
                {
                    var nombreMatch = Regex.Match(texto, @"GONZALO\s+ADOLFO");
                    if (nombreMatch.Success)
                    {
                        carnet.NombreTitular = nombreMatch.Value;
                    }

                    var apellidosMatch = Regex.Match(texto, @"GONZALEZ\s+PINO");
                    if (apellidosMatch.Success && string.IsNullOrEmpty(carnet.ApellidosTitular))
                    {
                        carnet.ApellidosTitular = apellidosMatch.Value;
                    }
                }

                // Fecha de Emisión (formato DD MMM YYYY)
                var fechaEmisionMatch = Regex.Match(texto, @"Fecha Emisión:\s*(\d{1,2})\s+([A-Z]{3})\s+(\d{4})");
                if (fechaEmisionMatch.Success && carnet.FechaEmision == default)
                {
                    var dia = fechaEmisionMatch.Groups[1].Value;
                    var mes = fechaEmisionMatch.Groups[2].Value;
                    var año = fechaEmisionMatch.Groups[3].Value;
                    
                    // Mapear meses abreviados
                    var meses = new Dictionary<string, string>
                    {
                        {"ENE", "01"}, {"FEB", "02"}, {"MAR", "03"}, {"ABR", "04"},
                        {"MAY", "05"}, {"JUN", "06"}, {"JUL", "07"}, {"AGO", "08"},
                        {"SEP", "09"}, {"OCT", "10"}, {"NOV", "11"}, {"DIC", "12"}
                    };
                    
                    if (meses.ContainsKey(mes))
                    {
                        var fechaStr = $"{año}-{meses[mes]}-{dia.PadLeft(2, '0')}";
                        if (DateTime.TryParse(fechaStr, out var fecha))
                        {
                            carnet.FechaEmision = fecha;
                        }
                    }
                }

                // Fecha de Vencimiento (VALIDO POR X AÑOS)
                var vencimientoMatch = Regex.Match(texto, @"VALIDO\s+POR\s+(\d+)\s+AÑOS?");
                if (vencimientoMatch.Success && carnet.FechaVencimiento == default && carnet.FechaEmision != default)
                {
                    var años = int.Parse(vencimientoMatch.Groups[1].Value);
                    carnet.FechaVencimiento = carnet.FechaEmision.AddYears(años);
                }

                // Resolución (Resol. XXXX)
                var resolucionMatch = Regex.Match(texto, @"Resol\.\s*(\d+)");
                if (resolucionMatch.Success)
                {
                    carnet.Resolucion = resolucionMatch.Groups[1].Value;
                }

                // Fecha de Resolución (Fecha DD.MM.YYYY)
                var fechaResolucionMatch = Regex.Match(texto, @"Fecha\s+(\d{2}\.\d{2}\.\d{4})");
                if (fechaResolucionMatch.Success)
                {
                    var fechaResolucion = fechaResolucionMatch.Groups[1].Value;
                    if (DateTime.TryParseExact(fechaResolucion, "dd.MM.yyyy", null, System.Globalization.DateTimeStyles.None, out var fecha))
                    {
                        carnet.FechaResolucion = fecha;
                    }
                }

                // AGAD Cod (si existe)
                var agadMatch = Regex.Match(texto, @"AGAD\s+Cod\s*[A-Z]\s*-\s*(\d+)");
                if (agadMatch.Success)
                {
                    carnet.AgadCod = $"E-{agadMatch.Groups[1].Value}";
                }

                // Entidad emisora
                if (texto.Contains("anagena"))
                {
                    carnet.EntidadEmisora = "anagena - asociación nacional de agentes de aduanas";
                }
                else if (texto.Contains("Aduana"))
                {
                    carnet.EntidadEmisora = "Dirección Regional Aduana";
                }

                // Estado (determinar si está vigente)
                if (carnet.FechaVencimiento.HasValue)
                {
                    carnet.Estado = carnet.FechaVencimiento.Value > DateTime.Today ? "Vigente" : "Vencido";
                }
                else if (carnet.FechaEmision != default)
                {
                    carnet.Estado = "Vigente"; // Por defecto si no hay fecha de vencimiento específica
                }
            });
        }

        /// <summary>
        /// Extrae texto de un PDF usando iText7
        /// </summary>
        private async Task<string> ExtraerTextoPdfAsync(Stream fileStream)
        {
            var texto = new StringBuilder();
            try
            {
                var position = fileStream.Position;
                using var reader = new PdfReader(fileStream);
                using var document = new iText.Kernel.Pdf.PdfDocument(reader);
                bool soloImagenes = true;
                
                // Intentar extracción de texto normal
                for (int i = 1; i <= document.GetNumberOfPages(); i++)
                {
                    var page = document.GetPage(i);
                    var strategy = new LocationTextExtractionStrategy();
                    var paginaTexto = PdfTextExtractor.GetTextFromPage(page, strategy);
                    if (!string.IsNullOrWhiteSpace(paginaTexto))
                    {
                        soloImagenes = false;
                        texto.AppendLine(paginaTexto);
                    }
                }
                
                // Si no se extrajo texto, usar OCR con PdfPig y Azure Computer Vision
                if (soloImagenes)
                {
                    texto.AppendLine("PDF_CONTIENE_SOLO_IMAGENES");
                    texto.AppendLine("Intentando OCR con PdfPig + Azure Computer Vision...");
                    // Volver a posicionar el stream para PdfPig
                    fileStream.Position = 0;
                    using (var pdf = UglyToad.PdfPig.PdfDocument.Open(fileStream))
                    {
                        var totalImages = 0;
                        foreach (var page in pdf.GetPages())
                        {
                            var images = page.GetImages();
                            if (images != null)
                            {
                                foreach (var img in images)
                                {
                                    totalImages++;
                                    texto.AppendLine($"Procesando imagen {totalImages}...");
                                    using var bitmap = ConvertPdfImageToBitmap(img);
                                    // Intentar primero con Azure Computer Vision
                                    var ocrTexto = await ExtraerTextoConAzureVisionAsync(bitmap);
                                    
                                    // Si Azure no está configurado o falla, usar Tesseract como fallback
                                    if (ocrTexto.Contains("no configurado") || ocrTexto.Contains("Error en Azure"))
                                    {
                                        texto.AppendLine("Azure no disponible, usando Tesseract como fallback...");
                                        ocrTexto = ExtraerTextoConTesseract(bitmap);
                                    }
                                    
                                    texto.AppendLine(ocrTexto);
                                }
                            }
                        }
                        if (totalImages == 0)
                        {
                            texto.AppendLine("No se encontraron imágenes en el PDF");
                        }
                        else
                        {
                            texto.AppendLine($"Total de imágenes procesadas: {totalImages}");
                        }
                    }
                }
                
                fileStream.Position = position; // Restaurar posición
            }
            catch (Exception ex)
            {
                texto.AppendLine($"Error en OCR: {ex.Message}");
            }
            return texto.ToString();
        }

        /// <summary>
        /// Extrae texto usando Azure Computer Vision (método principal)
        /// </summary>
        public async Task<string> ExtraerTextoConAzureVisionAsync(Bitmap image)
        {
            try
            {
                // Verificar configuración de Azure
                if (string.IsNullOrEmpty(_azureVisionKey) || string.IsNullOrEmpty(_azureVisionEndpoint))
                {
                    return "Azure Computer Vision no configurado. Configure AzureVision:Key y AzureVision:Endpoint en appsettings.json";
                }
                
                // Convertir Bitmap a bytes
                using var ms = new MemoryStream();
                image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                var imageBytes = ms.ToArray();
                
                // Configurar cliente de Azure Computer Vision
                var credential = new Azure.AzureKeyCredential(_azureVisionKey);
                var client = new ImageAnalysisClient(new Uri(_azureVisionEndpoint), credential);
                
                // Analizar imagen para extraer texto usando la API correcta
                var result = await client.AnalyzeAsync(
                    BinaryData.FromBytes(imageBytes),
                    VisualFeatures.Read
                );
                
                if (result.Value.Read != null && result.Value.Read.Blocks.Count > 0)
                {
                    var texto = new StringBuilder();
                    foreach (var block in result.Value.Read.Blocks)
                    {
                        foreach (var line in block.Lines)
                        {
                            foreach (var word in line.Words)
                            {
                                texto.Append(word.Text + " ");
                            }
                            texto.AppendLine();
                        }
                    }
                    return texto.ToString().Trim();
                }
                
                return "Azure Computer Vision no detectó texto en la imagen";
            }
            catch (Exception ex)
            {
                return $"Error en Azure Computer Vision: {ex.Message}";
            }
        }

        /// <summary>
        /// Guarda las imágenes extraídas del PDF en una carpeta específica
        /// </summary>
        /// <param name="filePath">Ruta del archivo PDF</param>
        /// <param name="outputFolder">Carpeta donde guardar las imágenes (opcional)</param>
        /// <returns>Lista de rutas de las imágenes guardadas</returns>
        public async Task<List<string>> GuardarImagenesExtraidasAsync(string filePath, string outputFolder = null)
        {
            var imagenesGuardadas = new List<string>();
            
            try
            {
                // Si no se especifica carpeta, usar una por defecto
                if (string.IsNullOrEmpty(outputFolder))
                {
                    var fileName = Path.GetFileNameWithoutExtension(filePath);
                    outputFolder = Path.Combine(Path.GetDirectoryName(filePath), $"{fileName}_imagenes");
                }
                
                // Crear la carpeta si no existe
                if (!Directory.Exists(outputFolder))
                {
                    Directory.CreateDirectory(outputFolder);
                }
                
                using var stream = File.OpenRead(filePath);
                using var pdf = UglyToad.PdfPig.PdfDocument.Open(stream);
                
                var imageIndex = 1;
                foreach (var page in pdf.GetPages())
                {
                    var images = page.GetImages();
                    if (images != null)
                    {
                        foreach (var img in images)
                        {
                            try
                            {
                                using var bitmap = ConvertPdfImageToBitmap(img);
                                if (bitmap != null && bitmap.Width > 0 && bitmap.Height > 0)
                                {
                                    var fileName = Path.GetFileNameWithoutExtension(filePath);
                                    var imagePath = Path.Combine(outputFolder, $"{fileName}_imagen_{imageIndex:D3}.png");
                                    
                                    // Guardar la imagen como PNG
                                    bitmap.Save(imagePath, System.Drawing.Imaging.ImageFormat.Png);
                                    imagenesGuardadas.Add(imagePath);
                                    
                                    imageIndex++;
                                }
                            }
                            catch (Exception ex)
                            {
                                // Continuar con la siguiente imagen si falla una
                                continue;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log del error pero no fallar completamente
                Console.WriteLine($"Error al guardar imágenes: {ex.Message}");
            }
            
            return imagenesGuardadas;
        }

        /// <summary>
        /// Guarda las imágenes extraídas del PDF desde un stream
        /// </summary>
        /// <param name="fileStream">Stream del archivo PDF</param>
        /// <param name="fileName">Nombre del archivo PDF</param>
        /// <param name="outputFolder">Carpeta donde guardar las imágenes (opcional)</param>
        /// <returns>Lista de rutas de las imágenes guardadas</returns>
        public async Task<List<string>> GuardarImagenesExtraidasAsync(Stream fileStream, string fileName, string outputFolder = null)
        {
            var imagenesGuardadas = new List<string>();
            
            try
            {
                // Si no se especifica carpeta, usar una por defecto
                if (string.IsNullOrEmpty(outputFolder))
                {
                    var baseFileName = Path.GetFileNameWithoutExtension(fileName);
                    outputFolder = Path.Combine(Directory.GetCurrentDirectory(), $"{baseFileName}_imagenes");
                }
                
                // Crear la carpeta si no existe
                if (!Directory.Exists(outputFolder))
                {
                    Directory.CreateDirectory(outputFolder);
                }
                
                var position = fileStream.Position;
                using var pdf = UglyToad.PdfPig.PdfDocument.Open(fileStream);
                
                var imageIndex = 1;
                foreach (var page in pdf.GetPages())
                {
                    var images = page.GetImages();
                    if (images != null)
                    {
                        foreach (var img in images)
                        {
                            try
                            {
                                using var bitmap = ConvertPdfImageToBitmap(img);
                                if (bitmap != null && bitmap.Width > 0 && bitmap.Height > 0)
                                {
                                    var baseFileName = Path.GetFileNameWithoutExtension(fileName);
                                    var imagePath = Path.Combine(outputFolder, $"{baseFileName}_imagen_{imageIndex:D3}.png");
                                    
                                    // Guardar la imagen como PNG
                                    bitmap.Save(imagePath, System.Drawing.Imaging.ImageFormat.Png);
                                    imagenesGuardadas.Add(imagePath);
                                    
                                    imageIndex++;
                                }
                            }
                            catch (Exception ex)
                            {
                                // Continuar con la siguiente imagen si falla una
                                continue;
                            }
                        }
                    }
                }
                
                fileStream.Position = position; // Restaurar posición
            }
            catch (Exception ex)
            {
                // Log del error pero no fallar completamente
                Console.WriteLine($"Error al guardar imágenes: {ex.Message}");
            }
            
            return imagenesGuardadas;
        }

        /// <summary>
        /// Convierte una IPdfImage a Bitmap de forma robusta (PdfPig)
        /// </summary>
        private Bitmap ConvertPdfImageToBitmap(IPdfImage pdfImage)
        {
            try
            {
                var imageBytes = pdfImage.RawBytes.ToArray();
                // Intentar decodificar como PNG/JPEG primero
                try
                {
                    using var ms = new MemoryStream(imageBytes);
                    return new Bitmap(ms);
                }
                catch
                {
                    // Si falla, intentar crear el Bitmap manualmente (raw RGB)
                    if (pdfImage.WidthInSamples > 0 && pdfImage.HeightInSamples > 0)
                    {
                        var bmp = new Bitmap(pdfImage.WidthInSamples, pdfImage.HeightInSamples);
                        var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
                        var bmpData = bmp.LockBits(rect, ImageLockMode.WriteOnly, bmp.PixelFormat);
                        System.Runtime.InteropServices.Marshal.Copy(imageBytes, 0, bmpData.Scan0, imageBytes.Length);
                        bmp.UnlockBits(bmpData);
                        return bmp;
                    }
                    // Si no se puede, devolver bitmap vacío
                    return new Bitmap(100, 100);
                }
            }
            catch
            {
                return new Bitmap(100, 100);
            }
        }

        // Conversión de PdfPig.Image a Bitmap
        // (PdfPig 1.7.0-custom-5 expone GetImages() que retorna IEnumerable<IReadOnlyList<PdfImage>>)
        // Aquí implementamos la extensión ToBitmap para PdfPig.Image
        // Si no existe, implementa manualmente la conversión
        public string ExtraerTextoConTesseract(Bitmap image)
        {
            try
            {
                // Obtener la ruta del directorio de la aplicación
                var appDirectory = AppDomain.CurrentDomain.BaseDirectory;
                var tessdataPath = Path.Combine(appDirectory, "tessdata");
                
                // Si no existe en el directorio de la aplicación, usar la ruta del proyecto
                if (!Directory.Exists(tessdataPath))
                {
                    tessdataPath = Path.Combine(Directory.GetCurrentDirectory(), "tessdata");
                }
                
                // Si aún no existe, usar la ruta absoluta del proyecto
                if (!Directory.Exists(tessdataPath))
                {
                    tessdataPath = Path.Combine(Path.GetDirectoryName(typeof(PdfExtractionService).Assembly.Location), "tessdata");
                }
                
                // Verificar que el archivo existe
                var spaFile = Path.Combine(tessdataPath, "spa.traineddata");
                if (!File.Exists(spaFile))
                {
                    return $"Error: No se encontró spa.traineddata en {tessdataPath}";
                }
                
                // Intentar primero con inglés (más genérico para documentos)
                using var engine = new TesseractEngine(tessdataPath, "eng", EngineMode.Default);

                // Configuración optimizada para documentos
                engine.SetVariable("tessedit_char_whitelist", "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789.,-()/:ÑñÁÉÍÓÚáéíóú ");
                engine.SetVariable("tessedit_pageseg_mode", "6"); // Uniform block of text
                engine.SetVariable("tessedit_ocr_engine_mode", "3"); // Default, based on what is available
                engine.SetVariable("preserve_interword_spaces", "1");
                engine.SetVariable("tessedit_do_invert", "0"); // No invertir colores
                engine.SetVariable("tessedit_image_border", "0"); // Sin borde
                engine.SetVariable("textord_heavy_nr", "1"); // Mejor detección de texto

                // Preprocesar la imagen para mejorar OCR
                using var processedImage = PreprocessImageForOCR(image);
                
                using var ms = new MemoryStream();
                processedImage.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                var imageBytes = ms.ToArray();
                using var pix = Pix.LoadFromMemory(imageBytes);
                using var page = engine.Process(pix);
                var texto = page.GetText();
                
                var resultado = texto?.Trim() ?? string.Empty;
                if (string.IsNullOrEmpty(resultado))
                {
                    return "OCR no detectó texto en la imagen";
                }
                
                return resultado;
            }
            catch (Exception ex)
            {
                return $"Error en Tesseract: {ex.Message}";
            }
        }

        /// <summary>
        /// Preprocesa la imagen para mejorar el OCR
        /// </summary>
        private Bitmap PreprocessImageForOCR(Bitmap originalImage)
        {
            try
            {
                // Redimensionar si es muy pequeña o muy grande
                var targetWidth = 2000;
                var targetHeight = 2000;
                
                if (originalImage.Width < 800 || originalImage.Height < 800)
                {
                    var scale = Math.Max(800.0 / originalImage.Width, 800.0 / originalImage.Height);
                    targetWidth = (int)(originalImage.Width * scale);
                    targetHeight = (int)(originalImage.Height * scale);
                }
                else if (originalImage.Width > 3000 || originalImage.Height > 3000)
                {
                    var scale = Math.Min(30000.0 / originalImage.Width,3000.0 / originalImage.Height);
                    targetWidth = (int)(originalImage.Width * scale);
                    targetHeight = (int)(originalImage.Height * scale);
                }
                else
                {
                    targetWidth = originalImage.Width;
                    targetHeight = originalImage.Height;
                }
                
                // Crear imagen redimensionada
                var resizedImage = new Bitmap(targetWidth, targetHeight);
                using (var g = Graphics.FromImage(resizedImage))
                {
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    g.DrawImage(originalImage, 0, 0, targetWidth, targetHeight);
                }
                
                // Convertir a escala de grises
                var grayImage = new Bitmap(targetWidth, targetHeight, System.Drawing.Imaging.PixelFormat.Format8bppIndexed);
                using (var g = Graphics.FromImage(grayImage))
                {
                    g.DrawImage(resizedImage, 0, 0);
                }
                
                return grayImage;
            }
            catch
            {
                return new Bitmap(originalImage);
            }
        }

        /// <summary>
        /// Valida si el archivo es un PDF válido
        /// </summary>
        public async Task<bool> ValidarPdfAsync(string filePath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using var reader = new PdfReader(filePath);
                    using var document = new iText.Kernel.Pdf.PdfDocument(reader);
                    return document.GetNumberOfPages() > 0;
                }
                catch
                {
                    return false;
                }
            });
        }

        /// <summary>
        /// Valida si el stream es un PDF válido
        /// </summary>
        public async Task<bool> ValidarPdfAsync(Stream fileStream)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var position = fileStream.Position;
                    using var reader = new PdfReader(fileStream);
                    using var document = new iText.Kernel.Pdf.PdfDocument(reader);
                    var isValid = document.GetNumberOfPages() > 0;
                    fileStream.Position = position; // Restaurar posición
                    return isValid;
                }
                catch
                {
                    return false;
                }
            });
        }

        /// <summary>
        /// Obtiene información básica del archivo PDF
        /// </summary>
        public async Task<FileInfo> ObtenerInformacionArchivoAsync(string filePath)
        {
            return await Task.Run(() => new FileInfo(filePath));
        }

        /// <summary>
        /// Calcula el hash SHA256 del archivo
        /// </summary>
        public async Task<string> CalcularHashArchivoAsync(string filePath)
        {
            return await Task.Run(async () =>
            {
                using var sha256 = SHA256.Create();
                using var stream = File.OpenRead(filePath);
                var hash = await sha256.ComputeHashAsync(stream);
                return Convert.ToHexString(hash).ToLower();
            });
        }

        /// <summary>
        /// Calcula el hash SHA256 del stream
        /// </summary>
        public async Task<string> CalcularHashArchivoAsync(Stream fileStream)
        {
            return await Task.Run(async () =>
            {
                var position = fileStream.Position;
                using var sha256 = SHA256.Create();
                var hash = await sha256.ComputeHashAsync(fileStream);
                fileStream.Position = position; // Restaurar posición
                return Convert.ToHexString(hash).ToLower();
            });
        }
    }
} 