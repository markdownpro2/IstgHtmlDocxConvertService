using System.ComponentModel.DataAnnotations;

/// <summary>
/// Request object for uploading a DOCX file to convert to HTML.
/// </summary>
public class DocxHtmlRequest
{
    /// <summary>
    /// The DOCX file to convert.
    /// </summary>
    [Required]
    public IFormFile file { get; set; }
}
