namespace Services.Interfaces
{
    public interface IAIService
    {
        // Chat operations
        Task<AIResponse> SendMessageAsync(string message, int? sessionId = null, int? userId = null);
        Task<AIResponse> SendMessageWithContextAsync(string message, List<ChatMessage> chatHistory, int? userId = null);
        Task<AIResponse> GenerateResponseAsync(string prompt, AIOptions? options = null);
        
        // Content generation
        Task<AIResponse> GenerateTextAsync(string prompt, int maxTokens = 1000);
        Task<AIResponse> SummarizeTextAsync(string text, int maxLength = 500);
        Task<AIResponse> TranslateTextAsync(string text, string targetLanguage = "vi");
        
        // Validation and utilities
        Task<bool> ValidateAPIConnectionAsync();
        Task<AIUsageStats> GetUsageStatsAsync(int userId);
    }

    // DTOs for AI service
    public class AIResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Response { get; set; } = string.Empty;
        public string? ErrorDetails { get; set; }
        public int TokensUsed { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public AIResponseMetadata? Metadata { get; set; }
    }

    public class ChatMessage
    {
        public string Role { get; set; } = string.Empty; // "user" or "assistant"
        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class AIOptions
    {
        public int MaxTokens { get; set; } = 1000;
        public double Temperature { get; set; } = 0.7;
        public int TopK { get; set; } = 40;
        public double TopP { get; set; } = 0.95;
        public string Model { get; set; } = "gemini-pro";
        public List<string>? StopSequences { get; set; }
    }

    public class AIResponseMetadata
    {
        public string Model { get; set; } = string.Empty;
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public int TotalTokens { get; set; }
        public double ProcessingTimeMs { get; set; }
    }

    public class AIUsageStats
    {
        public int UserId { get; set; }
        public int TotalRequests { get; set; }
        public int TotalTokensUsed { get; set; }
        public DateTime LastUsed { get; set; }
        public int RequestsToday { get; set; }
        public int TokensToday { get; set; }
    }
} 