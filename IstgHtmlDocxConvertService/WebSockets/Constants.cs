namespace IstgHtmlDocxConvertService.WebSockets
{
    public class WebSocketActions
    {
        public const string UpdateOoxml = "update-ooxml";
        public const string GetHtml = "get-html";
        public const string EndSession = "end-session";
        public const string SessionClosed = "session-closed";
        public const string Init = "init";
        public const string Unkown = "Unknown";
    }

    public class WebSocketPayloadTypes
    {
        public const string Html = "html";
        public const string Docx = "docx";
    }

    public class Origins
    {
        public const string Word = "word";
        public const string Client = "client";
        public const string Server = "server";
    }
}
