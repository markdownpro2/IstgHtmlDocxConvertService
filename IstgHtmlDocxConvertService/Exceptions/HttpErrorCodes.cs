namespace IstgHtmlDocxConvertService.Exceptions
{
    public static class HttpErrorCodes
    {
        public static class Codes
        {
            public const string InvalidRequest = "H1001";
            public const string Unauthorized = "H1002";
            public const string Forbidden = "H1003";
            public const string NotFound = "H1004";
            public const string InternalServerError = "H2001";
            public const string ServiceUnavailable = "H2002";
            public const string InvalidModelState = "H1005";
            public const string NewSessionIsNotAllowed = "H1006";
            public const string FailedToGenerateWordLink = "H1007";

        }

        public static readonly Dictionary<string, (string Fa, string En)> Messages = new()
        {
            [Codes.InvalidRequest] = ("درخواست نامعتبر", "Invalid request"),
            [Codes.Unauthorized] = ("دسترسی غیرمجاز", "Unauthorized access"),
            [Codes.Forbidden] = ("دسترسی غیرمجاز", "Forbidden"),
            [Codes.NotFound] = ("یافت نشد", "Not found"),
            [Codes.InternalServerError] = ("خطای داخلی سرور", "Internal server error"),
            [Codes.ServiceUnavailable] = ("سرویس در دسترس نیست", "Service unavailable"),
            [Codes.InvalidModelState] = ("داده‌های ورودی نامعتبر است", "Invalid input data"),
            [Codes.NewSessionIsNotAllowed] = ("ایجاد نشست جدید مجاز نمی باشد", "Generating new session is not allowed"),
            [Codes.FailedToGenerateWordLink] = ("تولید لینک فایل وورد با خطا مواجه شد", "Failed to generate word link")
        };

        public static string GetMessage(string code, string lang = "en")
        {
            if (!Messages.ContainsKey(code))
                return lang == "fa" ? "خطای ناشناخته" : "Unknown error";

            return lang == "fa" ? Messages[code].Fa : Messages[code].En;
        }
    }
}
