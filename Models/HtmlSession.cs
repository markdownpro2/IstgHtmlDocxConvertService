using System.Net.WebSockets;

namespace IstgHtmlDocxConvertService.Models
{
    public class HtmlSession
    {
        public string UserId { get; set; }
        public string SessionId { get; set; }
        public string Html { get; set; }
        public DateTime LastUpdated { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastUserInteraction { get; set; }
        public WebSocket? ClientSocket { get; set; }
        public WebSocket? WordSocket { get; set; }
        public string? WordFilePath { get; set; }
    }
}
