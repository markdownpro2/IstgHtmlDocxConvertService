using IstgHtmlDocxConvertService.Logging;
using IstgHtmlDocxConvertService.Models;
using IstgHtmlDocxConvertService.WebSockets;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.WebSockets;

namespace IstgHtmlDocxConvertService.Services
{
    public class SessionStorageService
    {
        private readonly ConcurrentDictionary<string, HtmlSession> _storage = new();
        private readonly TimeSpan _sessionLIfetime;
        private readonly TimeSpan _maxSessionLifetime;
        private readonly int _maxSessions;
        private readonly SystemEventLogger _eventLogger;

        public SessionStorageService(IConfiguration configuration, SystemEventLogger eventLogger)
        {
            _eventLogger = eventLogger;
            // Set session lifetime from configuration or default to 30 minutes
            _sessionLIfetime = TimeSpan.FromMinutes(configuration.GetValue<int>("SessionTTLMinutes", 30));
            _maxSessionLifetime = TimeSpan.FromMinutes(configuration.GetValue<int>("MaxSessionLifetimeMinutes", 120));
            _maxSessions = configuration.GetValue<int>("MaxSessions", 2);

        }
        public string CreateSession(string userId, string? initialHtml = null)
        {
            // We won't let user create more than he must
            if (GetSessionsByUserId(userId).Count() >= _maxSessions)
                throw new Exception($"The maximum number of sessions are {_maxSessions}");

            var sessionId = Guid.NewGuid().ToString("N");
            _storage.TryAdd(sessionId, new HtmlSession
            {
                UserId = userId,
                SessionId = sessionId,
                Html = initialHtml ?? "",
                CreatedAt = DateTime.Now,
                LastUpdated = DateTime.Now,
                LastUserInteraction = DateTime.Now,
            });

            return sessionId;
        }

        public HtmlSession? Get(string sessionId)
        {
            if (_storage.TryGetValue(sessionId, out var session))
            {
                if ((DateTime.Now - session.LastUserInteraction) > _sessionLIfetime)
                {
                    _storage.TryRemove(sessionId, out _);
                    return null;
                }
                return session;
            }
            return null;
        }


        public void SaveOrUpdate(string sessionId, string html)
        {
            // We make sure it can only update the existing sessions
            if (!Exists(sessionId))
                return;

            var htmlSession = new HtmlSession
            {
                SessionId = sessionId,
                Html = html,
                CreatedAt = DateTime.Now,
                LastUserInteraction = DateTime.Now,
                LastUpdated = DateTime.Now
            };
            _storage.AddOrUpdate(sessionId, htmlSession, (key, existing) =>
            {
                if (existing.Html != html)
                {
                    existing.Html = html;
                    existing.LastUserInteraction = DateTime.Now;
                }

                existing.LastUpdated = DateTime.Now;
                return existing;
            });
        }

        public bool RemoveSession(string sessionId)
        {
            if (_storage.TryRemove(sessionId, out var session))
            {
                if (!string.IsNullOrEmpty(session.WordFilePath) && File.Exists(session.WordFilePath))
                {
                    try
                    {
                        File.Delete(session.WordFilePath);
                    }
                    catch (Exception ex)
                    {
                        // Optional: log or handle cleanup failure
                        _eventLogger.Error($"Failed to delete file {session.WordFilePath}: {ex.Message}");
                    }
                }

                return true;
            }

            return false;
        }



        public bool Exists(string sessionId) => _storage.ContainsKey(sessionId);


        public bool TryRegisterSocket(string sessionId, WebSocket socket, string origin)
        {
            if (!_storage.TryGetValue(sessionId, out var session))
            {
                _eventLogger.Error($"Session '{sessionId}' not found for socket registration.");
                throw new InvalidOperationException($"Session '{sessionId}' does not exist.");
            }

            switch (origin)
            {
                case Origins.Client:
                    if (session.ClientSocket != null)
                        throw new InvalidOperationException("Client socket is already registered.");
                    session.ClientSocket = socket;
                    break;

                case Origins.Word:
                    if (session.WordSocket != null)
                        throw new InvalidOperationException("Word socket is already registered.");
                    session.WordSocket = socket;
                    break;

                default:
                    _eventLogger.Warn($"Invalid socket origin '{origin}' during registration.");
                    return false;
            }

            _eventLogger.Info($"Socket registered. SessionId: {sessionId}, Origin: {origin}");
            return true;
        }

        public void UnregisterSocket(string sessionId, WebSocket socket)
        {
            if (!_storage.TryGetValue(sessionId, out var session))
            {
                _eventLogger.Warn($"Attempted to unregister socket from non-existent session '{sessionId}'.");
                return;
            }

            if (session.ClientSocket == socket)
            {
                session.ClientSocket = null;
                _eventLogger.Info($"Client socket unregistered. SessionId: {sessionId}");
            }
            else if (session.WordSocket == socket)
            {
                session.WordSocket = null;
                _eventLogger.Info($"Word socket unregistered. SessionId: {sessionId}");
            }
            else
            {
                _eventLogger.Warn($"Socket not found in session '{sessionId}' during unregistration.");
            }
        }

        public void SetWordFilePath(string sessionId, string wordFilePath)
        {
            if (_storage.TryGetValue(sessionId, out var session))
            {
                session.WordFilePath = wordFilePath;
            }
        }

        // This is the method we use to remove expired sessions
        public List<WebSocket> CleanupExpiredSessionsAndReturnSocketsToClose()
        {
            var now = DateTime.Now;
            var socketsToClose = new List<WebSocket>();

            foreach (var sessionEntry in _storage)
            {
                var session = sessionEntry.Value;

                // Prioritize inactivity: expire session if user has been inactive too long
                bool isInactive = (now - session.LastUserInteraction) > _sessionLIfetime;

                // Absolute lifetime cap: expire even if user is active
                bool isTooOld = (now - session.CreatedAt) > _maxSessionLifetime;

                if (isInactive || isTooOld)
                {
                    if (session.ClientSocket != null && session.ClientSocket.State == WebSocketState.Open)
                        socketsToClose.Add(session.ClientSocket);
                    if (session.WordSocket != null && session.WordSocket.State == WebSocketState.Open)
                        socketsToClose.Add(session.WordSocket);

                    RemoveSession(sessionEntry.Key);
                }
            }

            return socketsToClose;
        }

        // We use this method for throttling
        public IEnumerable<HtmlSession> GetSessionsByUserId(string userId)
        {
            return _storage.Values.Where(s => s.UserId == userId);
        }

        // This will check if the socket is connected or not
        public bool IsSocketAuthenticated(string sessionId, WebSocket socket)
        {
            _storage.TryGetValue(sessionId, out var session);
            if (session == null)
                throw new Exception($"Is SocketAuthenticated: Session '{sessionId}' does not exist.");
            return session.ClientSocket == socket || session.WordSocket == socket;
        }

        public List<WebSocket> GetActiveSockets(string sessionId)
        {
            _storage.TryGetValue(sessionId, out var session);
            if (session == null)
                throw new Exception($"GetSockets: Session '{sessionId}' does not exist.");

            var activeSockets = new List<WebSocket>();
            if (session.ClientSocket != null && session.ClientSocket.State == WebSocketState.Open)
                activeSockets.Add(session.ClientSocket);

            if (session.WordSocket != null && session.WordSocket.State == WebSocketState.Open)
                activeSockets.Add(session.WordSocket);

            return activeSockets;
        }
    }
}
