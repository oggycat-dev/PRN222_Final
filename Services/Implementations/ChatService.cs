using DAL.Entities;
using DAL.Repositories;
using Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace Services.Implementations
{
    public class ChatService : IChatService
    {
        private readonly IMessageRepository _messageRepository;
        private readonly IPromptSessionRepository _promptSessionRepository;
        private readonly IUserRepository _userRepository;
        private readonly IAIService _aiService;
        private readonly ILogger<ChatService> _logger;

        public ChatService(
            IMessageRepository messageRepository,
            IPromptSessionRepository promptSessionRepository,
            IUserRepository userRepository,
            IAIService aiService,
            ILogger<ChatService> logger)
        {
            _messageRepository = messageRepository;
            _promptSessionRepository = promptSessionRepository;
            _userRepository = userRepository;
            _aiService = aiService;
            _logger = logger;
        }

        public async Task<ChatResponse> SendMessageAsync(int userId, int sessionId, string messageContent)
        {
            try
            {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                // Validate session exists and belongs to user
                var session = await _promptSessionRepository.GetByIdAsync(sessionId);
                if (session == null || session.UserId != userId)
                {
                    return new ChatResponse
                    {
                        Success = false,
                        Message = "Phiên trò chuyện không tồn tại hoặc không thuộc về bạn."
                    };
                }

                // Get user information
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return new ChatResponse
                    {
                        Success = false,
                        Message = "Người dùng không tồn tại."
                    };
                }

                // Create user message
                var userMessage = new Message
                {
                    UserId = userId,
                    PromptSessionId = sessionId,
                    Content = messageContent.Trim(),
                    MessageType = "user",
                    CreatedAt = DateTime.UtcNow
                };

                var savedUserMessage = await _messageRepository.CreateAsync(userMessage);

                // Get chat history for context
                var chatHistory = await GetSessionChatHistoryAsync(sessionId, 10);

                // Generate AI response
                var aiResponse = await _aiService.SendMessageWithContextAsync(messageContent, chatHistory, userId);

                MessageDto? aiMessageDto = null;
                if (aiResponse.Success)
                {
                    // Create AI message
                    var aiMessage = new Message
                    {
                        UserId = userId,
                        PromptSessionId = sessionId,
                        Content = aiResponse.Response,
                        MessageType = "assistant",
                        CreatedAt = DateTime.UtcNow
                    };

                    var savedAiMessage = await _messageRepository.CreateAsync(aiMessage);
                    aiMessageDto = MapToDto(savedAiMessage);
                    aiMessageDto.UserName = "AI Assistant";
                }

                stopwatch.Stop();

                return new ChatResponse
                {
                    Success = true,
                    Message = aiResponse.Success ? "Tin nhắn đã được gửi thành công." : "Tin nhắn được gửi nhưng AI không phản hồi.",
                    UserMessage = MapToDto(savedUserMessage),
                    AIResponse = aiMessageDto,
                    TokensUsed = aiResponse.TokensUsed,
                    ProcessingTimeMs = stopwatch.ElapsedMilliseconds
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending message for user {userId} in session {sessionId}");
                return new ChatResponse
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi gửi tin nhắn.",
                    ErrorDetails = ex.Message
                };
            }
        }

        // NEW: Simplified streaming method that returns the user message and AI response for streaming
        public async Task<StreamingChatResponse> SendMessageStreamingAsync(int userId, int sessionId, string messageContent, string connectionId)
        {
            try
            {
                // Validate session exists and belongs to user
                var session = await _promptSessionRepository.GetByIdAsync(sessionId);
                if (session == null || session.UserId != userId)
                {
                    return new StreamingChatResponse
                    {
                        Success = false,
                        Message = "Phiên trò chuyện không tồn tại hoặc không thuộc về bạn."
                    };
                }

                // Get user information
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return new StreamingChatResponse
                    {
                        Success = false,
                        Message = "Người dùng không tồn tại."
                    };
                }

                // Create user message
                var userMessage = new Message
                {
                    UserId = userId,
                    PromptSessionId = sessionId,
                    Content = messageContent.Trim(),
                    MessageType = "user",
                    CreatedAt = DateTime.UtcNow
                };

                var savedUserMessage = await _messageRepository.CreateAsync(userMessage);
                var userMessageDto = MapToDto(savedUserMessage);
                userMessageDto.UserName = user.FullName;

                return new StreamingChatResponse
                {
                    Success = true,
                    Message = "Tin nhắn đã được lưu, đang tạo phản hồi AI...",
                    UserMessage = userMessageDto,
                    StreamingMessageId = Guid.NewGuid().ToString()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error starting streaming message for user {userId} in session {sessionId}");
                return new StreamingChatResponse
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi gửi tin nhắn.",
                    ErrorDetails = ex.Message
                };
            }
        }

        // NEW: Method for Hub to get AI response for streaming
        public async Task<AIResponse> GetAIResponseAsync(int userId, int sessionId, string messageContent)
        {
            try
            {
                // Get chat history for context
                var chatHistory = await GetSessionChatHistoryAsync(sessionId, 10);

                // Generate AI response
                return await _aiService.SendMessageWithContextAsync(messageContent, chatHistory, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting AI response for user {userId}, session {sessionId}");
                return new AIResponse
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi tạo phản hồi AI.",
                    ErrorDetails = ex.Message
                };
            }
        }

        // NEW: Method to save AI message after streaming
        public async Task<MessageDto?> SaveAIMessageAsync(int userId, int sessionId, string content)
        {
            try
            {
                var aiMessage = new Message
                {
                    UserId = userId,
                    PromptSessionId = sessionId,
                    Content = content,
                    MessageType = "assistant",
                    CreatedAt = DateTime.UtcNow
                };

                var savedAiMessage = await _messageRepository.CreateAsync(aiMessage);
                var aiMessageDto = MapToDto(savedAiMessage);
                aiMessageDto.UserName = "AI Assistant";
                
                return aiMessageDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error saving AI message for user {userId}, session {sessionId}");
                return null;
            }
        }

        public async Task<IEnumerable<MessageDto>> GetSessionMessagesAsync(int userId, int sessionId)
        {
            try
            {
                // Validate session belongs to user
                var session = await _promptSessionRepository.GetByIdAsync(sessionId);
                if (session == null || session.UserId != userId)
                {
                    return new List<MessageDto>();
                }

                var messages = await _messageRepository.GetSessionMessagesOrderedAsync(sessionId);
                var user = await _userRepository.GetByIdAsync(userId);
                
                return messages.Select(m => {
                    var dto = MapToDto(m);
                    dto.UserName = m.MessageType == "user" ? (user?.FullName ?? "Unknown User") : "AI Assistant";
                    return dto;
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting session messages for user {userId}, session {sessionId}");
                return new List<MessageDto>();
            }
        }

        public async Task<MessageDto?> GetMessageAsync(int messageId, int userId)
        {
            try
            {
                var message = await _messageRepository.GetByIdAsync(messageId);
                if (message == null || message.UserId != userId)
                {
                    return null;
                }

                return MapToDto(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting message {messageId} for user {userId}");
                return null;
            }
        }

        public async Task<bool> DeleteMessageAsync(int messageId, int userId)
        {
            try
            {
                var message = await _messageRepository.GetByIdAsync(messageId);
                if (message == null || message.UserId != userId)
                {
                    return false;
                }

                return await _messageRepository.DeleteAsync(messageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting message {messageId} for user {userId}");
                return false;
            }
        }

        public async Task<MessageDto?> EditMessageAsync(int messageId, int userId, string newContent)
        {
            try
            {
                var message = await _messageRepository.GetByIdAsync(messageId);
                if (message == null || message.UserId != userId)
                {
                    return null;
                }

                message.Content = newContent.Trim();
                message.IsEdited = true;
                message.EditedAt = DateTime.UtcNow;

                var updatedMessage = await _messageRepository.UpdateAsync(message);
                return MapToDto(updatedMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error editing message {messageId} for user {userId}");
                return null;
            }
        }

        public async Task<List<ChatMessage>> GetSessionChatHistoryAsync(int sessionId, int messageCount = 10)
        {
            try
            {
                var messages = await _messageRepository.GetRecentMessagesAsync(sessionId, messageCount);
                
                return messages.Select(m => new ChatMessage
                {
                    Role = m.MessageType,
                    Content = m.Content,
                    Timestamp = m.CreatedAt
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting chat history for session {sessionId}");
                return new List<ChatMessage>();
            }
        }

        public async Task<int> GetSessionMessageCountAsync(int sessionId)
        {
            try
            {
                return await _messageRepository.GetSessionMessageCountAsync(sessionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting message count for session {sessionId}");
                return 0;
            }
        }

        private MessageDto MapToDto(Message message)
        {
            return new MessageDto
            {
                Id = message.Id,
                Content = message.Content,
                MessageType = message.MessageType,
                CreatedAt = message.CreatedAt,
                IsEdited = message.IsEdited,
                EditedAt = message.EditedAt,
                UserId = message.UserId,
                PromptSessionId = message.PromptSessionId,
                UserName = message.MessageType == "user" ? "User" : "AI Assistant"
            };
        }

        // Admin message management methods
        public async Task<AdminMessageStatsResponse> GetMessageStatsAsync()
        {
            try
            {
                var totalMessages = await _messageRepository.GetTotalMessagesCountAsync();
                var todayMessages = await _messageRepository.GetMessagesCreatedTodayAsync();
                var userMessages = await _messageRepository.GetUserMessagesCountAsync();
                var aiMessages = await _messageRepository.GetAIMessagesCountAsync();

                return new AdminMessageStatsResponse
                {
                    Success = true,
                    Message = "Lấy thống kê tin nhắn thành công",
                    TotalMessages = totalMessages,
                    TodayMessages = todayMessages,
                    UserMessages = userMessages,
                    AIMessages = aiMessages
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting message stats for admin");
                return new AdminMessageStatsResponse
                {
                    Success = false,
                    Message = $"Lỗi khi lấy thống kê tin nhắn: {ex.Message}",
                    TotalMessages = 0,
                    TodayMessages = 0,
                    UserMessages = 0,
                    AIMessages = 0
                };
            }
        }

        public async Task<IEnumerable<AdminMessageDto>> GetSessionMessagesForAdminAsync(int sessionId)
        {
            try
            {
                var messages = await _messageRepository.GetSessionMessagesOrderedAsync(sessionId);
                
                return messages.Select(m => new AdminMessageDto
                {
                    Id = m.Id,
                    Content = m.Content,
                    MessageType = m.MessageType,
                    CreatedAt = m.CreatedAt,
                    IsEdited = m.IsEdited,
                    EditedAt = m.EditedAt,
                    UserId = m.UserId,
                    Username = m.User?.Username ?? "Unknown",
                    UserFullName = m.User?.FullName ?? "Unknown"
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting session messages for admin, session {sessionId}");
                return new List<AdminMessageDto>();
            }
        }
    }
} 