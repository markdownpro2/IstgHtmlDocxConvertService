namespace IstgHtmlDocxConvertService.Models
{
    public class ConversionResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string FileName { get; set; }  // For the generated DOCX or HTML file
        public string FileContent { get; set; }  // Base64-encoded content if needed
    }
}
