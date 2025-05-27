using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using IstgHtmlDocxConvertService.Exceptions;
using IstgHtmlDocxConvertService.Interfaces;
using IstgHtmlDocxConvertService.Logging;
using IstgHtmlDocxConvertService.Models;
using IstgHtmlDocxConvertService.Services;

namespace IstgHtmlDocxConvertService.WebSockets
{
    public class WebSocketHandler
    {
        private readonly SessionStorageService _storage;
        private readonly ConversionService _conversionService;
        private readonly TokenValidationService _tokenValidationService;
        private readonly SystemEventLogger _eventLogger;
        private readonly Dictionary<string, Func<WebSocket, SocketMessageRequest, Task>> _handlers;

        public WebSocketHandler(SessionStorageService storage, ConversionService conversionService, TokenValidationService tokenValidationService, SystemEventLogger eventLogger)
        {
            _storage = storage;
            _conversionService = conversionService;
            _tokenValidationService = tokenValidationService;
            _eventLogger = eventLogger;

            _handlers = new Dictionary<string, Func<WebSocket, SocketMessageRequest, Task>>(StringComparer.OrdinalIgnoreCase)
            {
                [WebSocketActions.UpdateOoxml] = HandleUpdate,
                [WebSocketActions.GetHtml] = HandleGet,
                [WebSocketActions.EndSession] = HandleEnd
            };
        }

        private async Task<SocketMessageRequest?> ReceiveMessageAsync(WebSocket socket)
        {
            var buffer = new byte[1024 * 4];
            using var ms = new MemoryStream();
            WebSocketReceiveResult result;

            do
            {
                result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close)
                    return null;

                ms.Write(buffer, 0, result.Count);
            } while (!result.EndOfMessage);

            var json = Encoding.UTF8.GetString(ms.ToArray());
            return JsonSerializer.Deserialize<SocketMessageRequest>(json);
        }

        public async Task HandleAsync(WebSocket socket)
        {
            SocketMessageRequest? message = null;
            try
            {
                message = await ReceiveMessageAsync(socket);
                if (message == null)
                    return;

                var session = _storage.Get(message.SessionId);
                if (session == null)
                {
                    _eventLogger.Warn($"Session not found during initialization: {message.SessionId}");
                    await SendError(socket, WebSocketErrorCodes.SessionNotFound, WebSocketActions.Init);
                    await CloseSocketAsync(socket, "Session not found during websocket initial message");
                    return;
                }

                // ✅ Check token
                if (string.IsNullOrWhiteSpace(message.Token))
                {
                    await SendError(socket, WebSocketErrorCodes.MissingToken, WebSocketActions.Init);
                    await CloseSocketAsync(socket, "Missing authentication token in the message.");
                    return;
                }

                if (!_tokenValidationService.IsTokenValid(message.Token))
                {
                    _eventLogger.Error($"Invalid token for SessionId: {message.SessionId}");
                    await SendError(socket, WebSocketErrorCodes.InvalidToken, WebSocketActions.Init);
                    await CloseSocketAsync(socket);
                    return;
                }

                // ✅ Register sockets based on the origin
                _storage.TryRegisterSocket(message.SessionId, socket, message.Origin);
                _eventLogger.Info($"WebSocket connected and authenticated. SessionId: {message.SessionId}");

                while (socket.State == WebSocketState.Open)
                {
                    try
                    {
                        message = await ReceiveMessageAsync(socket);
                        if (message == null)
                            continue;

                        if (!_storage.Exists(message.SessionId))
                        {
                            _eventLogger.Warn($"Session expired or removed: {message.SessionId}");
                            await SendError(socket, WebSocketErrorCodes.SessionExpiredOrRemoved, message.Action);
                            await CloseSocketAsync(socket, "Session expired or removed");
                            return;
                        }

                        // 🔐 Only allow authenticated sockets to proceed
                        if (!_storage.IsSocketAuthenticated(message.SessionId, socket))
                        {
                            _eventLogger.Warn($"Unauthenticated socket attempted action: {message.Action}");
                            await SendError(socket, WebSocketErrorCodes.SocketNotAuthenticated, message.Action);
                            await CloseSocketAsync(socket);
                            return;
                        }


                        if (_handlers.TryGetValue(message.Action, out var handler))
                        {
                            await handler(socket, message);
                        }
                        else
                        {
                            _eventLogger.Warn($"Invalid WebSocket action received: {message.Action} (SessionId: {message.SessionId})");
                            await SendError(socket, WebSocketErrorCodes.InvalidAction, message.Action);
                        }
                    }
                    catch (Exception ex)
                    {
                        _eventLogger.Error($"Exception during WebSocket processing (SessionId: {message?.SessionId}): {ex.Message}");
                        await SendError(socket, WebSocketErrorCodes.MessageProcessingError, WebSocketActions.Unkown, ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                _eventLogger.Error($"Exception during WebSocket processing (SessionId: {message?.SessionId}): {ex.Message}");
                await SendError(socket, WebSocketErrorCodes.MessageProcessingError, WebSocketActions.Unkown, ex.Message);
            }
            finally
            {
                if (message != null)
                {
                    _storage.UnregisterSocket(message.SessionId, socket);
                    _eventLogger.Info($"WebSocket unregistered. SessionId: {message.SessionId}");
                }

                // Also attempt to close gracefully if it's still open
                await CloseSocketAsync(socket);
            }

        }

        private async Task HandleUpdate(WebSocket socket, SocketMessageRequest message)
        {
            var sessionId = message.SessionId;
            var ooxml = message.Content;

            if (!_storage.Exists(sessionId))
            {
                _eventLogger.Warn($"Session not found for SessionId: {sessionId} (Action: {WebSocketActions.UpdateOoxml})");
                await SendError(socket, WebSocketErrorCodes.SessionNotFound, WebSocketActions.UpdateOoxml);
                return;
            }

            var (html, error) = await _conversionService.ConvertOoxmlToHtml(ooxml);
            if (!string.IsNullOrEmpty(error))
            {
                _eventLogger.Error($"Conversion error for SessionId: {sessionId}: {error}");
                await SendError(socket, WebSocketErrorCodes.ConversionError, WebSocketActions.UpdateOoxml);
                return;
            }

            _storage.SaveOrUpdate(sessionId, html);

            _eventLogger.Info($"OOXML updated successfully for SessionId: {sessionId}. Broadcasting HTML.");

            var response = new SocketMessageResponse
            {
                Origin = Origins.Server,
                SessionId = sessionId,
                PayloadType = WebSocketPayloadTypes.Html,
                Action = WebSocketActions.GetHtml,
                Content = html,
                Success = true
            };

            foreach (var ws in _storage.GetActiveSockets(sessionId))
            {
                await SendMessage(ws, response);
            }
        }

        private async Task HandleGet(WebSocket socket, SocketMessageRequest message)
        {
            var sessionId = message.SessionId;
            var session = _storage.Get(sessionId);
            if (session == null)
            {
                _eventLogger.Warn($"Session not found for SessionId: {sessionId} (Action: {WebSocketActions.GetHtml})");
                await SendError(socket, WebSocketErrorCodes.SessionNotFound, WebSocketActions.GetHtml);
                return;
            }

            _eventLogger.Info($"HTML retrieved for SessionId: {sessionId}");

            var response = new SocketMessageResponse
            {
                Origin = Origins.Server,
                SessionId = sessionId,
                PayloadType = WebSocketPayloadTypes.Html,
                Action = WebSocketActions.GetHtml,
                Content = session.Html,
                Success = true
            };

            await SendMessage(socket, response);
        }

        private async Task HandleEnd(WebSocket socket, SocketMessageRequest message)
        {
            var sessionId = message.SessionId;

            var response = new SocketMessageResponse
            {
                Origin = Origins.Server,
                SessionId = sessionId,
                PayloadType = WebSocketPayloadTypes.Html,
                Action = WebSocketActions.EndSession,
                Content = "Session ended successfully.",
                Success = true
            };

            foreach (var ws in _storage.GetActiveSockets(sessionId))
            {
                await SendMessage(ws, response);
            }

            _storage.RemoveSession(sessionId);
            _eventLogger.Info($"Session ended and cleaned up. SessionId: {sessionId}");
        }

        private async Task SendMessage(WebSocket socket, ISocketMessage message)
        {
            try
            {
                var json = JsonSerializer.Serialize(message);
                var bytes = Encoding.UTF8.GetBytes(json);
                await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _eventLogger.Warn($"Failed to send message to WebSocket: {ex.Message}");
            }
        }


        private async Task SendError(WebSocket socket, string errorCode, string action, string detail = null)
        {
            var errorMessage = new SocketMessageError
            {
                Origin = Origins.Server,
                Action = action,
                Success = false,
                ErrorCode = errorCode,
                Content = detail == null ? WebSocketErrorCodes.GetMessage(errorCode) : $"{WebSocketErrorCodes.GetMessage(errorCode)} {detail}"
            };

            await SendMessage(socket, errorMessage);
        }


        public async Task CloseSocketAsync(WebSocket socket, string reason = null)
        {
            if (socket.State == WebSocketState.Open)
            {
                var message = new SocketMessageResponse
                {
                    Origin = Origins.Server,
                    Action = WebSocketActions.SessionClosed,
                    Content = reason ?? $"Session closed",
                    Success = true
                };
                // first we send the message to the client then we close the websocket connection
                await SendMessage(socket, message);
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Session closed", CancellationToken.None);

                _eventLogger.Info($"WebSocket closed: {reason ?? "No reason provided"}");

            }
        }

    }
}
