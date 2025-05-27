using Aspose.Words;
using Aspose.Words.Loading;
using Aspose.Words.Saving;
using IstgHtmlDocxConvertService.Logging;
using IstgHtmlDocxConvertService.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace IstgHtmlDocxConvertService.Services
{
    /// <summary>
    /// Service responsible for converting HTML content to DOCX format and vice versa.
    /// </summary>
    public class ConversionService
    {
        private HtmlService _htmlService;
        private IConfiguration _configuration;
        private string _tempFilesFolderPath;
        private string _publicHostingFolderUrl;
        private SessionStorageService _storage;
        private readonly TokenValidationService _tokenValidationService;
        private readonly SystemEventLogger _eventLogger;

        public ConversionService(HtmlService htmlService, SessionStorageService storageService, TokenValidationService tokenValidationService, SystemEventLogger eventLogger, IConfiguration configuration)
        {
            _htmlService = htmlService;
            _configuration = configuration;
            _storage = storageService;
            _tokenValidationService = tokenValidationService;
            _eventLogger = eventLogger;
            _tempFilesFolderPath = _configuration.GetSection("TempFilesFolderPath").Get<string>();
            _publicHostingFolderUrl = _configuration.GetSection("PublicHostingFolderUrl").Get<string>();

            // Register encoding provider (to handle special encodings)
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        /// <summary>
        /// Converts the provided HTML content into a DOCX file.
        /// </summary>
        public async Task<IActionResult> ConvertHtmlToDocx(string htmlContent)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(htmlContent))
                    return BadRequest("Invalid HTML content.");

                // Load HTML into Aspose Document
                using (var htmlStream = new MemoryStream(Encoding.UTF8.GetBytes(htmlContent)))
                {
                    var loadOptions = new LoadOptions { LoadFormat = LoadFormat.Html, Encoding = Encoding.UTF8 };
                    var document = new Document(htmlStream, loadOptions);

                    // Save Document to DOCX in memory
                    using (var outputStream = new MemoryStream())
                    {
                        await Task.Run(() => document.Save(outputStream, SaveFormat.Docx));
                        outputStream.Position = 0;

                        _eventLogger.Info("HTML successfully converted to DOCX.");
                        // Return the DOCX file as a downloadable file
                        return new FileContentResult(outputStream.ToArray(), "application/vnd.openxmlformats-officedocument.wordprocessingml.document")
                        {
                            FileDownloadName = "Document.docx"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                _eventLogger.Error($"Error converting HTML to DOCX. {ex.Message}");
                return BadRequest($"An error occurred while converting HTML to DOCX. {ex.Message}");
            }
        }

        /// <summary>
        /// Converts the provided DOCX file into HTML content.
        /// </summary>
        public async Task<IActionResult> ConvertDocxToHtml(byte[] docxFile)
        {
            try
            {
                if (docxFile == null || docxFile.Length == 0)
                {
                    _eventLogger.Warn("No DOCX file provided.");
                    return BadRequest("No DOCX file provided.");
                }

                using (var docxStream = new MemoryStream(docxFile))
                {
                    var document = new Document(docxStream);

                    // Create HtmlSaveOptions to embed images as Base64
                    HtmlSaveOptions options = new HtmlSaveOptions
                    {
                        ExportImagesAsBase64 = true,
                        ExportHeadersFootersMode = ExportHeadersFootersMode.PerSection,
                        PrettyFormat = true
                    };

                    using (var outputStream = new MemoryStream())
                    {
                        await Task.Run(() => document.Save(outputStream, options));

                        outputStream.Position = 0;

                        // Convert the output stream to string (HTML content)
                        using (var reader = new StreamReader(outputStream, Encoding.UTF8))
                        {
                            var htmlContent = await reader.ReadToEndAsync();

                            htmlContent = _htmlService.ExtractBodyInnerHtml(htmlContent);

                            // Respond with the converted HTML content


                            _eventLogger.Info("DOCX successfully converted to HTML.");
                            return new OkObjectResult(new DocxHtmlResponse
                            {
                                html = htmlContent,
                                document_language = "fa-IR"
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _eventLogger.Error($"Error converting DOCX to HTML. {ex.Message}");
                return BadRequest($"An error occurred while converting DOCX to HTML. {ex.Message}");
            }
        }

        /// <summary>
        /// Converts HTML to DOCX, embeds sessionId, stores the file temporarily,
        /// and returns the ms-word:// link for opening.
        /// </summary>
        public async Task<(string? WordLink, string? sessionId, string? ErrorMessage)> GenerateWordLaunchLink(string token, string htmlContent)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(htmlContent))
                {
                    _eventLogger.Warn("HTML content was empty in GenerateWordLaunchLink.");
                    return (null, null, "HTML content is required.");
                }

                var userId = _tokenValidationService.DecryptToken(token);
                var sessionId = _storage.CreateSession(userId, htmlContent);

                // Load HTML into Aspose Document
                using var htmlStream = new MemoryStream(Encoding.UTF8.GetBytes(htmlContent));
                var loadOptions = new LoadOptions { LoadFormat = LoadFormat.Html, Encoding = Encoding.UTF8 };
                var document = new Document(htmlStream, loadOptions);

                // Embed sessionId and token as custom properties
                document.CustomDocumentProperties.Add("sessionId", sessionId);
                document.CustomDocumentProperties.Add("token", token);

                // Save Document to temporary location
                var fileId = Guid.NewGuid().ToString("N");

                Directory.CreateDirectory(_tempFilesFolderPath);

                var filePath = Path.Combine(_tempFilesFolderPath, $"{fileId}.docx");

                await Task.Run(() => document.Save(filePath, SaveFormat.Docx));

                _storage.SetWordFilePath(sessionId, filePath);
                // Construct ms-word launch URL
                var publicUrl = $"{_publicHostingFolderUrl}/{fileId}.docx"; // Replace with real URL logic
                var wordUrl = $"ms-word:ofe|u|{publicUrl}";

                return (wordUrl, sessionId, null);
            }
            catch (Exception ex)
            {
                _eventLogger.Error($"Error generating Word file: {ex.Message}");
                return (null, null, $"Error generating Word file: {ex.Message}");
            }
        }

        public async Task<(string? Html, string? ErrorMessage)> ConvertOoxmlToHtml(string ooxmlContent)
        {
            try
            {
                var byteArray = Encoding.UTF8.GetBytes(ooxmlContent);
                using var stream = new MemoryStream(byteArray);
                var loadOptions = new LoadOptions
                {
                    LoadFormat = LoadFormat.FlatOpc,
                    Encoding = Encoding.UTF8
                };
                var document = new Document(stream, loadOptions);

                var htmlSaveOptions = new HtmlSaveOptions
                {
                    MemoryOptimization = true,
                    ImageResolution = 300,
                    ScaleImageToShapeSize = false,
                    UseAntiAliasing = true,
                    ExportImagesAsBase64 = true,
                    PrettyFormat = true,
                    ExportHeadersFootersMode = ExportHeadersFootersMode.PerSection,
                };

                using var outputStream = new MemoryStream();
                await Task.Run(() => document.Save(outputStream, htmlSaveOptions));
                outputStream.Position = 0;

                using var reader = new StreamReader(outputStream, Encoding.UTF8);
                var html = await reader.ReadToEndAsync();
                html = _htmlService.ExtractBodyInnerHtml(html);

                return (html, null);
            }
            catch (Exception ex)
            {
                _eventLogger.Error($"Error converting OOXML to HTML: {ex.Message}");
                return (null, ex.Message);
            }
        }

        private IActionResult BadRequest(string message)
        {
            _eventLogger.Error($"BadRequest: {message}");
            return new BadRequestObjectResult(new ConversionResponse { Success = false, Message = message });
        }
    }
}
