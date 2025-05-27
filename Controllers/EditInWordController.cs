using IstgHtmlDocxConvertService.Exceptions;
using IstgHtmlDocxConvertService.Models;
using IstgHtmlDocxConvertService.Services;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace IstgHtmlDocxConvertService.Controllers
{
    [ApiController]
    [Route("edit-in-word")]
    public class EditInWordController : ControllerBase
    {
        private readonly ConversionService _conversionService;
        private readonly TokenValidationService _tokenValidationService;
        private readonly SessionStorageService _storage;

        public EditInWordController(ConversionService conversionService, SessionStorageService storage, TokenValidationService tokenValidationService)
        {
            _conversionService = conversionService;
            _storage = storage;
            _tokenValidationService = tokenValidationService;
        }

        /// <summary>Generates a Word-compatible link from HTML</summary>
        [HttpPost("generate-word-launch-link")]
        [SwaggerOperation(
            Summary = "Generate Word link from HTML",
            Description = "Converts the provided HTML into a .docx file and returns a link to open it in Microsoft Word. " +
                          "If sessionId is not provided, a new session will be created."
        )]
        public async Task<IActionResult> OpenInWordHtmlDocx([FromHeader(Name = "token")] string token, [FromBody] OpenInWordHtmlDocxRequest request)
        {
            // Validate token if valid we return the link otherwise we return error

            if (!_tokenValidationService.IsTokenValid(token))
                return Unauthorized();

            var (wordUrl, sessionId, error) = await _conversionService.GenerateWordLaunchLink(token, request.Html);

            if (wordUrl == null && error != null)
                return BadRequest(new EditInWordConversionError { ErrorCode = HttpErrorCodes.Codes.NewSessionIsNotAllowed, Success = false, Message = error });
            
            if (sessionId == null)
                return BadRequest(new EditInWordConversionError { ErrorCode = HttpErrorCodes.Codes.InvalidRequest, Success = false, Message = "Session ID is required." });

            return Ok(new EditInWordConversionResponse
            {
                Success = true,
                Message = "Word link generated successfully.",
                sessionId = sessionId,
                WordUrl = wordUrl
            });
        }

    }
}
