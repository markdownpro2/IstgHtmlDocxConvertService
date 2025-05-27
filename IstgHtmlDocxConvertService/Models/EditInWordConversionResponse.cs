namespace IstgHtmlDocxConvertService.Models
{
    public class EditInWordConversionResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string sessionId { get; set; }
        public string? WordUrl { get; set; }
    }

    public class EditInWordConversionError
    {
        public bool Success { get; set; }
        public string ErrorCode { get; set; }
        public string Message { get; set; }
        public string sessionId { get; set; }
        public string? WordUrl { get; set; }
    }
}
