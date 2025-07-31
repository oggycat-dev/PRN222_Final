using Microsoft.AspNetCore.SignalR;
using Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace FinalProject.Hubs
{
    public class ChatHub : Hub
    {
        private readonly IChatService _chatService;
        private readonly ILogger<ChatHub> _logger;

        public ChatHub(IChatService chatService, ILogger<ChatHub> logger)
        {
            _chatService = chatService;
            _logger = logger;
        }

        // Method called when client sends a message
        public async Task SendMessage(int sessionId, string message, int userId)
        {
            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(message))
                {
                    await Clients.Caller.SendAsync("Error", "Tin nhắn không được để trống.");
                    return;
                }

                if (sessionId <= 0 || userId <= 0)
                {
                    await Clients.Caller.SendAsync("Error", "Thông tin phiên hoặc người dùng không hợp lệ.");
                    return;
                }

                _logger.LogInformation($"User {userId} sending message to session {sessionId} via SignalR");

                // Save user message and get initial response from ChatService
                var response = await _chatService.SendMessageStreamingAsync(userId, sessionId, message, Context.ConnectionId);

                if (!response.Success)
                {
                    await Clients.Caller.SendAsync("Error", response.Message);
                    return;
                }

                // Send user message immediately to client
                if (response.UserMessage != null)
                {
                    await Clients.Caller.SendAsync("MessageReceived", response.UserMessage);
                }

                // Start streaming AI response
                var streamingMessageId = response.StreamingMessageId ?? Guid.NewGuid().ToString();
                
                // Send AI typing indicator and start streaming
                await Clients.Caller.SendAsync("AITypingStart");
                await Clients.Caller.SendAsync("AIResponseStart", streamingMessageId);

                // Do streaming synchronously to avoid disposal issues
                await StreamAIResponseAsync(userId, sessionId, message, streamingMessageId);

                _logger.LogInformation($"Completed streaming response for user {userId}, session {sessionId}, streamingId: {streamingMessageId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in SendMessage for user {userId}, session {sessionId}");
                await Clients.Caller.SendAsync("Error", "Đã xảy ra lỗi khi gửi tin nhắn.");
                await Clients.Caller.SendAsync("AITypingStop");
            }
        }

        // Synchronous method to handle AI streaming - no background task needed
        private async Task StreamAIResponseAsync(int userId, int sessionId, string messageContent, string streamingMessageId)
        {
            try
            {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                // Get AI response from ChatService
                var aiResponse = await _chatService.GetAIResponseAsync(userId, sessionId, messageContent);

                if (aiResponse.Success && !string.IsNullOrEmpty(aiResponse.Response))
                {
                    var fullResponse = aiResponse.Response;
                    
                    // Simulate streaming by sending chunks
                    var words = fullResponse.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    var chunkSize = 3; // Send 3 words at a time
                    
                    for (int i = 0; i < words.Length; i += chunkSize)
                    {
                        var chunk = string.Join(' ', words.Skip(i).Take(chunkSize));
                        if (i + chunkSize < words.Length) chunk += " "; // Add space if not last chunk
                        
                        // Send chunk via SignalR
                        await Clients.Caller.SendAsync("AIResponseChunk", new { 
                            messageId = streamingMessageId, 
                            chunk = chunk 
                        });
                        
                        // Small delay to simulate streaming
                        await Task.Delay(100);
                    }

                    // Save the complete AI message to database via ChatService
                    var savedAiMessage = await _chatService.SaveAIMessageAsync(userId, sessionId, fullResponse);

                    stopwatch.Stop();

                    // Send completion notification
                    if (savedAiMessage != null)
                    {
                        await Clients.Caller.SendAsync("AIResponseComplete", new
                        {
                            message = savedAiMessage,
                            tokensUsed = aiResponse.TokensUsed,
                            processingTime = stopwatch.ElapsedMilliseconds
                        });
                    }
                    else
                    {
                        await Clients.Caller.SendAsync("AIResponseError", "Không thể lưu phản hồi AI.");
                    }

                    _logger.LogInformation($"✅ Streaming response completed for user {userId}, session {sessionId}. Response: {fullResponse.Length} chars, Time: {stopwatch.ElapsedMilliseconds}ms");
                }
                else
                {
                    // Send error response
                    await Clients.Caller.SendAsync("AIResponseError", 
                        aiResponse.ErrorDetails ?? "Không thể tạo phản hồi AI.");
                }

                // Stop typing indicator
                await Clients.Caller.SendAsync("AITypingStop");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in streaming AI response for user {userId}, session {sessionId}");
                
                try
                {
                    // Send error notification
                    await Clients.Caller.SendAsync("AIResponseError", 
                        $"Đã xảy ra lỗi: {ex.Message}");
                    
                    // Stop typing indicator
                    await Clients.Caller.SendAsync("AITypingStop");
                }
                catch (Exception notifyEx)
                {
                    _logger.LogError(notifyEx, $"Failed to send error notification to client");
                }
            }
        }

        // Session management methods
        public async Task JoinSession(int sessionId, string userId)
        {
            try
            {
                var groupName = GetSessionGroupName(sessionId);
                await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
                
                _logger.LogInformation($"User {userId} joined session {sessionId} group with connection {Context.ConnectionId}");
                
                // Notify others in the session (for future multi-user support)
                await Clients.Group(groupName).SendAsync("UserJoined", userId, Context.ConnectionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error joining session {sessionId} for user {userId}");
                await Clients.Caller.SendAsync("Error", "Không thể tham gia phiên trò chuyện.");
            }
        }

        public async Task LeaveSession(int sessionId, string userId)
        {
            try
            {
                var groupName = GetSessionGroupName(sessionId);
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
                
                _logger.LogInformation($"User {userId} left session {sessionId} group");
                
                // Notify others in the session
                await Clients.Group(groupName).SendAsync("UserLeft", userId, Context.ConnectionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error leaving session {sessionId} for user {userId}");
            }
        }

        // Typing indicators
        public async Task StartTyping(int sessionId, string userId)
        {
            try
            {
                var groupName = GetSessionGroupName(sessionId);
                await Clients.GroupExcept(groupName, Context.ConnectionId).SendAsync("UserTypingStart", userId);
                
                _logger.LogDebug($"User {userId} started typing in session {sessionId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in StartTyping for user {userId}, session {sessionId}");
            }
        }

        public async Task StopTyping(int sessionId, string userId)
        {
            try
            {
                var groupName = GetSessionGroupName(sessionId);
                await Clients.GroupExcept(groupName, Context.ConnectionId).SendAsync("UserTypingStop", userId);
                
                _logger.LogDebug($"User {userId} stopped typing in session {sessionId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in StopTyping for user {userId}, session {sessionId}");
            }
        }

        // Connection management
        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation($"Client connected: {Context.ConnectionId}");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (exception != null)
            {
                _logger.LogError(exception, $"Client disconnected with error: {Context.ConnectionId}");
            }
            else
            {
                _logger.LogInformation($"Client disconnected: {Context.ConnectionId}");
            }
            
            await base.OnDisconnectedAsync(exception);
        }

        // Helper method to get consistent group names for sessions
        private static string GetSessionGroupName(int sessionId)
        {
            return $"Session_{sessionId}";
        }

        // Heartbeat method to check connection health
        public async Task Ping()
        {
            try
            {
                await Clients.Caller.SendAsync("Pong", DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in Ping for connection {Context.ConnectionId}");
            }
        }

        // Method to get connection info (for debugging)
        public async Task GetConnectionInfo()
        {
            try
            {
                var connectionInfo = new
                {
                    ConnectionId = Context.ConnectionId,
                    ConnectedAt = DateTime.UtcNow,
                    UserAgent = Context.GetHttpContext()?.Request.Headers["User-Agent"].ToString(),
                    RemoteIpAddress = Context.GetHttpContext()?.Connection.RemoteIpAddress?.ToString()
                };

                await Clients.Caller.SendAsync("ConnectionInfo", connectionInfo);
                _logger.LogDebug($"Connection info requested: {Context.ConnectionId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting connection info for {Context.ConnectionId}");
            }
        }
    }
} 