using DAL.Entities;

namespace Services.Interfaces
{
    public interface IChatService
    {
        // Chat operations
        Task<ChatResponse> SendMessageAsync(int userId, int sessionId, string messageContent);
        Task<IEnumerable<MessageDto>> GetSessionMessagesAsync(int userId, int sessionId);
        Task<MessageDto?> GetMessageAsync(int messageId, int userId);
        
        // NEW: Streaming chat operations
        Task<StreamingChatResponse> SendMessageStreamingAsync(int userId, int sessionId, string messageContent, string connectionId);
        Task<AIResponse> GetAIResponseAsync(int userId, int sessionId, string messageContent);
        Task<MessageDto?> SaveAIMessageAsync(int userId, int sessionId, string content);
        
        // Message management
        Task<bool> DeleteMessageAsync(int messageId, int userId);
        Task<MessageDto?> EditMessageAsync(int messageId, int userId, string newContent);
        
        // Session context
        Task<List<ChatMessage>> GetSessionChatHistoryAsync(int sessionId, int messageCount = 10);
        Task<int> GetSessionMessageCountAsync(int sessionId);
        
        // Admin message management methods
        Task<AdminMessageStatsResponse> GetMessageStatsAsync();
        Task<IEnumerable<AdminMessageDto>> GetSessionMessagesForAdminAsync(int sessionId);
    }

    // DTOs for chat service
    public class ChatResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public MessageDto? UserMessage { get; set; }
        public MessageDto? AIResponse { get; set; }
        public string? ErrorDetails { get; set; }
        public int TokensUsed { get; set; }
        public double ProcessingTimeMs { get; set; }
    }

    // NEW: DTO for streaming chat responses
    public class StreamingChatResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public MessageDto? UserMessage { get; set; }
        public string? StreamingMessageId { get; set; }
        public string? ErrorDetails { get; set; }
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    }

    public class MessageDto
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public string MessageType { get; set; } = string.Empty; // "user" or "assistant"
        public DateTime CreatedAt { get; set; }
        public bool IsEdited { get; set; }
        public DateTime? EditedAt { get; set; }
        public int UserId { get; set; }
        public int PromptSessionId { get; set; }
        public string UserName { get; set; } = string.Empty;
    }
    
    // Admin-specific DTOs
    public class AdminMessageStatsResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int TotalMessages { get; set; }
        public int TodayMessages { get; set; }
        public int UserMessages { get; set; }
        public int AIMessages { get; set; }
    }

    public class AdminMessageDto
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public string MessageType { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsEdited { get; set; }
        public DateTime? EditedAt { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string UserFullName { get; set; } = string.Empty;
    }
} 