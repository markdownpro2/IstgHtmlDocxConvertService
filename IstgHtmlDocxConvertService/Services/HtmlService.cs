namespace IstgHtmlDocxConvertService.Services
{
    /// <summary>
    /// Provides services for processing and manipulating HTML content.
    /// </summary>
    public class HtmlService
    {
        public string ExtractBodyInnerHtml(string htmlContent)
        {
            if (string.IsNullOrWhiteSpace(htmlContent))
                return string.Empty;

            string lowerHtml = htmlContent.ToLowerInvariant();
            int bodyStart = lowerHtml.IndexOf("<body");

            if (bodyStart == -1)
                return htmlContent; // No <body> tag found, return full content

            int bodyOpenEnd = htmlContent.IndexOf(">", bodyStart);
            int bodyClose = lowerHtml.IndexOf("</body>", bodyOpenEnd);

            if (bodyOpenEnd == -1 || bodyClose == -1)
                return htmlContent; // Incomplete <body> tag structure

            string bodyInnerHtml = htmlContent.Substring(
                bodyOpenEnd + 1,
                bodyClose - bodyOpenEnd - 1
            );

            return bodyInnerHtml.Trim();
        }

    }
}
