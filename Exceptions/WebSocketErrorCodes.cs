namespace IstgHtmlDocxConvertService.Exceptions
{
    public static class WebSocketErrorCodes
    {
        public const string SessionNotFound = "E1001";
        public const string MissingToken = "E1002";
        public const string InvalidToken = "E1003";
        public const string InvalidAction = "E1004";
        public const string SessionExpired = "E1005";
        public const string SessionExpiredOrRemoved = "E1006";
        public const string SocketNotAuthenticated = "E1007";
        public const string MessageProcessingError = "E2001";
        public const string ConversionError = "E2002";
        public const string SendError = "E2003";

        private static readonly Dictionary<string, (string Fa, string En)> Messages = new()
        {
            [SessionNotFound] = ("نشست یافت نشد", "Session not found"),
            [MissingToken] = ("توکن احراز هویت موجود نیست", "Missing authentication token"),
            [InvalidToken] = ("توکن نامعتبر است", "Invalid token"),
            [InvalidAction] = ("عملیات نامعتبر است", "Invalid action"),
            [SessionExpired] = ("نشست منقضی شده است", "Session expired"),
            [SessionExpiredOrRemoved] = ("نشست منقضی یا پاک شده است", "Session expired or removed"),
            [SocketNotAuthenticated] = ("سوکت احراز هویت نشده است", "Socket not authenticated"),
            [MessageProcessingError] = ("خطا در پردازش پیام", "Error processing message"),
            [ConversionError] = ("خطا در تبدیل محتوا", "Conversion error"),
            [SendError] = ("خطا در ارسال پیام", "Error sending message")
        };

        public static string GetMessage(string code, string lang = "en")
        {
            if (!Messages.TryGetValue(code, out var msg))
                return lang == "fa" ? "خطای ناشناخته" : "Unknown error";

            return lang == "fa" ? msg.Fa : msg.En;
        }
    }
}
