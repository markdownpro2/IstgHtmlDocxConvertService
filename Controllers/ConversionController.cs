using IstgHtmlDocxConvertService.Models;
using IstgHtmlDocxConvertService.Services;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace IstgHtmlDocxConvertService.Controllers
{
    [ApiController]
    [Route("api/convert")]
    public class ConversionController : ControllerBase
    {
        private readonly ConversionService _conversionService;

        public ConversionController(ConversionService conversionService)
        {
            _conversionService = conversionService;
        }

        /// <summary>
        /// Converts HTML content to a DOCX file.
        /// </summary>
        /// <param name="request">The request containing the HTML content to be converted.</param>
        /// <returns>A DOCX file or an error message if the conversion fails.</returns>
        [HttpPost("html-docx")]
        [SwaggerOperation(
            Summary = "Converts HTML to DOCX",
            Description = "Sending HTML representation and Returns a DOCX file."
        )]

        public async Task<IActionResult> HtmlToDocx([FromBody] HtmlDocxRequest request)
        {
            if (string.IsNullOrEmpty(request.html))
            {
                return BadRequest(new ConversionResponse
                {
                    Success = false,
                    Message = "HTML content cannot be empty."
                });
            }

            var result = await _conversionService.ConvertHtmlToDocx(request.html);
            return result;
        }

        /// <summary>
        /// Converts a DOCX file to HTML.
        /// </summary>
        /// <param name="request">Form containing the DOCX file.</param>
        /// <returns>HTML content or an error message.</returns>
        [HttpPost("docx-html")]
        [Consumes("multipart/form-data")]
        [SwaggerOperation(
            Summary = "Converts DOCX to HTML",
            Description = "Uploads a DOCX file and returns its HTML representation."

        )]
        public async Task<IActionResult> DocxToHtml([FromForm] DocxHtmlRequest request)
        {
            if (request.file == null || request.file.Length == 0)
            {
                return BadRequest(new ConversionResponse
                {
                    Success = false,
                    Message = "DOCX file cannot be empty."
                });
            }

            // Read the file content into a byte array
            using (var memoryStream = new MemoryStream())
            {
                await request.file.CopyToAsync(memoryStream);
                byte[] fileContent = memoryStream.ToArray();

                // Convert DOCX to HTML using the service
                return await _conversionService.ConvertDocxToHtml(fileContent);
            }
        }

    }
}
