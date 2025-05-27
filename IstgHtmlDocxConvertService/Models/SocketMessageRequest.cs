using IstgHtmlDocxConvertService.Interfaces;

namespace IstgHtmlDocxConvertService.Models
{
    public class SocketMessageRequest : ISocketMessage
    {
        public string Origin { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string SessionId { get; set; } = string.Empty;
        public string PayloadType { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public bool? Success { get; set; } = false;
    }

    public class SocketMessageResponse : ISocketMessage
    {
        public string Origin { get; set; } = string.Empty;
        public string SessionId { get; set; } = string.Empty;
        public string PayloadType { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public bool? Success { get; set; } = false;
    }

    
    public class SocketMessageError: ISocketMessage
    {
        public string Origin { get; set; } = string.Empty;   
        public string SessionId { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public bool? Success { get; set; } = false;
        public string ErrorCode { get; set; } = string.Empty;
    }

}
