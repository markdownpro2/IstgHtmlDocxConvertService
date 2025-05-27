using System.Diagnostics;

namespace IstgHtmlDocxConvertService.Logging
{
    public class SystemEventLogger
    {
        private readonly string _source;
        private readonly string _logName;

        public SystemEventLogger(IConfiguration configuration, string logName = "Application")
        {
            _source = configuration.GetSection("Logging").GetValue<string>("Name");
            _logName = logName;

            if (!EventLog.SourceExists(_source))
            {
                EventLog.CreateEventSource(_source, _logName);
            }
        }

        private void WriteEntry(string message, EventLogEntryType type)
        {
            EventLog.WriteEntry(_source, message, type);
        }

        public void Info(string message)
        {
            WriteEntry(message, EventLogEntryType.Information);
        }

        public void Warn(string message)
        {
            WriteEntry(message, EventLogEntryType.Warning);
        }

        public void Error(string message)
        {
            WriteEntry(message, EventLogEntryType.Error);
        }
    }
}
