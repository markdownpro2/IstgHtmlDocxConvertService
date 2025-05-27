using IstgHtmlDocxConvertService.WebSockets;

namespace IstgHtmlDocxConvertService.Services
{
    public class SessionCleanupService : BackgroundService
    {
        private readonly IServiceProvider _servicProvider;
        private readonly TimeSpan _cleanupInterval;

        public SessionCleanupService(IServiceProvider serviceProvider, IConfiguration configuration)
        {
            _servicProvider = serviceProvider;
            var intervalMinutes = configuration.GetValue<int>("SessionCleanupIntervalMinutes", 20);
            _cleanupInterval = TimeSpan.FromMinutes(intervalMinutes);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _servicProvider.CreateScope())
                {
                    var sessionService = scope.ServiceProvider.GetRequiredService<SessionStorageService>();
                    var webSocketHandler = scope.ServiceProvider.GetRequiredService<WebSocketHandler>();
                    // Cleanup expired sessions and get the sockets to close
                    var socketsToClose = sessionService.CleanupExpiredSessionsAndReturnSocketsToClose();
                    // Send final message and Close the sockets
                    socketsToClose.ForEach(async (socket) => await webSocketHandler.CloseSocketAsync(socket, "Cleaning expired sessions"));

                }
                await Task.Delay(_cleanupInterval, stoppingToken);
            }
        }
    }
}
