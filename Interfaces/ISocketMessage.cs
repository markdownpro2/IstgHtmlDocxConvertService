namespace IstgHtmlDocxConvertService.Interfaces
{
    public interface ISocketMessage
    {
        public string Origin { get; }
        public string SessionId { get; }
        public string Action { get; }
        public string Content { get; }
        public bool? Success { get; }
    }
}
